// Listener class.
// Copyright (C) 2008-2018 Malcolm Crowe, Lex Li, and other contributors.
// Copyright (C) 2018-2026 LeXtudio Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Samples.Pipeline
{
    /// <summary>
    /// Listener that manages one or more <see cref="UdpTransportListener"/> instances
    /// and dispatches parsed SNMP messages through events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the high-performance replacement for the legacy listener. Internally it
    /// reads <see cref="Lextm.SharpSnmpLib.Transport.SnmpDatagram"/> values from each
    /// transport listener's channel, parses them with <see cref="MessageFactory"/>,
    /// and fires <see cref="MessageReceived"/> / <see cref="ExceptionRaised"/> events
    /// consumed by <see cref="SnmpEngine"/>.
    /// </para>
    /// </remarks>
    public sealed class Listener : IDisposable
    {
        private UserRegistry? _users;
        private CancellationTokenSource? _cts;
        private List<Task>? _dispatchTasks;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        public Listener()
        {
            Bindings = new List<UdpTransportListener>();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Listener"/> is reclaimed by garbage collection.
        /// </summary>
        ~Listener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes resources in use.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();

                if (Bindings != null)
                {
                    foreach (var binding in Bindings)
                    {
                        binding.Dispose();
                    }

                    Bindings.Clear();
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        /// <value>The users.</value>
        public UserRegistry Users
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return _users ??= new UserRegistry();
            }

            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                _users = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Listener"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; private set; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!Active)
            {
                return;
            }

            // Cancel dispatch loops first.
            _cts?.Cancel();

            // Stop all transport listeners (closes sockets, unblocks receive loops).
            foreach (var binding in Bindings)
            {
                binding.Stop();
            }

            // Wait for dispatch tasks to drain.
            if (_dispatchTasks != null)
            {
                try
                {
                    Task.WaitAll(_dispatchTasks.ToArray(), TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Swallow — tasks may have been cancelled.
                }

                _dispatchTasks = null;
            }

            _cts?.Dispose();
            _cts = null;
            Active = false;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <exception cref="PortInUseException"/>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Active)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _dispatchTasks = new List<Task>(Bindings.Count);

            try
            {
                foreach (var binding in Bindings)
                {
                    binding.Start();
                    _dispatchTasks.Add(Task.Run(() => DispatchLoopAsync(binding, _cts.Token)));
                }
            }
#pragma warning disable CS0618 // Type or member is obsolete
            catch (PortInUseException)
#pragma warning restore CS0618
            {
                Stop(); // stop all started bindings.
                throw;
            }

            Active = true;
        }

        /// <summary>
        /// Gets the transport listener bindings.
        /// </summary>
        internal IList<UdpTransportListener> Bindings { get; }

        /// <summary>
        /// Occurs when an exception is raised.
        /// </summary>
        public event EventHandler<ExceptionRaisedEventArgs>? ExceptionRaised;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        /// <summary>
        /// Adds a UDP binding.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="multicastAddress">Optional multicast address.</param>
        public void AddBinding(IPEndPoint endpoint, string? multicastAddress = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Active)
            {
                throw new InvalidOperationException("Must be called when Active == false");
            }

            if (Bindings.Any(existing => existing.Endpoint.Equals(endpoint)))
            {
                return;
            }

            var binding = new UdpTransportListener(endpoint, multicastAddress);
            Bindings.Add(binding);
        }

        /// <summary>
        /// Removes the binding.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void RemoveBinding(IPEndPoint endpoint)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Active)
            {
                throw new InvalidOperationException("Must be called when Active == false");
            }

            for (var i = 0; i < Bindings.Count; i++)
            {
                if (Bindings[i].Endpoint.Equals(endpoint))
                {
                    Bindings[i].Dispose();
                    Bindings.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Clears the bindings.
        /// </summary>
        public void ClearBindings()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            foreach (var binding in Bindings)
            {
                binding.Stop();
                binding.Dispose();
            }

            Bindings.Clear();
        }

        /// <summary>
        /// Reads datagrams from a transport listener's channel, parses SNMP messages,
        /// and fires the <see cref="MessageReceived"/> event. This is the bridge between
        /// the channel-based transport layer and the event-based engine layer.
        /// </summary>
        private async Task DispatchLoopAsync(UdpTransportListener transport, CancellationToken ct)
        {
            try
            {
                await foreach (var datagram in transport.DatagramReader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        var messages = MessageFactory.ParseMessages(datagram.Buffer, 0, datagram.Length, Users);
                        if (messages == null)
                        {
                            continue;
                        }

                        var sender = datagram.GetSenderEndPoint();
                        foreach (var message in messages)
                        {
                            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(sender, message, transport));
                        }
                    }
                    catch (Exception ex)
                    {
                        var bytes = new byte[datagram.Length];
                        Array.Copy(datagram.Buffer, bytes, datagram.Length);
                        var exception = new MessageFactoryException("Invalid message bytes found. Use tracing to analyze the bytes.", ex);
                        exception.SetBytes(bytes);
                        ExceptionRaised?.Invoke(this, new ExceptionRaisedEventArgs(exception));
                    }
                    finally
                    {
                        datagram.ReturnBuffer();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                ExceptionRaised?.Invoke(this, new ExceptionRaisedEventArgs(ex));
            }
        }
    }
}
