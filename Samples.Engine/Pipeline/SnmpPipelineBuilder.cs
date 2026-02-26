// SNMP pipeline builder.
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Samples.Pipeline
{
    /// <summary>
    /// Fluent API for building an SNMP middleware pipeline.
    /// </summary>
    public sealed class SnmpPipelineBuilder
    {
        private readonly List<ISnmpMiddleware> _middleware = new List<ISnmpMiddleware>();

        /// <summary>
        /// Adds a context factory middleware that creates the version-specific SNMP context.
        /// </summary>
        /// <param name="users">The user registry.</param>
        /// <param name="group">The engine core group.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public SnmpPipelineBuilder UseContextFactory(UserRegistry users, EngineGroup group)
        {
            _middleware.Add(new ContextFactoryMiddleware(users, group));
            return this;
        }

        /// <summary>
        /// Adds an authentication middleware.
        /// </summary>
        /// <param name="membership">The membership provider.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public SnmpPipelineBuilder UseAuthentication(IMembershipProvider membership)
        {
            _middleware.Add(new AuthenticationMiddleware(membership));
            return this;
        }

        /// <summary>
        /// Adds terminal request processing middleware.
        /// </summary>
        /// <param name="store">The object store (MIB).</param>
        /// <param name="trapV1Received">Optional callback for trap v1 messages.</param>
        /// <param name="trapV2Received">Optional callback for trap v2 messages.</param>
        /// <param name="informReceived">Optional callback for inform messages.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public SnmpPipelineBuilder UseMessageHandler(
            ObjectStore store,
            Action<TrapV1MessageReceivedEventArgs> trapV1Received = null,
            Action<TrapV2MessageReceivedEventArgs> trapV2Received = null,
            Action<InformRequestMessageReceivedEventArgs> informReceived = null)
        {
            _middleware.Add(new MessageHandlerMiddleware(store, trapV1Received, trapV2Received, informReceived));
            return this;
        }

        /// <summary>
        /// Adds custom middleware to the pipeline.
        /// </summary>
        /// <param name="middleware">The middleware instance.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public SnmpPipelineBuilder Use(ISnmpMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middleware.Add(middleware);
            return this;
        }

        /// <summary>
        /// Builds the middleware pipeline into a single invocable delegate.
        /// </summary>
        /// <returns>A delegate that invokes the complete pipeline.</returns>
        public Func<SnmpMessageContext, Task> Build()
        {
            // Terminal: does nothing (end of pipeline).
            Func<SnmpMessageContext, Task> pipeline = _ => Task.CompletedTask;

            // Compose middleware in reverse order so the first-added middleware is outermost.
            for (int i = _middleware.Count - 1; i >= 0; i--)
            {
                var current = _middleware[i];
                var next = pipeline;
                pipeline = context => current.InvokeAsync(context, next);
            }

            return pipeline;
        }
    }
}
