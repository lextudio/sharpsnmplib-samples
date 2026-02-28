using Lextm.SharpSnmpLib.Messaging;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using System.Net;
using Xunit;
using IListenerBinding = Samples.Pipeline.IListenerBinding;
using Lextm.SharpSnmpLib;
using NSubstitute;

namespace Samples.Unit.Pipeline
{
    public class SnmpContextFactoryTestFixture
    {
        [Fact]
        public void Test()
        {
            var messageSubstitute = Substitute.For<ISnmpMessage>();
            messageSubstitute.Version.Returns(VersionCode.V3);
            var bindingSubstitute = Substitute.For<IListenerBinding>();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var context = SnmpContextFactory.Create(messageSubstitute, new IPEndPoint(IPAddress.Loopback, 0), new UserRegistry(),
                                          new EngineGroup(engineId),
                                          bindingSubstitute);
            context.SendResponse();
            bindingSubstitute.Received(0).SendResponse(Arg.Any<ISnmpMessage>(), Arg.Any<EndPoint>());
        }
    }
}
