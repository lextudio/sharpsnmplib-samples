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
    public class SetMessageHandlerTestFixture
    {
        [Fact]
        public void WrongType()
        {
            var handler = new SetMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0")).Returns(substitute);

            // NSubstitute does not have a direct equivalent to Moq's SetupSet for throwing exceptions.
            // You can use When..Do to achieve similar behavior.
            substitute.When(x => x.Data = Arg.Is<Integer32>(val => val.ToInt32() == 400))
                     .Do(x => { throw new ArgumentException(); });

            var store = new ObjectStore();
            store.Add(substitute);

            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new Integer32(400))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var wrongType = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.WrongType, wrongType.ErrorStatus);
        }

        [Fact]
        public void NoAccess()
        {
            var handler = new SetMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0")).Returns(substitute);

            // NSubstitute does not have a direct equivalent to Moq's SetupSet for throwing exceptions.
            // You can use When..Do to achieve similar behavior.
            substitute.When(x => x.Data = Arg.Is<OctetString>(val => val.Equals(new OctetString("test"))))
                     .Do(x => { throw new AccessFailureException(); });

            var store = new ObjectStore();
            store.Add(substitute);

            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var noAccess = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoAccess, noAccess.ErrorStatus);
        }

        [Fact]
        public void GenError()
        {
            var handler = new SetMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0")).Returns(substitute);

            // NSubstitute does not have a direct equivalent to Moq's SetupSet for throwing exceptions.
            // You would typically handle this logic in your test or by using a When..Do construct.
            substitute.When(x => x.Data = Arg.Any<OctetString>()).Do(x => { throw new Exception(); });

            var store = new ObjectStore();
            store.Add(substitute);

            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
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

        [Fact]
        public void NoError()
        {
            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            store.Add(new SysContact());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.Equal(new OctetString("test"), noerror.Variables()[0].Data);
        }

        [Fact]
        public void NotWritable()
        {
            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            handler.Handle(context, store);
            var notWritable = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NotWritable, notWritable.ErrorStatus);
        }
        
        [Fact]
        public void TooBig()
        {
            var list = new List<Variable>();
            for (int i = 0; i < 5000; i++)
            {
                list.Add(new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test")));
            }

            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    list
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            handler.Handle(context, store);
            var notWritable = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.TooBig, notWritable.ErrorStatus);
        }
    }
}
