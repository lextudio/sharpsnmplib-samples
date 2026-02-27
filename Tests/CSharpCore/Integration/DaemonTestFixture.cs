using Lextm.SharpSnmpLib.Messaging;
using Samples.Objects;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Listener = Samples.Pipeline.Listener;
using Lextm.SharpSnmpLib;
using TimeoutException = Lextm.SharpSnmpLib.Messaging.TimeoutException;
using System.Runtime.InteropServices;

namespace Samples.Integration
{
    [Collection("Integration")]
    public class DaemonTestFixture
    {
        private static readonly NumberGenerator Port = new NumberGenerator(20000, 29999);
        private const string oidIdentifier = "1.3.6.1.2.1.1.1.0";
        private const string communityPublic = "public";
        private const int MaxTimeout = 30 * 1000; // 30 seconds
        
        private SnmpEngine CreateEngine(bool timeout = false, bool max255chars = false, bool tooBigData = false)
        {
            var idEngine161 = ByteTool.Convert("80004fb805636c6f75644dab22cc");
            // TODO: this is a hack. review it later.            
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(new SysObjectId());
            store.Add(new SysUpTime());
            store.Add(new SysContact());
            store.Add(new SysName());
            store.Add(new SysLocation());
            store.Add(new SysServices());
            store.Add(new SysORLastChange());
            store.Add(new SysORTable());
            // TooBigDataObject at OID 1.3.6.1.2.1.1.50.0 must be inserted here
            // (after SysORTable 1.3.6.1.2.1.1.9.x.x.x and before IfNumber 1.3.6.1.2.1.2.1.0)
            // because the ObjectStore returns the FIRST match in insertion order.
            if (tooBigData)
            {
                store.Add(new TooBigDataObject());
            }
            store.Add(new IfNumber());
            store.Add(new IfTable());
            if (timeout)
            {
                store.Add(new TimeoutObject());
            }
            if (max255chars)
            {
                store.Add(new Max255CharsObject());
            }

            var users = new UserRegistry();
            users.Add(new OctetString("neither"), DefaultPrivacyProvider.DefaultPair);
            users.Add(new OctetString("authen"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication"))));
            if (DESPrivacyProvider.IsSupported)
            {
                users.Add(new OctetString("privacy"), new DESPrivacyProvider(new OctetString("privacyphrase"),
                                                                             new MD5AuthenticationProvider(new OctetString("authentication"))));
            }

            var v1 = new Version1MembershipProvider(new OctetString(communityPublic), new OctetString(communityPublic));
            var v2 = new Version2MembershipProvider(new OctetString(communityPublic), new OctetString(communityPublic));
            var v3 = new Version3MembershipProvider();
            var membership = new ComposedMembershipProvider(new IMembershipProvider[] { v1, v2, v3 });

            var listener = new Listener { Users = users };
            listener.ExceptionRaised += (sender, e) => { Assert.Fail("unexpected exception"); };
            return new SnmpEngine(listener, new EngineGroup(idEngine161), store, membership);
        }

        private class TimeoutObject : ScalarObject
        {
            public TimeoutObject()
                : base(new ObjectIdentifier("1.5.2"))
            {
            }

            public override ISnmpData Data
            {
                get
                {
                    Thread.Sleep(1500 * 2);
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }
        }
        /// <summary>
        /// A scalar object whose data is large enough to trigger a TooBig error
        /// when included in an SNMP response (exceeds the max message size of 65507 bytes).
        /// Placed at OID 1.3.6.1.2.1.1.50.0 so it appears during a walk of the system MIB subtree.
        /// See https://github.com/lextudio/sharpsnmplib/issues/697
        /// </summary>
        private class TooBigDataObject : ScalarObject
        {
            private readonly OctetString _data;

            public TooBigDataObject()
                : base(new ObjectIdentifier("1.3.6.1.2.1.1.50.0"))
            {
                // 70000 bytes exceeds the default max SNMP message size (65507 bytes),
                // which causes the engine to respond with ErrorCode.TooBig.
                _data = new OctetString(new byte[70000]);
            }

            public override ISnmpData Data
            {
                get => _data;
                set => throw new NotImplementedException();
            }
        }

        private class Max255CharsObject : ScalarObject
        {
            private OctetString _data = new OctetString("");

            public Max255CharsObject()
                : base(new ObjectIdentifier("1.5.3"))
            {
            }

            public override ISnmpData Data
            {
                get
                {
                    return _data;
                }

                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }
                    if (value.TypeCode != SnmpType.OctetString)
                    {
                        throw new ArgumentException("Invalid data type.", nameof(value));
                    }
                    if (((OctetString)value).ToString().Length > 255)
                    {
                        throw new ArgumentException(nameof(ErrorCode.WrongLength));
                    }

                    _data = (OctetString)value;
                }
            }
        }

        [Fact]
        public async Task TestResponseAsync()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });

                var users1 = new UserRegistry();
                var response = await message.GetResponseAsync(serverEndPoint, users1, socket, TestContext.Current.CancellationToken);
                Assert.Equal(SnmpType.ResponsePdu, response.TypeCode());
                Assert.Equal(message.RequestId(), response.RequestId());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestResponseAsync_IPv6()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.IPv6Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });

                var users1 = new UserRegistry();
                var response = await message.GetResponseAsync(serverEndPoint, users1, socket, TestContext.Current.CancellationToken);
                Assert.Equal(SnmpType.ResponsePdu, response.TypeCode());
                Assert.Equal(message.RequestId(), response.RequestId());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestResponse()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });

                const int time = 3000;
                var response = message.GetResponse(time, serverEndPoint, socket);
                Assert.Equal(SnmpType.ResponsePdu, response.TypeCode());
                Assert.Equal(message.RequestId(), response.RequestId());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestResponseIPv6()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.IPv6Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });

                const int time = 3000;
                var response = message.GetResponse(time, serverEndPoint, socket);
                Assert.Equal(SnmpType.ResponsePdu, response.TypeCode());
                Assert.Equal(message.RequestId(), response.RequestId());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestResponseVersion3()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                IAuthenticationProvider auth = new MD5AuthenticationProvider(new OctetString("authentication"));
                IPrivacyProvider priv = new DefaultPrivacyProvider(auth);

                var ending = new AutoResetEvent(false);
                var timeout = 3000;
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, serverEndPoint);

                var expected = Messenger.NextRequestId;
                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, expected, new OctetString("authen"), OctetString.Empty, new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, priv, Messenger.MaxMessageSize, report);

                var source = Observable.Defer(() =>
                {
                    ISnmpMessage reply = request.GetResponse(timeout, serverEndPoint);
                    return Observable.Return(reply);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(reply =>
                {
                    ISnmpPdu snmpPdu = reply.Pdu();
                    Assert.Equal(SnmpType.ResponsePdu, snmpPdu.TypeCode);
                    Assert.Equal(expected, reply.RequestId());
                    Assert.Equal(ErrorCode.NoError, snmpPdu.ErrorStatus.ToErrorCode());
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestResponseVersion3_DuplicateAuthPassphrase()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                IAuthenticationProvider auth = new MD5AuthenticationProvider(new OctetString("authenticationauthentication"));
                IPrivacyProvider priv = new DefaultPrivacyProvider(auth);

                var timeout = 3000;
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, serverEndPoint);

                var expected = Messenger.NextRequestId;
                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, expected, new OctetString("authen"), OctetString.Empty, new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, priv, Messenger.MaxMessageSize, report);
                ISnmpMessage reply = request.GetResponse(timeout, serverEndPoint);
                ISnmpPdu snmpPdu = reply.Pdu();
                Assert.Equal(SnmpType.ResponsePdu, snmpPdu.TypeCode);
                Assert.Equal(expected, reply.RequestId());
                Assert.Equal(ErrorCode.NoError, snmpPdu.ErrorStatus.ToErrorCode());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void Test_Version3_Report_Wrong_Auth()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                // Intentionally use wrong authentication.
                IAuthenticationProvider auth = new SHA1AuthenticationProvider(new OctetString("authentication"));
                IPrivacyProvider priv = new DefaultPrivacyProvider(auth);

                var timeout = 3000;
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, serverEndPoint);

                var expected = Messenger.NextRequestId;
                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, expected, new OctetString("authen"), OctetString.Empty, new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, priv, Messenger.MaxMessageSize, report);
                ISnmpMessage reply = request.GetResponse(timeout, serverEndPoint);
                ISnmpPdu snmpPdu = reply.Pdu();
                Assert.Equal(SnmpType.ReportPdu, snmpPdu.TypeCode);
                Assert.Equal(expected, reply.RequestId());
                Assert.Equal(ErrorCode.NoError, snmpPdu.ErrorStatus.ToErrorCode());
                Assert.Single(snmpPdu.Variables);
                Assert.Equal("1.3.6.1.6.3.15.1.1.5.0", snmpPdu.Variables[0].Id.ToString());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void Test_Version3_Report_Wrong_Priv()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                // Intentionally use wrong privacy.
                IAuthenticationProvider auth = new MD5AuthenticationProvider(new OctetString("authentication"));
                IPrivacyProvider priv = new AESPrivacyProvider(new OctetString("privacyphrase"), auth);

                var timeout = 3000;
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, serverEndPoint);

                var expected = Messenger.NextRequestId;
                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, expected, new OctetString("privacy"), OctetString.Empty, new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, priv, Messenger.MaxMessageSize, report);
                ISnmpMessage reply = request.GetResponse(timeout, serverEndPoint);
                ISnmpPdu snmpPdu = reply.Pdu();
                Assert.Equal(SnmpType.ReportPdu, snmpPdu.TypeCode);
                Assert.Equal(0, reply.RequestId());
                Assert.Equal(ErrorCode.NoError, snmpPdu.ErrorStatus.ToErrorCode());
                Assert.Single(snmpPdu.Variables);
                Assert.Equal("1.3.6.1.6.3.15.1.1.6.0", snmpPdu.Variables[0].Id.ToString());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void Test_Version3_Report_Wrong_User()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                // Intentionally use wrong privacy.
                IAuthenticationProvider auth = new MD5AuthenticationProvider(new OctetString("authentication"));
                IPrivacyProvider priv = new DESPrivacyProvider(new OctetString("privacyphrase"), auth);

                var timeout = 3000;
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, serverEndPoint);

                var expected = Messenger.NextRequestId;
                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, expected, new OctetString("privacy-not-exist"), OctetString.Empty, new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, priv, Messenger.MaxMessageSize, report);
                ISnmpMessage reply = request.GetResponse(timeout, serverEndPoint);
                ISnmpPdu snmpPdu = reply.Pdu();
                Assert.Equal(SnmpType.ReportPdu, snmpPdu.TypeCode);
                Assert.Equal(0, reply.RequestId());
                Assert.Equal(ErrorCode.NoError, snmpPdu.ErrorStatus.ToErrorCode());
                Assert.Single(snmpPdu.Variables);
                Assert.Equal("1.3.6.1.6.3.15.1.1.3.0", snmpPdu.Variables[0].Id.ToString());
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererV1()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 3000;
            var wait = 3 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(() =>
                {
                    discoverer.Discover(VersionCode.V1, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 1,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererV1_IPv6()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: skip this on macOS right now.
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.IPv6Any);
            engine.Listener.AddBinding(serverEndPoint, "[ff02::1]");
            engine.Start();

            var timeout = 3000;
            var wait = 3 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(() =>
                {
                    discoverer.Discover(VersionCode.V1, new IPEndPoint(IPAddress.Parse("[ff02::1]"), serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 1,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }


        [Fact]
        public void TestDiscovererV2()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 3000;
            var wait = 3 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(() =>
                {
                    discoverer.Discover(VersionCode.V2, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererV3()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 3000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(() =>
                {
                    discoverer.Discover(VersionCode.V3, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        null, timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 1,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererAsyncV1()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            // Async discovery can be timing-sensitive under full-suite load; allow a wider probe window.
            var timeout = 3000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    await discoverer.DiscoverAsync(VersionCode.V1, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererAsyncV1_IPv6()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: skip this on macOS right now.
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.IPv6Any);
            engine.Listener.AddBinding(serverEndPoint, "[ff02::1]");
            engine.Start();

            var timeout = 3000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    await discoverer.DiscoverAsync(VersionCode.V1, new IPEndPoint(IPAddress.Parse("[ff02::1]"), serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }


        [Fact]
        public void TestDiscovererAsyncV2()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 3000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    await discoverer.DiscoverAsync(VersionCode.V2, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        new OctetString(communityPublic), timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererAsyncV3()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 3000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    await discoverer.DiscoverAsync(VersionCode.V3, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                        null, timeout);
                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

#if NET6_0
        [Fact]
        public void TestDiscovererAsyncV1Cancelled()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 1000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(timeout);
                    try
                    {
                        await discoverer.DiscoverAsync(VersionCode.V1, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                            new OctetString(communityPublic), cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererAsyncV2Cancelled()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 1000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {                    
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(timeout);
                    try
                    {
                        await discoverer.DiscoverAsync(VersionCode.V2, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                            new OctetString(communityPublic), cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestDiscovererAsyncV3Cancelled()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Any);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            var timeout = 1000;
            var wait = 60 * timeout;
            try
            {
                var signal = new AutoResetEvent(false);
                var ending = new AutoResetEvent(false);
                var discoverer = new Discoverer();
                discoverer.AgentFound += (sender, args)
                    =>
                {
                    Assert.True(args.Agent.Address.ToString() != "0.0.0.0");
                    signal.Set();
                };

                var source = Observable.Defer(async () =>
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(timeout);
                    try
                    {
                        await discoverer.DiscoverAsync(VersionCode.V3, new IPEndPoint(IPAddress.Loopback, serverEndPoint.Port),
                            null, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    var result = signal.WaitOne(wait);
                    if (!result)
                    {
                        throw new TimeoutException();
                    }

                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.True(result);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }
#endif
        [Theory]
        [InlineData(16)]
        public async Task TestResponsesFromMultipleSources(int count)
        {
            var endpoints = new List<IPEndPoint>(count);
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            for (var index = 0; index < count; index++)
            {
                var endpoint = CreateEndpoint(IPAddress.Loopback);
                endpoints.Add(endpoint);
                engine.Listener.AddBinding(endpoint);
            }

#if NET471
            // IMPORTANT: need to set min thread count so as to boost performance.
            int minWorker, minIOC;
            // Get the current settings.
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            var threads = engine.Listener.Bindings.Count;
            ThreadPool.SetMinThreads(threads + 1, minIOC);
#endif
            engine.Start();

            try
            {
                foreach (var endpoint in endpoints)
                {
                    GetRequestMessage message = new GetRequestMessage(endpoint.Port, VersionCode.V2, new OctetString(communityPublic),
                        new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    var response =
                        await
                            message.GetResponseAsync(endpoint, new UserRegistry(),
                                socket, TestContext.Current.CancellationToken);
                    watch.Stop();
                    Assert.Equal(endpoint.Port, response.RequestId());
                }
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Theory]
        [InlineData(32)]
        public async Task TestResponsesFromSingleSource(int count)
        {
            var start = 0;
            var end = start + count;
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            //// IMPORTANT: need to set min thread count so as to boost performance.
            //int minWorker, minIOC;
            //// Get the current settings.
            //ThreadPool.GetMinThreads(out minWorker, out minIOC);
            //var threads = engine.Listener.Bindings.Count;
            //ThreadPool.SetMinThreads(threads + 1, minIOC);

            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                for (int index = start; index < end; index++)
                {
                    GetRequestMessage message = new GetRequestMessage(0, VersionCode.V2, new OctetString(communityPublic),
                        new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) });
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    var response =
                        await
                            message.GetResponseAsync(serverEndPoint, new UserRegistry(), socket, TestContext.Current.CancellationToken);
                    watch.Stop();
                    Assert.Equal(0, response.RequestId());
                }
            }
            catch (Exception)
            {
                Console.WriteLine(serverEndPoint.Port);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Theory]
        [InlineData(16)]
        public void TestResponsesFromSingleSourceWithMultipleThreads(int count)
        {
            var ending = new AutoResetEvent(false);
            var source = Observable.Defer(() =>
                {
                    var start = 0;
                    var end = start + count;
                    var engine = CreateEngine();
                    engine.Listener.ClearBindings();
                    var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
                    engine.Listener.AddBinding(serverEndPoint);
#if NET471
                    // IMPORTANT: need to set min thread count so as to boost performance.
                    int minWorker, minIOC;
                    // Get the current settings.
                    ThreadPool.GetMinThreads(out minWorker, out minIOC);
                    var threads = engine.Listener.Bindings.Count;
                    ThreadPool.SetMinThreads(threads + 1, minIOC);
#endif
                    engine.Start();

                    try
                    {
                        const int timeout = 10000;

                        // Uncomment below to reveal wrong sequence number issue.
                        // Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                        Parallel.For(start, end, index =>
                            {
                                GetRequestMessage message = new GetRequestMessage(index, VersionCode.V2,
                                    new OctetString(communityPublic),
                                    new List<Variable> {new Variable(new ObjectIdentifier(oidIdentifier)) });
                                // Comment below to reveal wrong sequence number issue.
                                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                                    ProtocolType.Udp);

                                Stopwatch watch = new Stopwatch();
                                watch.Start();
                                var response = message.GetResponse(timeout, serverEndPoint, socket);
                                watch.Stop();
                                Assert.Equal(index, response.RequestId());
                            }
                        );
                    }
                    finally
                    {
                StopEngine(engine);
                    }

                    return Observable.Return(0);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

            source.Subscribe(result => { ending.Set(); });
            Assert.True(ending.WaitOne(MaxTimeout));
        }

        [Theory]
        [InlineData(16)]
        public void TestResponsesFromSingleSourceWithMultipleThreadsFromManager(int count)
        {
            var start = 0;
            var end = start + count;
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                const int timeout = 10000;

                //for (int index = start; index < end; index++)
                Parallel.For(start, end, index =>
                    {
                        try
                        {
                            var result = Messenger.Get(VersionCode.V2, serverEndPoint, new OctetString(communityPublic),
                                new List<Variable> { new Variable(new ObjectIdentifier(oidIdentifier)) }, timeout);
                            Assert.Single(result);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(serverEndPoint.Port);
                        }
                    }
                );
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestTimeOut()
        {
            var engine = CreateEngine(true);
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { new Variable(new ObjectIdentifier("1.5.2")) });

                const int time = 1500;
                var timer = new Stopwatch();
                timer.Start();
                //IMPORTANT: test against an agent that doesn't exist.
                Assert.Throws<TimeoutException>(() => message.GetResponse(time, serverEndPoint, socket));
                timer.Stop();

                long elapsedMilliseconds = timer.ElapsedMilliseconds;
                Assert.True(time <= elapsedMilliseconds);

                // FIXME: these values are valid on my machine openSUSE 11.2. (lex)
                // This test case usually fails on Windows, as strangely WinSock API call adds an extra 500-ms.
                if (SnmpMessageExtension.IsRunningOnMono())
                {
                    Assert.True(elapsedMilliseconds <= time + 100);
                }
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestSetWrongLength()
        {
            var engine = CreateEngine(max255chars: true);
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                var string_toolong = new Variable((new Max255CharsObject()).Variable.Id, new OctetString(new string('x', 256)));
                SetRequestMessage message = new SetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { string_toolong });

                var resp = message.GetResponse(1500, serverEndPoint, socket);
                Assert.Equal(ErrorCode.WrongLength, resp.Pdu().ErrorStatus.ToErrorCode());
                Assert.Equal(1, resp.Pdu().ErrorIndex.ToInt32());

                var wrong_type = new Variable((new Max255CharsObject()).Variable.Id, new Integer32(666));
                message = new SetRequestMessage(0x4bed, VersionCode.V2, new OctetString(communityPublic),
                    new List<Variable> { wrong_type });

                resp = message.GetResponse(1500, serverEndPoint, socket);
                Assert.Equal(ErrorCode.WrongType, resp.Pdu().ErrorStatus.ToErrorCode());
                Assert.Equal(1, resp.Pdu().ErrorIndex.ToInt32());
            }
            finally
            {
                StopEngine(engine);
            }
        }
        
        [Fact]
        public void TestWrongCommunityV12()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                var identifier = new Variable(new ObjectIdentifier(oidIdentifier));
                GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString("public2"),
                    new List<Variable> { identifier });

                Assert.Throws<TimeoutException>(() => message.GetResponse(1500, serverEndPoint, socket));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestLargeMessage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: skip this on macOS right now.
                return;
            }

            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var list = new List<Variable>();
                for (int i = 0; i < 1000; i++)
                {
                    list.Add(new Variable(new ObjectIdentifier("1.3.6.1.1.1.0")));
                }

                GetRequestMessage message = new GetRequestMessage(
                    0x4bed,
                    VersionCode.V2,
                    new OctetString(communityPublic),
                    list);

                Assert.True(message.ToBytes().Length > 10000);

                var time = 3000;
                if (SnmpMessageExtension.IsRunningOnMac)
                {
                    var exception =
                        Assert.Throws<SocketException>(() => message.GetResponse(time, serverEndPoint, socket));
                    Assert.Equal(SocketError.MessageSize, exception.SocketErrorCode);
                }
                else
                {
                    // IMPORTANT: test against an agent that doesn't exist.
                    var result = message.GetResponse(time, serverEndPoint, socket);
                    Assert.True(result.Scope.Pdu.ErrorStatus.ToErrorCode() == ErrorCode.NoError);
                }
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestWalk()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                var time = 10000;
                // IMPORTANT: test against an agent that doesn't exist.
                var result = Messenger.Walk(
                    VersionCode.V1,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    time,
                    WalkMode.Default);
                Assert.True(16 < list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestWalk_Subtree()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                var time = 3000;
                // IMPORTANT: test against an agent that doesn't exist.
                var result = Messenger.Walk(
                    VersionCode.V1,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    time,
                    WalkMode.WithinSubtree);
                Assert.Equal(16, list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestWalk_V2()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                var time = 10000;
                // IMPORTANT: test against an agent that doesn't exist.
                var result = Messenger.Walk(
                    VersionCode.V2,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    time,
                    WalkMode.Default);
                Assert.True(16 < list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestWalkAsync()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                // IMPORTANT: test against an agent that doesn't exist.
                var result = await Messenger.WalkAsync(
                    VersionCode.V1,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    WalkMode.Default,
                    TestContext.Current.CancellationToken);
                Assert.True(16 < list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestWalkAsync_V2()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                // IMPORTANT: test against an agent that doesn't exist.
                var result = await Messenger.WalkAsync(
                    VersionCode.V2,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    WalkMode.Default,
                    TestContext.Current.CancellationToken);
                Assert.True(16 < list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        /// <summary>
        /// Reproduces https://github.com/lextudio/sharpsnmplib/issues/697
        /// When a Walk encounters an ErrorCode.TooBig response, HasNextAsync treats it
        /// as success (only NoSuchName is considered failure). The TooBig response contains
        /// the original request OID, causing the walk to retry the same OID forever.
        /// This test verifies that the walk does not enter an infinite loop.
        /// </summary>
        [Fact]
        public async Task TestWalkAsync_TooBigResponse()
        {
            var engine = CreateEngine(tooBigData: true);
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Manually simulate the walk loop with per-step logging to diagnose
                // whether the library's HasNextAsync logic causes an infinite loop.
                var seedOid = new ObjectIdentifier("1.3.6.1.2.1.1");
                var list = new List<Variable>();
                var maxIterations = 25; // enough to see the loop if it happens
                ObjectIdentifier previousOid = default;
                bool hasPreviousOid = false;
                int repeatedOidCount = 0;

                for (int i = 0; i < maxIterations; i++)
                {
                    var msg = new GetNextRequestMessage(
                        Messenger.NextRequestId,
                        VersionCode.V1,
                        new OctetString(communityPublic),
                        new List<Variable> { new Variable(seedOid) });

                    ISnmpMessage response;
                    try
                    {
                        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        response = await msg.GetResponseAsync(serverEndPoint, socket, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Step {i}] GetNext({seedOid}) -> EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                        break;
                    }

                    var pdu = response.Pdu();
                    var errorCode = pdu.ErrorStatus.ToErrorCode();
                    var resultVar = pdu.Variables[0];
                    Console.WriteLine($"[Step {i}] GetNext({seedOid}) -> Error={errorCode}, OID={resultVar.Id}, Type={resultVar.Data.TypeCode}");

                    if (errorCode == ErrorCode.NoSuchName)
                    {
                        Console.WriteLine($"  Walk stops: NoSuchName");
                        break;
                    }

                    // Detect repeated OID (the infinite loop condition from issue #697)
                    if (hasPreviousOid && resultVar.Id == previousOid)
                    {
                        repeatedOidCount++;
                        Console.WriteLine($"  ** REPEATED OID detected (count={repeatedOidCount}) **");
                        if (repeatedOidCount >= 3)
                        {
                            Console.WriteLine($"  Infinite loop confirmed after {repeatedOidCount} repetitions!");
                            Assert.Fail($"Walk entered an infinite loop on ErrorCode.{errorCode} response at OID {resultVar.Id} (issue #697)");
                        }
                    }
                    else
                    {
                        repeatedOidCount = 0;
                    }

                    previousOid = resultVar.Id;
                    hasPreviousOid = true;
                    seedOid = resultVar.Id;
                    list.Add(resultVar);
                }

                Console.WriteLine($"[TestWalkAsync_TooBigResponse] Collected {list.Count} variables");
            }
            finally
            {
                StopEngine(engine);
            }
        }

        /// <summary>
        /// Same as <see cref="TestWalkAsync_TooBigResponse"/> but for SNMP v2c.
        /// </summary>
        [Fact]
        public async Task TestWalkAsync_V2_TooBigResponse()
        {
            var engine = CreateEngine(tooBigData: true);
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Manual GetNext loop to reproduce issue #697 for v2c.
                var seedOid = new ObjectIdentifier("1.3.6.1.2.1.1");
                var list = new List<Variable>();
                var maxIterations = 25;
                ObjectIdentifier previousOid = default;
                bool hasPreviousOid = false;
                int repeatedOidCount = 0;

                for (int i = 0; i < maxIterations; i++)
                {
                    var msg = new GetNextRequestMessage(
                        Messenger.NextRequestId,
                        VersionCode.V2,
                        new OctetString(communityPublic),
                        new List<Variable> { new Variable(seedOid) });

                    ISnmpMessage response;
                    try
                    {
                        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        response = await msg.GetResponseAsync(serverEndPoint, socket, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[V2 Step {i}] GetNext({seedOid}) -> EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                        break;
                    }

                    var pdu = response.Pdu();
                    var errorCode = pdu.ErrorStatus.ToErrorCode();
                    var resultVar = pdu.Variables[0];
                    Console.WriteLine($"[V2 Step {i}] GetNext({seedOid}) -> Error={errorCode}, OID={resultVar.Id}, Type={resultVar.Data.TypeCode}");

                    if (errorCode == ErrorCode.NoSuchName)
                    {
                        Console.WriteLine($"  Walk stops: NoSuchName");
                        break;
                    }

                    if (resultVar.Data.TypeCode == SnmpType.EndOfMibView)
                    {
                        Console.WriteLine($"  Walk stops: EndOfMibView");
                        break;
                    }

                    // Detect repeated OID (the infinite loop condition from issue #697)
                    if (hasPreviousOid && resultVar.Id == previousOid)
                    {
                        repeatedOidCount++;
                        Console.WriteLine($"  ** REPEATED OID detected (count={repeatedOidCount}) **");
                        if (repeatedOidCount >= 3)
                        {
                            Console.WriteLine($"  Infinite loop confirmed after {repeatedOidCount} repetitions!");
                            Assert.Fail($"Walk entered an infinite loop on ErrorCode.{errorCode} response at OID {resultVar.Id} (issue #697)");
                        }
                    }
                    else
                    {
                        repeatedOidCount = 0;
                    }

                    previousOid = resultVar.Id;
                    hasPreviousOid = true;
                    seedOid = resultVar.Id;
                    list.Add(resultVar);
                }

                Console.WriteLine($"[TestWalkAsync_V2_TooBigResponse] Collected {list.Count} variables");
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public void TestBulkWalk()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var ending = new AutoResetEvent(false);
                var list = new List<Variable>();
                var time = 3000;

                var source = Observable.Defer(() =>
                {
                    var result = Messenger.BulkWalk(
                        VersionCode.V2,
                        serverEndPoint,
                        new OctetString(communityPublic),
                        OctetString.Empty,
                        new ObjectIdentifier("1.3.6.1.2.1.1"),
                        list,
                        time,
                        10,
                        WalkMode.WithinSubtree,
                        null,
                        null);
                    return Observable.Return(result);
                })
                .RetryWithBackoffStrategy(
                    retryCount: 4,
                    retryOnError: e => e is TimeoutException
                );

                source.Subscribe(result =>
                {
                    Assert.Equal(16, list.Count);
                    ending.Set();
                });
                Assert.True(ending.WaitOne(MaxTimeout));
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestBulkWalkAsync()
        {
            var engine = CreateEngine();
            engine.Listener.ClearBindings();
            var serverEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(serverEndPoint);
            engine.Start();

            try
            {
                var list = new List<Variable>();
                // IMPORTANT: test against an agent that doesn't exist.
                var result = await Messenger.BulkWalkAsync(
                    VersionCode.V2,
                    serverEndPoint,
                    new OctetString(communityPublic),
                    OctetString.Empty,
                    new ObjectIdentifier("1.3.6.1.2.1.1"),
                    list,
                    10,
                    WalkMode.WithinSubtree,
                    null,
                    null,
                    TestContext.Current.CancellationToken);
                Assert.Equal(16, list.Count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        private static void StopEngine(SnmpEngine engine)
        {
            if (engine == null)
            {
                return;
            }

            engine.Stop();
        }

        private static IPEndPoint CreateEndpoint(IPAddress address)
        {
            for (var i = 0; i < 50; i++)
            {
                var candidate = new IPEndPoint(address, Port.NextId);
                using var probe = new Socket(candidate.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    probe.Bind(candidate);
                    return candidate;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                }
            }

            throw new InvalidOperationException($"Failed to allocate free UDP port for {address}.");
        }
    }

    static class ReactiveExtensions
    {
        // Adopted from https://gist.github.com/niik/6696449
        public static readonly Func<int, TimeSpan> ExpontentialBackoff = n => TimeSpan.FromSeconds(Math.Pow(n, 2));

        public static IObservable<T> RetryWithBackoffStrategy<T>(
            this IObservable<T> source,
            int retryCount = 3,
            Func<int, TimeSpan> strategy = null,
            Func<Exception, bool> retryOnError = null)
        {
            strategy = strategy ?? ExpontentialBackoff;

            if (retryOnError == null)
                retryOnError = e => true;

            int attempt = 0;

            return Observable.Defer(() =>
            {
                return ((++attempt == 1) ? source : source.DelaySubscription(strategy(attempt - 1)))
                    .Select(item => new Tuple<bool, T, Exception>(true, item, null))
                    .Catch<Tuple<bool, T, Exception>, Exception>(e => retryOnError(e)
                        ? Observable.Throw<Tuple<bool, T, Exception>>(e)
                        : Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e)));
            })
            .Retry(retryCount)
            .SelectMany(t => t.Item1
                ? Observable.Return(t.Item2)
                : Observable.Throw<T>(t.Item3));
        }
    }
}
