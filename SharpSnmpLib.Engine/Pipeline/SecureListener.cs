﻿using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Listener class.
    /// </summary>
    public sealed class SecureListener
    {
        private UserRegistry _users;
        private bool _disposed;

        /// <summary>
        /// Error message for non IP v4 OS.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Pv")]
        public const string ErrorIPv4NotSupported = "cannot use IP v4 as the OS does not support it";

        /// <summary>
        /// Error message for non IP v6 OS.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Pv")]
        public const string ErrorIPv6NotSupported = "cannot use IP v6 as the OS does not support it";

        /// <summary>
        /// Initializes a new instance of the <see cref="List"/> class.
        /// </summary>
        public SecureListener()
        {
            Bindings = new List<SecureListenerBinding>();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="List"/> is reclaimed by garbage collection.
        /// </summary>
        ~SecureListener()
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

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (Bindings != null)
                {
                    foreach (var binding in Bindings)
                    {
                        binding.Dispose();
                    }

                    Bindings.Clear();
                    Bindings = null;
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

                return _users ?? (_users = new UserRegistry());
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
        /// Gets or sets a value indicating whether this <see cref="List"/> is active.
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

            foreach (var binding in Bindings)
            {
                binding.Stop();
            }

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

            try
            {
                foreach (var binding in Bindings)
                {
                    binding.Start();
                }
            }
            catch (PortInUseException)
            {
                Stop(); // stop all started bindings.
                throw;
            }

            Active = true;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <exception cref="PortInUseException"/>
        public async Task StartAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Active)
            {
                return;
            }

            try
            {
                foreach (var binding in Bindings)
                {
                    await binding.StartAsync();
                }
            }
            catch (PortInUseException)
            {
                Stop(); // stop all started bindings.
                throw;
            }

            Active = true;
        }

        /// <summary>
        /// Gets or sets the bindings.
        /// </summary>
        /// <value>The bindings.</value>
        public IList<SecureListenerBinding> Bindings { get; set; }

        /// <summary>
        /// Occurs when an exception is raised.
        /// </summary>
        public event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Adds the binding.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void AddBinding(IPEndPoint endpoint)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Active)
            {
                throw new InvalidOperationException("Must be called when Active == false");
            }

            if (Bindings.Any(existed => existed.Endpoint.Equals(endpoint)))
            {
                return;
            }

            var binding = new SecureListenerBinding(Users, endpoint);
            binding.ExceptionRaised += (o, args) =>
            {
                ExceptionRaised?.Invoke(o, args);
            };
            binding.MessageReceived += (o, args) =>
            {
                MessageReceived?.Invoke(o, args);
            };
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
                    Bindings.RemoveAt(i);
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
    }
}
