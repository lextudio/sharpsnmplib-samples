using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Samples.Pipeline;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Listener = Samples.Pipeline.Listener;

namespace Samples.Integration
{
    [Collection("Integration")]
    public class TrapDaemonTestFixture
    {
        private static readonly NumberGenerator Port = new NumberGenerator(30000, 39999);
        private const int MaxTimeout = 15 * 1000; // 15 seconds
        
        [Fact]
        public void TestDiscoveryWorks()
        {
            Assert.True(true);
        }

        [Fact]
        public async Task TestTrapV2HandlerWithV2Message()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var pipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                trapV2Received: _ =>
                {
                    count++;
                    manualEvent.Set();
                });

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            var listenerMessages = 0;
            Exception listenerException = null;
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Listener.MessageReceived += (_, _) => Interlocked.Increment(ref listenerMessages);
            engine.Listener.ExceptionRaised += (_, e) => listenerException = e.Exception;
            engine.Start();

            try
            {
                await AssertCompletesAsync(() => Messenger.SendTrapV2Async(
                        1,
                        VersionCode.V2,
                        daemonEndPoint,
                        new OctetString("public"),
                        new ObjectIdentifier("1.3.6.1"),
                        500,
                        new List<Variable>()),
                    "SendTrapV2Async(v2c)");

                var signaled = manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken);
                Assert.True(
                    signaled,
                    $"Timed out waiting for TrapV2 handler. listenerMessages={listenerMessages}; listenerException={listenerException}");
                Assert.Equal(1, count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestTrapV2HandlerWithV3Message()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId, authUserKnownEngineId: true);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var pipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                trapV2Received: _ =>
                {
                    count++;
                    manualEvent.Set();
                });

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Start();

            try
            {
                var privacy = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication")));
                var trap = new TrapV2Message(
                    VersionCode.V3,
                    1004947569,
                    234419641,
                    new OctetString("authen"),
                    new ObjectIdentifier("1.3.6"),
                    0,
                    new List<Variable>(),
                    privacy,
                    0x10000,
                    new OctetString(engineId),
                    0,
                    0);
                await trap.SendAsync(daemonEndPoint, TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for TrapV2 v3 handler");
                Assert.Equal(1, count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestTrapV2HandlerWithV3MessageAndNoEngineId()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var innerPipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                trapV2Received: _ => { count++; });

            Func<SnmpMessageContext, Task> pipeline = async context =>
            {
                await innerPipeline(context);
                manualEvent.Set();
            };

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Start();

            try
            {
                var privacy = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication")));
                var trap = new TrapV2Message(
                    VersionCode.V3,
                    1004947569,
                    234419641,
                    new OctetString("authen"),
                    new ObjectIdentifier("1.3.6"),
                    0,
                    new List<Variable>(),
                    privacy,
                    0x10000,
                    new OctetString(ByteTool.Convert("80001F8880E9630000D61FF450")),
                    0,
                    0);
                await trap.SendAsync(daemonEndPoint, TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for NoEngineId pipeline");
                Assert.Equal(0, count);
                Assert.Equal(new Counter32(1), group.UnknownEngineId.Data);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestTrapV2HandlerWithV3MessageAndWrongEngineId()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId, authUserKnownEngineId: true);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var innerPipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                trapV2Received: _ => { count++; });

            Func<SnmpMessageContext, Task> pipeline = async context =>
            {
                await innerPipeline(context);
                manualEvent.Set();
            };

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Start();

            try
            {
                var privacy = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication")));
                var trap = new TrapV2Message(
                    VersionCode.V3,
                    1004947569,
                    234419641,
                    new OctetString("authen"),
                    new ObjectIdentifier("1.3.6"),
                    0,
                    new List<Variable>(),
                    privacy,
                    0x10000,
                    new OctetString(ByteTool.Convert("80001F8880E9630000D61FF450")),
                    0,
                    0);
                await trap.SendAsync(daemonEndPoint, TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for WrongEngineId pipeline");
                Assert.Equal(0, count);
                Assert.Equal(new Counter32(1), group.UnknownEngineId.Data);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestInformV2HandlerWithV2Message()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var pipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                informReceived: _ =>
                {
                    count++;
                    manualEvent.Set();
                });

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Listener.ExceptionRaised += (sender, e) => { Assert.Fail("unhandled exception"); };
            engine.Start();

            try
            {
                await Messenger.SendInformAsync(
                    1,
                    VersionCode.V2,
                    daemonEndPoint,
                    new OctetString("public"),
                    OctetString.Empty,
                    new ObjectIdentifier("1.3.6.1"),
                    500,
                    new List<Variable>(),
                    DefaultPrivacyProvider.DefaultPair,
                    null,
                    TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for InformV2 handler");
                Assert.Equal(1, count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestInformV2HandlerWithV3Message()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var pipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                informReceived: _ =>
                {
                    count++;
                    manualEvent.Set();
                });

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Listener.ExceptionRaised += (sender, e) => { Assert.Fail("unhandled exception"); };
            engine.Start();

            try
            {
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.InformRequestPdu);
                ReportMessage report = discovery.GetResponse(5000, daemonEndPoint);
                await Messenger.SendInformAsync(
                    1,
                    VersionCode.V3,
                    daemonEndPoint,
                    new OctetString("neither"),
                    OctetString.Empty,
                    new ObjectIdentifier("1.3.6.1"),
                    500,
                    new List<Variable>(),
                    DefaultPrivacyProvider.DefaultPair,
                    report,
                    TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for InformV3 handler");
                Assert.Equal(1, count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        [Fact]
        public async Task TestInformV2HandlerWithV3MessageDES()
        {
            var manualEvent = new ManualResetEventSlim();
            var engineId = ByteTool.Convert("80001F8880E9630000D61FF449");
            var users = CreateUsers(engineId);
            var store = new ObjectStore();
            var membership = CreateMembership();

            var count = 0;
            var group = new EngineGroup(engineId);
            var pipeline = CreatePipeline(
                users,
                group,
                store,
                membership,
                informReceived: _ =>
                {
                    count++;
                    manualEvent.Set();
                });

            var engine = new SnmpEngine(new Listener { Users = users }, pipeline);
            var daemonEndPoint = CreateEndpoint(IPAddress.Loopback);
            engine.Listener.AddBinding(daemonEndPoint);
            engine.Listener.ExceptionRaised += (sender, e) => { Assert.Fail("unhandled exception"); };
            engine.Start();

            try
            {
                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.InformRequestPdu);
                ReportMessage report = discovery.GetResponse(5000, daemonEndPoint);
                await Messenger.SendInformAsync(
                    1,
                    VersionCode.V3,
                    daemonEndPoint,
                    new OctetString("privacy"),
                    OctetString.Empty,
                    new ObjectIdentifier("1.3.6.1"),
                    500,
                    new List<Variable>(),
                    new DESPrivacyProvider(
                        new OctetString("privacyphrase"),
                        new MD5AuthenticationProvider(new OctetString("authentication"))),
                    report,
                    TestContext.Current.CancellationToken);

                Assert.True(manualEvent.Wait(MaxTimeout, TestContext.Current.CancellationToken), "Timed out waiting for InformV3DES handler");
                Assert.Equal(1, count);
            }
            finally
            {
                StopEngine(engine);
            }
        }

        private static Func<SnmpMessageContext, Task> CreatePipeline(
            UserRegistry users,
            EngineGroup group,
            ObjectStore store,
            IMembershipProvider membership,
            Action<TrapV1MessageReceivedEventArgs> trapV1Received = null,
            Action<TrapV2MessageReceivedEventArgs> trapV2Received = null,
            Action<InformRequestMessageReceivedEventArgs> informReceived = null)
        {
            return new SnmpPipelineBuilder()
                .UseContextFactory(users, group)
                .UseAuthentication(membership)
                .UseMessageHandler(store, trapV1Received, trapV2Received, informReceived)
                .Build();
        }

        private static UserRegistry CreateUsers(byte[] engineId, bool authUserKnownEngineId = false)
        {
            var users = new UserRegistry();
            users.Add(new OctetString("neither"), DefaultPrivacyProvider.DefaultPair);

            var authProvider = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication")));
            if (authUserKnownEngineId)
            {
                authProvider.EngineIds = new List<OctetString> { new OctetString(engineId) };
            }

            users.Add(new OctetString("authen"), authProvider);

            if (DESPrivacyProvider.IsSupported)
            {
                users.Add(
                    new OctetString("privacy"),
                    new DESPrivacyProvider(
                        new OctetString("privacyphrase"),
                        new MD5AuthenticationProvider(new OctetString("authentication"))));
            }

            return users;
        }

        private static IMembershipProvider CreateMembership()
        {
            var v1 = new Version1MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v2 = new Version2MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v3 = new Version3MembershipProvider();
            return new ComposedMembershipProvider(new IMembershipProvider[] { v1, v2, v3 });
        }

        private static async Task AssertCompletesAsync(Func<Task> operation, string operationName)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var task = Task.Run(operation, TestContext.Current.CancellationToken);
            var timeoutTask = Task.Delay(MaxTimeout, TestContext.Current.CancellationToken);
            var completed = await Task.WhenAny(task, timeoutTask);
            Assert.True(completed == task, $"{operationName} exceeded {MaxTimeout} ms.");
            await task;
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
}
