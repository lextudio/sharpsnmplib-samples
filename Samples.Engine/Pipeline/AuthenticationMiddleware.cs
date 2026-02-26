// SNMP authentication middleware.
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

namespace Samples.Pipeline
{
    /// <summary>
    /// Middleware that authenticates SNMP requests using versioned membership providers.
    /// Mirrors the authentication step from <see cref="SnmpApplication.Process"/>.
    /// </summary>
    /// <remarks>
    /// For v1/v2c: checks community string via the membership provider.
    /// For v3: calls <see cref="ISnmpContext.HandleMembership"/> which performs
    /// USM authentication, discovery, engine ID validation, etc.
    /// 
    /// Short-circuits the pipeline when:
    /// - Authentication fails (no response is sent for v1/v2c; v3 may have an error REPORT).
    /// - A response is already set after authentication (v3 discovery REPORT).
    /// </remarks>
    public sealed class AuthenticationMiddleware : ISnmpMiddleware
    {
        private readonly IMembershipProvider _membership;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="membership">The membership provider (typically a <see cref="ComposedMembershipProvider"/>).</param>
        public AuthenticationMiddleware(IMembershipProvider membership)
        {
            _membership = membership ?? throw new ArgumentNullException(nameof(membership));
        }

        /// <inheritdoc />
        public async Task InvokeAsync(SnmpMessageContext context, Func<SnmpMessageContext, Task> next)
        {
            // Authenticate the request through the membership provider chain.
            // For v1/v2c this checks community strings.
            // For v3 this calls SecureSnmpContext.HandleMembership() which handles
            // discovery, user lookup, engine ID validation, etc.
            if (!_membership.AuthenticateRequest(context.SnmpContext))
            {
                // Authentication failed. For v3, the context may have set a REPORT response
                // (e.g., unknownSecurityName). That response will be sent by the engine.
                return;
            }

            // Authentication succeeded, but the context may already have a response
            // (e.g., v3 discovery REPORT). If so, short-circuit — the response will be sent.
            if (context.SnmpContext.Response != null)
            {
                return;
            }

            // Proceed to the next middleware (handler execution).
            await next(context);
        }
    }
}
