﻿using System;
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
    public class GetNextMessageHandlerTestFixture
    {
        [Fact]
        public void NoError()
        {
            var handler = new GetNextMessageHandler();
            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
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
            store.Add(new SysObjectId());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"), noerror.Variables()[0].Id);
        }

        [Fact]
        public void GenError()
        {
            var handler = new GetNextMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0")).Returns(substitute);
            substitute.MatchGetNext(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")).Returns(substitute);

            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(substitute);

            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
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
            handler.Handle(context, store);
            var genError = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.GenError, genError.ErrorStatus);
        }

        [Fact]
        public void EndOfMibView()
        {
            var handler = new GetNextMessageHandler();
            var store = new ObjectStore();
            store.Add(new SysDescr());
            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
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
            var endOfMibView = (ResponseMessage)context.Response;
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"), endOfMibView.Variables()[0].Id);
            Assert.Equal(new EndOfMibView(), endOfMibView.Variables()[0].Data);
        }
    }
}
