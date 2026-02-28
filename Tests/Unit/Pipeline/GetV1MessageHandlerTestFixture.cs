using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Samples.Objects;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using Xunit;
using Lextm.SharpSnmpLib;
using NSubstitute;

namespace Samples.Unit.Pipeline
{
    public class GetV1MessageHandlerTestFixture
    {
        [Fact]
        public void NoSuchName()
        {
            var handler = new GetV1MessageHandler();
            var context = SnmpContextFactory.Create(
                new GetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            handler.Handle(context, store);
            var noSuchName = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoSuchName, noSuchName.ErrorStatus);
        }

        [Fact]
        public void NoError()
        {
            var handler = new GetV1MessageHandler();
            var context = SnmpContextFactory.Create(
                new GetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoError, noerror.ErrorStatus);
        }
        
        [Fact]
        public void NoSuchName2()
        {
            var handler = new GetV1MessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            substitute.Data.Returns(x => throw new AccessFailureException());
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0")).Returns(substitute);
            var store = new ObjectStore();
            store.Add(substitute);
            var context = SnmpContextFactory.Create(
                new GetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var genError = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoSuchName, genError.ErrorStatus);
        }

        [Fact]
        public void GenError()
        {
            var handler = new GetV1MessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            substitute.Data.Returns(x => throw new Exception());
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0")).Returns(substitute);
            var store = new ObjectStore();
            store.Add(substitute);
            var context = SnmpContextFactory.Create(
                new GetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var genError = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.GenError, genError.ErrorStatus);
        }
    }
}
