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
    public class TrapV2MessageHandlerTestFixture
    {
        [Fact]
        public void Test()
        {
            // Create substitutes for the interfaces
            var substitute = Substitute.For<ISnmpContext>();
            var substitute2 = Substitute.For<IListenerBinding>();

            // Create the list of variables and the message
            IList<Variable> v = new List<Variable>();
            var message = new TrapV2Message(0, VersionCode.V2, new OctetString("community"), new ObjectIdentifier("1.3.6"), 0, v);

            // Set up the substitute to return specific values when its properties are accessed
            substitute.Binding.Returns(substitute2);
            substitute.Request.Returns(message);
            substitute.Sender.Returns(new IPEndPoint(IPAddress.Any, 0));

            // Set up the substitute to do something when a method is called
            substitute.When(x => x.CopyRequest(ErrorCode.NoError, 0)).Do(x => { /* this must be called */ });

            // Create the handler and test it
            var handler = new TrapV2MessageHandler();
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(substitute, null));
            handler.MessageReceived += delegate(object args, TrapV2MessageReceivedEventArgs e)
            {
                Assert.Equal(substitute2, e.Binding);
                Assert.Equal(message, e.TrapV2Message);
                Assert.True(new IPEndPoint(IPAddress.Any, 0).Equals(e.Sender));
            };
            handler.Handle(substitute, new ObjectStore());
        }
    }
}
