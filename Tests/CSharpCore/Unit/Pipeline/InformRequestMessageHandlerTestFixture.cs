/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/6
 * Time: 17:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using Lextm.SharpSnmpLib.Messaging;
using Samples.Pipeline;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;
using IListenerBinding = Samples.Pipeline.IListenerBinding;
using Lextm.SharpSnmpLib;
using NSubstitute;

namespace Samples.Unit.Pipeline
{
    public class InformRequestMessageHandlerTestFixture
    {
        [Fact]
        public void Test()
        {
            var substitute = Substitute.For<ISnmpContext>();
            var substitute2 = Substitute.For<IListenerBinding>();
            IList<Variable> v = new List<Variable>();
            var message = new InformRequestMessage(0, VersionCode.V1, new OctetString("community"), new ObjectIdentifier("1.3.6"), 0, v);
            substitute.Binding.Returns(substitute2);
            substitute.Request.Returns(message);
            substitute.Sender.Returns(new IPEndPoint(IPAddress.Any, 0));

            substitute.When(x => x.CopyRequest(ErrorCode.NoError, 0)).Do(x => { /* this must be called */ });

            var handler = new InformRequestMessageHandler();
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(substitute, null));

            handler.MessageReceived += delegate(object args, InformRequestMessageReceivedEventArgs e)
            {
                Assert.Equal(substitute2, e.Binding);
                Assert.Equal(message, e.InformRequestMessage);
                Assert.True(new IPEndPoint(IPAddress.Any, 0).Equals(e.Sender));
            };

            handler.Handle(substitute, new ObjectStore());
        }
    }
}
