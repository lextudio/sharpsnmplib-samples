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
    public class GetBulkMessageHandlerTestFixture
    {       
        [Fact]
        public void NoErrorNonRepeater0()
        {
            var handler = new GetBulkMessageHandler();
            var context = SnmpContextFactory.Create(
                new GetBulkRequestMessage(
                    300,
                    VersionCode.V2,
                    new OctetString("lextm"),
                    0,
                    2,
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
            store.Add(new SysUpTime());
            store.Add(new SysContact());
            store.Add(new SysName());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"), noerror.Variables()[0].Id);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"), noerror.Variables()[1].Id);
            Assert.Equal(2, noerror.Variables().Count);
        }

        [Fact]
        public void GenErrorNonRepeater0()
        {
            var handler = new GetBulkMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0")).Returns(substitute);
            substitute.MatchGetNext(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")).Returns(substitute);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(substitute);
            var context = SnmpContextFactory.Create(
                new GetBulkRequestMessage(
                    300,
                    VersionCode.V2,
                    new OctetString("lextm"),
                    0,
                    2,
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
        public void EndOfMibViewNonRepeater0()
        {
            var handler = new GetBulkMessageHandler();
            var store = new ObjectStore();
            store.Add(new SysDescr());
            var context = SnmpContextFactory.Create(
                new GetBulkRequestMessage(
                    300,
                    VersionCode.V2,
                    new OctetString("lextm"),
                    0,
                    2,
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

        [Fact]
        public void NoErrorNonRepeater1()
        {
            var handler = new GetBulkMessageHandler();
            var context = SnmpContextFactory.Create(
                new GetBulkRequestMessage(
                    300,
                    VersionCode.V2,
                    new OctetString("lextm"),
                    1,
                    2,
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")),
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(new SysObjectId());
            store.Add(new SysUpTime());
            store.Add(new SysContact());
            store.Add(new SysName());
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.Equal(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"), noerror.Variables()[0].Id);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), noerror.Variables()[1].Id);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.1.5.0"), noerror.Variables()[2].Id);
            Assert.Equal(3, noerror.Variables().Count);
        }
        
        [Fact]
        public void GenErrorNonRepeater1()
        {
            var handler = new GetBulkMessageHandler();
            var substitute = Substitute.For<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            substitute.Data.Returns(x => { throw new Exception(); });
            substitute.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0")).Returns(substitute);
            substitute.MatchGetNext(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")).Returns(substitute);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(substitute);

            var context = SnmpContextFactory.Create(
                new GetBulkRequestMessage(
                    300,
                    VersionCode.V2,
                    new OctetString("lextm"),
                    1,
                    2,
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
    }
}
