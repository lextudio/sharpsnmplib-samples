// SNMP message handler middleware.
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

namespace Samples.Pipeline
{
    /// <summary>
    /// Terminal middleware that processes SNMP PDUs and produces responses when required.
    /// </summary>
    public sealed class MessageHandlerMiddleware : ISnmpMiddleware
    {
        private readonly ObjectStore _store;
        private readonly Action<TrapV1MessageReceivedEventArgs> _trapV1Received;
        private readonly Action<TrapV2MessageReceivedEventArgs> _trapV2Received;
        private readonly Action<InformRequestMessageReceivedEventArgs> _informReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="store">The object store (MIB).</param>
        /// <param name="trapV1Received">Optional callback for trap v1 messages.</param>
        /// <param name="trapV2Received">Optional callback for trap v2 messages.</param>
        /// <param name="informReceived">Optional callback for inform messages.</param>
        public MessageHandlerMiddleware(
            ObjectStore store,
            Action<TrapV1MessageReceivedEventArgs> trapV1Received = null,
            Action<TrapV2MessageReceivedEventArgs> trapV2Received = null,
            Action<InformRequestMessageReceivedEventArgs> informReceived = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _trapV1Received = trapV1Received;
            _trapV2Received = trapV2Received;
            _informReceived = informReceived;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(SnmpMessageContext context, Func<SnmpMessageContext, Task> next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var snmpContext = context.SnmpContext;
            if (snmpContext == null)
            {
                throw new ArgumentException("SNMP context must be initialized before message processing.", nameof(context));
            }

            var request = snmpContext.Request;
            var pduType = request.Pdu().TypeCode;

            switch (pduType)
            {
                case SnmpType.GetRequestPdu:
                    if (request.Version == VersionCode.V1)
                    {
                        HandleGetV1(snmpContext);
                    }
                    else
                    {
                        HandleGet(snmpContext);
                    }

                    break;

                case SnmpType.GetNextRequestPdu:
                    if (request.Version == VersionCode.V1)
                    {
                        HandleGetNextV1(snmpContext);
                    }
                    else
                    {
                        HandleGetNext(snmpContext);
                    }

                    break;

                case SnmpType.GetBulkRequestPdu:
                    HandleGetBulk(snmpContext);
                    break;

                case SnmpType.SetRequestPdu:
                    if (request.Version == VersionCode.V1)
                    {
                        HandleSetV1(snmpContext);
                    }
                    else
                    {
                        HandleSet(snmpContext);
                    }

                    break;

                case SnmpType.TrapV1Pdu:
                    _trapV1Received?.Invoke(new TrapV1MessageReceivedEventArgs(
                        snmpContext.Sender,
                        (TrapV1Message)request,
                        snmpContext.Binding));
                    break;

                case SnmpType.TrapV2Pdu:
                    _trapV2Received?.Invoke(new TrapV2MessageReceivedEventArgs(
                        snmpContext.Sender,
                        (TrapV2Message)request,
                        snmpContext.Binding));
                    break;

                case SnmpType.InformRequestPdu:
                    _informReceived?.Invoke(new InformRequestMessageReceivedEventArgs(
                        snmpContext.Sender,
                        (InformRequestMessage)request,
                        snmpContext.Binding));
                    snmpContext.CopyRequest(ErrorCode.NoError, 0);
                    break;
            }

            await Task.CompletedTask;
        }

        private void HandleGet(ISnmpContext context)
        {
            var index = 0;
            IList<Variable> result = new List<Variable>();
            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                try
                {
                    var obj = _store.GetObject(variable.Id);
                    if (obj == null)
                    {
                        result.Add(new Variable(variable.Id, new NoSuchInstance()));
                    }
                    else
                    {
                        result.Add(obj.Variable);
                    }
                }
                catch (AccessFailureException)
                {
                    result.Add(new Variable(variable.Id, new NoSuchObject()));
                }
                catch (Exception)
                {
                    context.CopyRequest(ErrorCode.GenError, index);
                    return;
                }
            }

            context.GenerateResponse(result);
        }

        private void HandleGetV1(ISnmpContext context)
        {
            var status = ErrorCode.NoError;
            var index = 0;
            IList<Variable> result = new List<Variable>();
            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                var obj = _store.GetObject(variable.Id);
                if (obj != null)
                {
                    try
                    {
                        result.Add(obj.Variable);
                    }
                    catch (AccessFailureException)
                    {
                        status = ErrorCode.NoSuchName;
                    }
                    catch (Exception)
                    {
                        context.CopyRequest(ErrorCode.GenError, index);
                        return;
                    }
                }
                else
                {
                    status = ErrorCode.NoSuchName;
                }

                if (status != ErrorCode.NoError)
                {
                    context.CopyRequest(status, index);
                    return;
                }
            }

            context.GenerateResponse(result);
        }

        private void HandleGetNext(ISnmpContext context)
        {
            var index = 0;
            IList<Variable> result = new List<Variable>();
            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                try
                {
                    var next = _store.GetNextObject(variable.Id);
                    result.Add(next == null ? new Variable(variable.Id, new EndOfMibView()) : next.Variable);
                }
                catch (Exception)
                {
                    context.CopyRequest(ErrorCode.GenError, index);
                    return;
                }
            }

            context.GenerateResponse(result);
        }

        private void HandleGetNextV1(ISnmpContext context)
        {
            var status = ErrorCode.NoError;
            var index = 0;
            IList<Variable> result = new List<Variable>();
            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                try
                {
                    var next = _store.GetNextObject(variable.Id);
                    if (next == null)
                    {
                        status = ErrorCode.NoSuchName;
                    }
                    else
                    {
                        result.Add(next.Variable);
                    }
                }
                catch (Exception)
                {
                    context.CopyRequest(ErrorCode.GenError, index);
                    return;
                }

                if (status != ErrorCode.NoError)
                {
                    context.CopyRequest(status, index);
                    return;
                }
            }

            context.GenerateResponse(result);
        }

        private void HandleGetBulk(ISnmpContext context)
        {
            var pdu = context.Request.Pdu();
            IList<Variable> result = new List<Variable>();
            var index = 0;
            var nonRepeaters = pdu.ErrorStatus.ToInt32();
            var variables = pdu.Variables;

            if (nonRepeaters > variables.Count)
            {
                nonRepeaters = variables.Count;
            }

            for (var i = 0; i < nonRepeaters; i++)
            {
                var variable = variables[i];
                index++;
                try
                {
                    var next = _store.GetNextObject(variable.Id);
                    result.Add(next == null ? new Variable(variable.Id, new EndOfMibView()) : next.Variable);
                }
                catch (Exception)
                {
                    context.CopyRequest(ErrorCode.GenError, index);
                    return;
                }
            }

            for (var j = nonRepeaters; j < variables.Count; j++)
            {
                var variable = variables[j];
                index++;
                var temp = variable;
                var repetition = pdu.ErrorIndex.ToInt32();
                while (repetition-- > 0)
                {
                    try
                    {
                        var next = _store.GetNextObject(temp.Id);
                        if (next == null)
                        {
                            temp = new Variable(temp.Id, new EndOfMibView());
                            result.Add(temp);
                            break;
                        }

                        result.Add(next.Variable);
                        temp = next.Variable;
                    }
                    catch (Exception)
                    {
                        context.CopyRequest(ErrorCode.GenError, index);
                        return;
                    }
                }
            }

            context.GenerateResponse(result);
        }

        private void HandleSet(ISnmpContext context)
        {
            context.CopyRequest(ErrorCode.InconsistentName, int.MaxValue);
            if (context.TooBig)
            {
                context.GenerateTooBig();
                return;
            }

            var index = 0;
            var status = ErrorCode.NoError;
            IList<Variable> result = new List<Variable>();

            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                var obj = _store.GetObject(variable.Id);
                if (obj != null)
                {
                    try
                    {
                        obj.Data = variable.Data;
                    }
                    catch (AccessFailureException)
                    {
                        status = ErrorCode.NoAccess;
                    }
                    catch (ArgumentException ex)
                    {
                        if (!Enum.TryParse(ex.Message, out status) || status == ErrorCode.NoError)
                        {
                            status = ErrorCode.WrongType;
                        }
                    }
                    catch (Exception)
                    {
                        status = ErrorCode.GenError;
                    }
                }
                else
                {
                    status = ErrorCode.NotWritable;
                }

                if (status != ErrorCode.NoError)
                {
                    context.CopyRequest(status, index);
                    return;
                }

                result.Add(variable);
            }

            context.GenerateResponse(result);
        }

        private void HandleSetV1(ISnmpContext context)
        {
            var index = 0;
            var status = ErrorCode.NoError;
            IList<Variable> result = new List<Variable>();

            foreach (var variable in context.Request.Pdu().Variables)
            {
                index++;
                var obj = _store.GetObject(variable.Id);
                if (obj != null)
                {
                    try
                    {
                        obj.Data = variable.Data;
                    }
                    catch (AccessFailureException)
                    {
                        status = ErrorCode.NoSuchName;
                    }
                    catch (ArgumentException)
                    {
                        status = ErrorCode.BadValue;
                    }
                    catch (Exception)
                    {
                        status = ErrorCode.GenError;
                    }
                }
                else
                {
                    status = ErrorCode.NoSuchName;
                }

                if (status != ErrorCode.NoError)
                {
                    context.CopyRequest(status, index);
                    return;
                }

                result.Add(variable);
            }

            context.GenerateResponse(result);
        }
    }
}
