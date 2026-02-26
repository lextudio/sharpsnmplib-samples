// SNMP middleware interface.
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
    /// Middleware interface for the SNMP processing pipeline.
    /// Each middleware receives a context and a delegate to the next middleware.
    /// It may short-circuit the pipeline by not calling <paramref name="next"/>.
    /// </summary>
    public interface ISnmpMiddleware
    {
        /// <summary>
        /// Processes the SNMP message context.
        /// </summary>
        /// <param name="context">The SNMP message context.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        Task InvokeAsync(SnmpMessageContext context, Func<SnmpMessageContext, Task> next);
    }
}
