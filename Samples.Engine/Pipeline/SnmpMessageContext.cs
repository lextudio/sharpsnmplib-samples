// SNMP message context for middleware pipeline.
// Copyright (C) 2024 Lex Li
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

using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Samples.Pipeline
{
    /// <summary>
    /// Context object passed through the middleware pipeline.
    /// Wraps the raw SNMP message and accumulates state as middleware processes it.
    /// </summary>
    public sealed class SnmpMessageContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpMessageContext"/> class.
        /// </summary>
        /// <param name="request">The raw SNMP request message.</param>
        /// <param name="sender">The sender endpoint.</param>
        /// <param name="binding">The listener binding that received the message.</param>
        public SnmpMessageContext(ISnmpMessage request, IPEndPoint sender, IListenerBinding binding)
        {
            Request = request;
            Sender = sender;
            Binding = binding;
        }

        /// <summary>
        /// Gets the raw SNMP request message.
        /// </summary>
        public ISnmpMessage Request { get; }

        /// <summary>
        /// Gets the sender endpoint.
        /// </summary>
        public IPEndPoint Sender { get; }

        /// <summary>
        /// Gets the listener binding.
        /// </summary>
        public IListenerBinding Binding { get; }

        /// <summary>
        /// Gets or sets the SNMP context created by the context factory middleware.
        /// This is the version-specific context (NormalSnmpContext or SecureSnmpContext).
        /// </summary>
        public ISnmpContext SnmpContext { get; set; }
    }
}
