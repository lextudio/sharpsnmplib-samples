// TCP transport listener for SNMP over TCP (RFC 3430).
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
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipelines;
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
    /// TCP transport listener implementing RFC 3430 length-prefix framing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Accepts TCP connections, reads SNMP messages framed with a 4-byte big-endian
    /// length prefix (RFC 3430 §2), and produces <see cref="SnmpDatagram"/> values
    /// through a <see cref="Channel{T}"/>. Implements both <see cref="ITransportListener"/>
    /// and <see cref="IListenerBinding"/> so that the existing pipeline infrastructure
    /// (<see cref="Listener"/>, <see cref="SnmpContextBase"/>) works transparently.
    /// </para>
    /// <para>
    /// This is an <b>experimental</b> implementation.
    /// </para>
    /// </remarks>
    public sealed class TcpTransportListener : ITransportListener, IListenerBinding, IDisposable
    {
        private enum FrameEncoding
        {
            Unknown = 0,
            Rfc3430LengthPrefix = 1,
            Asn1SequenceLength = 2
        }

        private readonly IPEndPoint _endpoint;
        private readonly Channel<SnmpDatagram> _channel;
        private Socket? _listenSocket;
        private CancellationTokenSource? _cts;
        private Task? _acceptTask;
        private readonly ConcurrentDictionary<SocketAddress, Socket> _clientSockets = new();
        private readonly ConcurrentDictionary<SocketAddress, FrameEncoding> _clientFrameEncodings = new();
        private bool _disposed;

        /// <summary>
        /// Maximum SNMP message size over TCP.
        /// </summary>
        private const int MaxMessageSize = 65535;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransportListener"/> class.
        /// </summary>
        /// <param name="endpoint">The local endpoint to bind to.</param>
        public TcpTransportListener(IPEndPoint endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
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
        public IPEndPoint Endpoint => (IPEndPoint?)_listenSocket?.LocalEndPoint ?? _endpoint;

        /// <summary>
        /// Gets a value indicating whether this listener is actively accepting connections.
        /// </summary>
        public bool Active => _cts != null && !_cts.IsCancellationRequested;

        /// <summary>
        /// Starts the listener. Binds a TCP socket, begins listening, and launches
        /// the accept loop as a background task.
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

            _listenSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (SnmpMessageExtension.IsRunningOnWindows)
            {
                _listenSocket.ExclusiveAddressUse = true;
            }

            try
            {
                _listenSocket.Bind(_endpoint);
                _listenSocket.Listen(128);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                throw new PortInUseException("Endpoint is already in use", ex) { Endpoint = _endpoint };
#pragma warning restore CS0618
            }

            _cts = new CancellationTokenSource();
            _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Stops the listener, cancelling all connection loops and closing sockets.
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

            // Close the listen socket to unblock AcceptAsync.
            var socket = _listenSocket;
            _listenSocket = null;
            if (socket != null)
            {
                try { socket.Close(0); } catch { }
                try { socket.Dispose(); } catch { }
            }

            // Close all client sockets.
            foreach (var kvp in _clientSockets)
            {
                try { kvp.Value.Close(0); } catch { }
                try { kvp.Value.Dispose(); } catch { }
            }

            _clientSockets.Clear();

            // Wait for accept task to complete.
            if (_acceptTask != null)
            {
                try
                {
                    _acceptTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Swallow — task may have been cancelled.
                }

                _acceptTask = null;
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
        /// Accepts incoming TCP connections and spawns a per-connection read loop.
        /// </summary>
        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Socket client;
                try
                {
                    client = await _listenSocket!.AcceptAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }

                client.NoDelay = true;

                // Fire-and-forget per-connection handler.
                // The connection task is tracked via _clientSockets for cleanup.
                _ = HandleConnectionAsync(client, ct);
            }
        }

        /// <summary>
        /// Handles a single TCP connection using <see cref="System.IO.Pipelines"/>
        /// for incremental reading and RFC 3430 frame parsing.
        /// </summary>
        private async Task HandleConnectionAsync(Socket client, CancellationToken ct)
        {
            // Build a SocketAddress for this client to use as the sender identity.
            var remoteEndPoint = (IPEndPoint)client.RemoteEndPoint!;
            var clientAddress = remoteEndPoint.Serialize();

            _clientSockets.TryAdd(clientAddress, client);
            _clientFrameEncodings.TryAdd(clientAddress, FrameEncoding.Unknown);

            var pipe = new Pipe(new PipeOptions(
                pool: MemoryPool<byte>.Shared,
                minimumSegmentSize: 4096));

            try
            {
                var fillTask = FillPipeAsync(client, pipe.Writer, ct);
                var parseTask = ParsePipeAsync(clientAddress, pipe.Reader, ct);

                await Task.WhenAll(fillTask, parseTask).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Connection-level errors are swallowed; the connection is simply closed.
            }
            finally
            {
                _clientSockets.TryRemove(clientAddress, out _);
                _clientFrameEncodings.TryRemove(clientAddress, out _);
                try { client.Close(0); } catch { }
                try { client.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Reads raw bytes from the TCP socket into the pipe.
        /// </summary>
        private static async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken ct)
        {
            const int MinBufferSize = 4096;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var memory = writer.GetMemory(MinBufferSize);
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, ct)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        break; // Connection closed by remote.
                    }

                    writer.Advance(bytesRead);

                    var result = await writer.FlushAsync(ct).ConfigureAwait(false);
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                await writer.CompleteAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Parses RFC 3430 length-prefixed SNMP frames from the pipe and writes
        /// <see cref="SnmpDatagram"/> values into the channel.
        /// </summary>
        private async Task ParsePipeAsync(
            SocketAddress senderAddress,
            PipeReader reader,
            CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(ct).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    while (TryReadFrame(ref buffer, out var frame, out var encoding))
                    {
                        _clientFrameEncodings.AddOrUpdate(senderAddress, encoding, (_, _) => encoding);

                        // Copy the frame into a pooled buffer for the channel consumer.
                        var length = (int)frame.Length;
                        var pooled = ArrayPool<byte>.Shared.Rent(length);
                        frame.CopyTo(pooled);

                        // Copy the sender address (it's shared across all frames from this connection).
                        var senderCopy = new SocketAddress(senderAddress.Family, senderAddress.Size);
                        senderAddress.Buffer.Slice(0, senderAddress.Size).CopyTo(senderCopy.Buffer);

                        var datagram = new SnmpDatagram(pooled, length, senderCopy);
                        await _channel.Writer.WriteAsync(datagram, ct).ConfigureAwait(false);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to read a single RFC 3430 length-prefixed frame from the buffer.
        /// </summary>
        /// <param name="buffer">
        /// The unconsumed pipe buffer. On success, advanced past the consumed frame.
        /// </param>
        /// <param name="frame">The SNMP message bytes (without the length prefix).</param>
        /// <returns><c>true</c> if a complete frame was read; <c>false</c> if more data is needed.</returns>
        private static bool TryReadFrame(
            ref ReadOnlySequence<byte> buffer,
            out ReadOnlySequence<byte> frame,
            out FrameEncoding encoding)
        {
            frame = default;
            encoding = FrameEncoding.Unknown;

            if (buffer.Length < 2)
            {
                return false;
            }

            // First try RFC 3430 framing (4-byte big-endian message length).
            if (buffer.Length >= 4)
            {
                Span<byte> lengthBytes = stackalloc byte[4];
                buffer.Slice(0, 4).CopyTo(lengthBytes);
                int messageLength = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);

                if (messageLength > 0 && messageLength <= MaxMessageSize)
                {
                    if (buffer.Length < 4 + messageLength)
                    {
                        return false; // Incomplete frame; need more data.
                    }

                    frame = buffer.Slice(4, messageLength);
                    buffer = buffer.Slice(4 + messageLength);
                    encoding = FrameEncoding.Rfc3430LengthPrefix;
                    return true;
                }
            }

            // Fallback: accept BER-definite-length top-level SEQUENCE (used by some tools over TCP).
            if (!TryReadAsn1SequenceLength(buffer, out var totalLength))
            {
                // Invalid frame — discard the connection's remaining data.
                buffer = buffer.Slice(buffer.End);
                return false;
            }

            if (buffer.Length < totalLength)
            {
                return false; // Incomplete frame; need more data.
            }

            frame = buffer.Slice(0, totalLength);
            buffer = buffer.Slice(totalLength);
            encoding = FrameEncoding.Asn1SequenceLength;
            return true;
        }

        private static bool TryReadAsn1SequenceLength(ReadOnlySequence<byte> buffer, out int totalLength)
        {
            totalLength = 0;

            if (buffer.Length < 2)
            {
                return false;
            }

            Span<byte> header = stackalloc byte[6];
            int toCopy = (int)Math.Min(buffer.Length, header.Length);
            buffer.Slice(0, toCopy).CopyTo(header);

            if (header[0] != 0x30)
            {
                return false;
            }

            byte lengthByte = header[1];
            if ((lengthByte & 0x80) == 0)
            {
                int contentLength = lengthByte;
                if (contentLength <= 0 || contentLength > MaxMessageSize)
                {
                    return false;
                }

                totalLength = 2 + contentLength;
                return true;
            }

            int lengthOfLength = lengthByte & 0x7F;
            if (lengthOfLength == 0 || lengthOfLength > 4)
            {
                return false;
            }

            int headerLength = 2 + lengthOfLength;
            if (buffer.Length < headerLength)
            {
                return false;
            }

            int contentLengthLong = 0;
            for (int i = 0; i < lengthOfLength; i++)
            {
                contentLengthLong = (contentLengthLong << 8) | header[2 + i];
            }

            if (contentLengthLong <= 0 || contentLengthLong > MaxMessageSize)
            {
                return false;
            }

            totalLength = headerLength + contentLengthLong;
            return true;
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

            // Find the client socket for this receiver.
            var ipReceiver = (IPEndPoint)receiver;
            var receiverAddress = ipReceiver.Serialize();

            if (!_clientSockets.TryGetValue(receiverAddress, out var clientSocket))
            {
                return; // Client disconnected.
            }

            var messageBytes = response.ToBytes();

            try
            {
                if (!_clientFrameEncodings.TryGetValue(receiverAddress, out var encoding) ||
                    encoding == FrameEncoding.Rfc3430LengthPrefix)
                {
                    var lengthPrefix = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, messageBytes.Length);
                    clientSocket.Send(lengthPrefix, SocketFlags.None);
                }

                clientSocket.Send(messageBytes, SocketFlags.None);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
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

            var ipReceiver = (IPEndPoint)receiver;
            var receiverAddress = ipReceiver.Serialize();

            if (!_clientSockets.TryGetValue(receiverAddress, out var clientSocket))
            {
                return; // Client disconnected.
            }

            var messageBytes = response.ToBytes();

            try
            {
                if (!_clientFrameEncodings.TryGetValue(receiverAddress, out var encoding) ||
                    encoding == FrameEncoding.Rfc3430LengthPrefix)
                {
                    var lengthPrefix = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, messageBytes.Length);
                    await clientSocket.SendAsync(lengthPrefix, SocketFlags.None).ConfigureAwait(false);
                }

                await clientSocket.SendAsync(messageBytes, SocketFlags.None).ConfigureAwait(false);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
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

            if (!_clientSockets.TryGetValue(receiver, out var clientSocket))
            {
                return; // Client disconnected.
            }

            try
            {
                if (!_clientFrameEncodings.TryGetValue(receiver, out var encoding) ||
                    encoding == FrameEncoding.Rfc3430LengthPrefix)
                {
                    var lengthPrefix = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, response.Length);
                    await clientSocket.SendAsync(lengthPrefix, SocketFlags.None, cancellationToken)
                        .ConfigureAwait(false);
                }

                await clientSocket.SendAsync(response, SocketFlags.None, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
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
        public override string ToString() => $"TcpTransportListener({_endpoint})";
    }
}
