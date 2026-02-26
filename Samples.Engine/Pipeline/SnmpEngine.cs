// SNMP engine class.
// Copyright (C) 2009-2010 Lex Li
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/28/2009
 * Time: 12:40 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace Samples.Pipeline
{
    /// <summary>
    /// SNMP engine, who is the core of an SNMP entity (manager or agent).
    /// Like ASP.NET Core's <c>WebApplication</c>, the engine hosts a middleware pipeline
    /// and dispatches each incoming message through it.
    /// </summary>
    public sealed class SnmpEngine : IDisposable
    {
        private readonly Func<SnmpMessageContext, Task> _pipeline;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpEngine"/> class
        /// with a pre-built middleware pipeline.
        /// </summary>
        /// <param name="listener">The listener (transport layer).</param>
        /// <param name="pipeline">The compiled middleware pipeline.</param>
        public SnmpEngine(Listener listener, Func<SnmpMessageContext, Task> pipeline)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpEngine"/> class.
        /// Builds the default middleware pipeline (context factory → authentication → request processing).
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <param name="group">Engine core group.</param>
        /// <param name="store">The object store (MIB).</param>
        /// <param name="membership">The membership provider. If <c>null</c>, a default
        /// provider accepting community "public" for v1/v2c and USM for v3 is used.</param>
        /// <param name="trapV1Received">Optional callback for trap v1 messages.</param>
        /// <param name="trapV2Received">Optional callback for trap v2 messages.</param>
        /// <param name="informReceived">Optional callback for inform messages.</param>
        public SnmpEngine(
            Listener listener,
            EngineGroup group,
            ObjectStore store,
            IMembershipProvider membership = null,
            Action<TrapV1MessageReceivedEventArgs> trapV1Received = null,
            Action<TrapV2MessageReceivedEventArgs> trapV2Received = null,
            Action<InformRequestMessageReceivedEventArgs> informReceived = null)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));

            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }
            
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            membership ??= new ComposedMembershipProvider(new IMembershipProvider[]
            {
                new Version1MembershipProvider(new OctetString("public"), new OctetString("public")),
                new Version2MembershipProvider(new OctetString("public"), new OctetString("public")),
                new Version3MembershipProvider()
            });

            _pipeline = new SnmpPipelineBuilder()
                .UseContextFactory(listener.Users, group)
                .UseAuthentication(membership)
                .UseMessageHandler(store, trapV1Received, trapV2Received, informReceived)
                .Build();
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
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SnmpEngine"/> is reclaimed by garbage collection.
        /// </summary>
        ~SnmpEngine()
        {
            Dispose(false);
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
                if (Listener != null)
                {
                    Listener.Dispose();
                    Listener = null;
                }
            }
            
            _disposed = true;
        }
        
        /// <summary>
        /// Gets or sets the listener.
        /// </summary>
        /// <value>The listener.</value>
        public Listener Listener { get; private set; }

        private async void ListenerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var context = new SnmpMessageContext(e.Message, e.Sender, e.Binding);
            try
            {
                await _pipeline(context);
            }
            catch (Exception)
            {
                // Swallow exceptions to prevent crashing the listener.
            }

            // Send the response after the pipeline completes (like Kestrel writing
            // the HTTP response after all middleware has run).
            context.SnmpContext?.SendResponse();
        }

        /// <summary>
        /// Starts the engine.
        /// </summary>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            
            Listener.ExceptionRaised += ListenerExceptionRaised;
            Listener.MessageReceived += ListenerMessageReceived;
            Listener.Start();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            
            Listener.Stop();
            Listener.ExceptionRaised -= ListenerExceptionRaised;
            Listener.MessageReceived -= ListenerMessageReceived;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SnmpEngine"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                
                return Listener.Active;
            }
        }

        private void ListenerExceptionRaised(object sender, ExceptionRaisedEventArgs e)
        {
            var handler = ExceptionRaised;
            if (handler != null)
            {
                handler(sender, e);
            }
        }        
        
        /// <summary>
        /// Occurs when an exception is raised.
        /// </summary>
        public event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;
    }
}
