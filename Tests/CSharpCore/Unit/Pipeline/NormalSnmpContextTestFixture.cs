﻿using Lextm.SharpSnmpLib.Messaging;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using System.Collections.Generic;
using System.Net;
using Xunit;
using IListenerBinding = Samples.Pipeline.IListenerBinding;
using Lextm.SharpSnmpLib;
using NSubstitute;

namespace Samples.Unit.Pipeline
{
    public class NormalSnmpContextTestFixture
    {
        [Fact]
        public void Test()
        {
            var message = new GetRequestMessage(0, VersionCode.V1, new OctetString("public"), new List<Variable>());
            var bindingSubstitute = Substitute.For<IListenerBinding>();
            var context = new NormalSnmpContext(message, new IPEndPoint(IPAddress.Loopback, 0),
                                                new UserRegistry(), bindingSubstitute);
            context.GenerateResponse(new List<Variable>());
            Assert.NotNull(context.Response);
            context.SendResponse();
            Assert.False(context.HandleMembership());

            var list = new List<Variable>();
            for (int i = 0; i < 5000; i++)
            {
                list.Add(new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test")));
            }

            context.GenerateResponse(list);
            Assert.Equal(ErrorCode.TooBig, context.Response.Pdu().ErrorStatus.ToErrorCode());
            bindingSubstitute.Received(1).SendResponse(Arg.Any<ISnmpMessage>(), Arg.Any<EndPoint>());
        }
    }
}
