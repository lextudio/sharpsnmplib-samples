// High-performance UDP transport listener.
// Copyright (C) 2026 LeXtudio Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Transport;

namespace Samples.Pipeline
{
    /// <summary>
    /// High-performance UDP transport listener that implements both
    /// <see cref="ITransportListener"/> (channel-based producer) and
    /// <see cref="IListenerBinding"/> (response sender for the pipeline).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="ArrayPool{T}.Shared"/> for zero-allocation receive buffers,
    /// <see cref="SocketAddress"/>-based receive for reduced per-packet allocations,
    /// and multiple concurrent receive loops for high throughput.
    /// </para>
    /// <para>
    /// Each instance binds a single UDP socket. The <see cref="Listener"/> class
    /// manages one or more <see cref="UdpTransportListener"/> instances.
    /// </para>
    /// </remarks>
    public sealed class UdpTransportListener : ITransportListener, IListenerBinding, IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly string? _multicastAddress;
        private readonly Channel<SnmpDatagram> _channel;
        private Socket? _socket;
        private CancellationTokenSource? _cts;
        private Task[]? _receiveTasks;
        private bool _disposed;

        /// <summary>
        /// Number of concurrent receive loops per socket.
        /// </summary>
        private const int ConcurrentReceiveLoops = 4;

        /// <summary>
        /// Maximum UDP datagram size for SNMP.
        /// </summary>
        private const int MaxDatagramSize = 65535;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpTransportListener"/> class.
        /// </summary>
        /// <param name="endpoint">The local endpoint to bind to.</param>
        /// <param name="multicastAddress">Optional IPv6 multicast address to join.</param>
        public UdpTransportListener(IPEndPoint endpoint, string? multicastAddress = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _multicastAddress = multicastAddress;
            _channel = Channel.CreateBounded<SnmpDatagram>(new BoundedChannelOptions(1024)
            {
                SingleWriter = false,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        /// <inheritdoc/>
        public ChannelReader<SnmpDatagram> DatagramReader => _channel.Reader;

        /// <inheritdoc/>
        EndPoint ITransportListener.LocalEndPoint => Endpoint;

        /// <summary>
        /// Gets the local endpoint this listener is bound to.
        /// </summary>
        public IPEndPoint Endpoint => (IPEndPoint?)_socket?.LocalEndPoint ?? _endpoint;

        /// <summary>
        /// Gets a value indicating whether this listener is actively receiving.
        /// </summary>
        public bool Active => _cts != null && !_cts.IsCancellationRequested;

        /// <summary>
        /// Starts the listener synchronously. Binds the socket and launches
        /// concurrent receive loops as background tasks.
        /// </summary>
        /// <exception cref="PortInUseException">The endpoint is already in use.</exception>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var addressFamily = _endpoint.AddressFamily;
            if (addressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4)
            {
                throw new InvalidOperationException("Cannot use IPv4 as the OS does not support it.");
            }

            if (addressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
            {
                throw new InvalidOperationException("Cannot use IPv6 as the OS does not support it.");
            }

            _socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
            if (SnmpMessageExtension.IsRunningOnWindows)
            {
                _socket.ExclusiveAddressUse = true;
            }

            try
            {
                _socket.Bind(_endpoint);
                if (addressFamily == AddressFamily.InterNetworkV6
                    && !string.IsNullOrEmpty(_multicastAddress))
                {
                    // Strip brackets if present (e.g., "[ff02::1]" → "ff02::1")
                    var addr = _multicastAddress.Trim('[', ']');
                    if (IPAddress.TryParse(addr, out var multicastAddress))
                    {
                        _socket.SetSocketOption(
                            SocketOptionLevel.IPv6,
                            SocketOptionName.AddMembership,
                            new IPv6MulticastOption(multicastAddress));
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                throw new PortInUseException("Endpoint is already in use", ex) { Endpoint = _endpoint };
#pragma warning restore CS0618
            }

            _cts = new CancellationTokenSource();
            _receiveTasks = new Task[ConcurrentReceiveLoops];
            for (int i = 0; i < ConcurrentReceiveLoops; i++)
            {
                _receiveTasks[i] = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            }
        }

        /// <summary>
        /// Stops the listener, cancelling all receive loops and closing the socket.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();

            // Close the socket to unblock any pending ReceiveFromAsync calls.
            var socket = _socket;
            _socket = null;
            if (socket != null)
            {
                try { socket.Close(0); } catch { }
                try { socket.Dispose(); } catch { }
            }

            // Wait for receive tasks to complete (with timeout to avoid deadlocks).
            if (_receiveTasks != null)
            {
                try
                {
                    Task.WaitAll(_receiveTasks, TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Swallow — tasks may have been cancelled or faulted.
                }

                _receiveTasks = null;
            }

            _cts.Dispose();
            _cts = null;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Start();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Stop();
            return Task.CompletedTask;
        }

        /// <summary>
        /// The core receive loop. Each concurrent instance reads from the shared
        /// socket and writes <see cref="SnmpDatagram"/> instances to the channel.
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            // Each loop gets its own SocketAddress to avoid cross-loop races.
            var senderAddress = new SocketAddress(_endpoint.AddressFamily);

            while (!ct.IsCancellationRequested)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(MaxDatagramSize);
                try
                {
                    var bytesReceived = await _socket!.ReceiveFromAsync(
                        buffer.AsMemory(),
                        SocketFlags.None,
                        senderAddress,
                        ct).ConfigureAwait(false);

                    // Copy the sender address since the receive loop reuses the same instance.
                    var senderCopy = new SocketAddress(senderAddress.Family, senderAddress.Size);
                    senderAddress.Buffer.Slice(0, senderAddress.Size).CopyTo(senderCopy.Buffer);

                    var datagram = new SnmpDatagram(buffer, bytesReceived, senderCopy);
                    await _channel.Writer.WriteAsync(datagram, ct).ConfigureAwait(false);
                    // Ownership of buffer transferred to datagram consumer.
                }
                catch (OperationCanceledException)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // WSAECONNRESET — ignore and continue.
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                catch (ObjectDisposedException)
                {
                    // Socket was closed during shutdown.
                    ArrayPool<byte>.Shared.Return(buffer);
                    break;
                }
                catch (Exception)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    // Bubble up to the Task — Listener's dispatch loop will surface it
                    // via the ExceptionRaised event.
                    throw;
                }
            }
        }

        #region IListenerBinding

        /// <inheritdoc/>
        public void SendResponse(ISnmpMessage response, EndPoint receiver)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (_socket == null)
            {
                return;
            }

            var buffer = response.ToBytes();
            try
            {
                _socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, receiver);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // Socket was closed — ignore.
            }
        }

        /// <inheritdoc/>
        public async Task SendResponseAsync(ISnmpMessage response, EndPoint receiver)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (_socket == null)
            {
                return;
            }

            try
            {
                await _socket.SendToAsync(
                    new ArraySegment<byte>(response.ToBytes()),
                    SocketFlags.None,
                    receiver).ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // Socket was closed — ignore.
            }
        }

        #endregion

        #region ITransportListener.SendResponseAsync

        /// <inheritdoc/>
        public async ValueTask SendResponseAsync(
            ReadOnlyMemory<byte> response,
            SocketAddress receiver,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (_socket == null)
            {
                return;
            }

            try
            {
                await _socket.SendToAsync(response, SocketFlags.None, receiver, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // Socket was closed — ignore.
            }
        }

        #endregion

        #region IAsyncDisposable / IDisposable

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                Stop();
                _channel.Writer.TryComplete();
                _disposed = true;
            }

            await ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _channel.Writer.TryComplete();
                _disposed = true;
            }
        }

        #endregion

        /// <inheritdoc/>
        public override string ToString() => $"UdpTransportListener({_endpoint})";
    }
}
