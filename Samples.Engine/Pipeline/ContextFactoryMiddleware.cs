// SNMP context factory middleware.
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
using System.Threading.Tasks;
using Lextm.SharpSnmpLib.Security;

namespace Samples.Pipeline
{
    /// <summary>
    /// Middleware that creates the version-specific SNMP context (v1/v2c or v3)
    /// from the raw incoming message.
    /// </summary>
    public sealed class ContextFactoryMiddleware : ISnmpMiddleware
    {
        private readonly UserRegistry _users;
        private readonly EngineGroup _group;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextFactoryMiddleware"/> class.
        /// </summary>
        /// <param name="users">The user registry.</param>
        /// <param name="group">The engine core group.</param>
        public ContextFactoryMiddleware(UserRegistry users, EngineGroup group)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _group = group ?? throw new ArgumentNullException(nameof(group));
        }

        /// <inheritdoc />
        public async Task InvokeAsync(SnmpMessageContext context, Func<SnmpMessageContext, Task> next)
        {
            context.SnmpContext = SnmpContextFactory.Create(
                context.Request,
                context.Sender,
                _users,
                _group,
                context.Binding);

            await next(context);
        }
    }
}
