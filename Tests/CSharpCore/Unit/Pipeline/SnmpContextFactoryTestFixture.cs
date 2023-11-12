﻿using Lextm.SharpSnmpLib.Messaging;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using Moq;
using System.Net;
using Xunit;
using IListenerBinding = Samples.Pipeline.IListenerBinding;
using Lextm.SharpSnmpLib;

namespace Samples.Unit.Pipeline
{
    public class SnmpContextFactoryTestFixture
    {
        [Fact]
        public void Test()
        {
            var messageMock = new Mock<ISnmpMessage>();
            messageMock.Setup(foo => foo.Version).Returns(VersionCode.V3);
            var bindingMock = new Mock<IListenerBinding>();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var context = SnmpContextFactory.Create(messageMock.Object, new IPEndPoint(IPAddress.Loopback, 0), new UserRegistry(),
                                      new EngineGroup(engineId),
                                      bindingMock.Object);
            context.SendResponse();
            bindingMock.Verify(foo => foo.SendResponse(It.IsAny<ISnmpMessage>(), It.IsAny<EndPoint>()), Times.AtMostOnce);
        }
    }
}
