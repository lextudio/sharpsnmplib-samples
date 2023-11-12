/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/23
 * Time: 19:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using System;
using System.Collections.Generic;
using System.Net;
using Listener = Samples.Pipeline.Listener;
using System.Threading;
using System.Threading.Tasks;

namespace SnmpTrapD
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length != 0)
            {
                return;
            }

            var idEngine = ByteTool.Convert("8000000001020304");
            var users = new UserRegistry();
            users.Add(new OctetString("usr-none-none"), DefaultPrivacyProvider.DefaultPair);
            users.Add(new OctetString("usr-md5-none"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha-none"), new DefaultPrivacyProvider(new SHA1AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha256-none"), new DefaultPrivacyProvider(new SHA256AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha384-none"), new DefaultPrivacyProvider(new SHA384AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha512-none"), new DefaultPrivacyProvider(new SHA512AuthenticationProvider(new OctetString("authkey1"))));
            if (DESPrivacyProvider.IsSupported)
            {
                users.Add(new OctetString("usr-md5-des"), new DESPrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha-des"), new DESPrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha256-des"), new DESPrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha384-des"), new DESPrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha512-des"), new DESPrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
            }

            users.Add(new OctetString("usr-md5-3des"), new TripleDESPrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha-3des"), new TripleDESPrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha256-3des"), new TripleDESPrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha384-3des"), new TripleDESPrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
            users.Add(new OctetString("usr-sha512-3des"), new TripleDESPrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
            if (AESPrivacyProviderBase.IsSupported)
            {
                users.Add(new OctetString("usr-md5-aes"), new AESPrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-md5-aes128"), new AESPrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-md5-aes192"), new AES192PrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-md5-aes256"), new AES256PrivacyProvider(new OctetString("privkey1"), new MD5AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha-aes"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha-aes128"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha-aes192"), new AES192PrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha-aes256"), new AES256PrivacyProvider(new OctetString("privkey1"), new SHA1AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha256-aes"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha256-aes128"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha256-aes192"), new AES192PrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha256-aes256"), new AES256PrivacyProvider(new OctetString("privkey1"), new SHA256AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha384-aes"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha384-aes128"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha384-aes192"), new AES192PrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha384-aes256"), new AES256PrivacyProvider(new OctetString("privkey1"), new SHA384AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha512-aes"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha512-aes128"), new AESPrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha512-aes192"), new AES192PrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
                users.Add(new OctetString("usr-sha512-aes256"), new AES256PrivacyProvider(new OctetString("privkey1"), new SHA512AuthenticationProvider(new OctetString("authkey1"))));
            }

            var trapv1 = new TrapV1MessageHandler();
            trapv1.MessageReceived += WatcherTrapV1Received;
            var trapv1Mapping = new HandlerMapping("v1", "TRAPV1", trapv1);

            var trapv2 = new TrapV2MessageHandler();
            trapv2.MessageReceived += WatcherTrapV2Received;
            var trapv2Mapping = new HandlerMapping("v2,v3", "TRAPV2", trapv2);

            var inform = new InformRequestMessageHandler();
            inform.MessageReceived += WatcherInformRequestReceived;
            var informMapping = new HandlerMapping("v2,v3", "INFORM", inform);

            var store = new ObjectStore();
            var v1 = new Version1MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v2 = new Version2MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v3 = new Version3MembershipProvider();
            var membership = new ComposedMembershipProvider(new IMembershipProvider[] { v1, v2, v3 });
            var handlerFactory = new MessageHandlerFactory(new[] { trapv1Mapping, trapv2Mapping, informMapping });

            var pipelineFactory = new SnmpApplicationFactory(store, membership, handlerFactory);
            using (var engine = new SnmpEngine(pipelineFactory, new Listener { Users = users }, new EngineGroup(idEngine)))
            {
                engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, 162));
                engine.Listener.ExceptionRaised += (sender, e) => Console.WriteLine($"Exception occurred: {e.Exception}");
                engine.Start();
                Console.WriteLine("#SNMP is available at https://sharpsnmp.com");
                Console.WriteLine("Press Ctrl+C to stop . . . ");
                var cancellationTokenSource = new CancellationTokenSource();
                AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
                Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
                await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { });
                engine.Stop();
            }
        }

        private static void WatcherInformRequestReceived(object sender, InformRequestMessageReceivedEventArgs e)
        {
            Console.WriteLine("INFORM version {0}: {1}", e.InformRequestMessage.Version, e.InformRequestMessage);
            foreach (var variable in e.InformRequestMessage.Variables())
            {
                Console.WriteLine(variable);
            }
        }

        private static void WatcherTrapV2Received(object sender, TrapV2MessageReceivedEventArgs e)
        {
            Console.WriteLine("TRAP version {0}: {1}", e.TrapV2Message.Version, e.TrapV2Message);
            foreach (var variable in e.TrapV2Message.Variables())
            {
                Console.WriteLine(variable);
            }
        }

        private static void WatcherTrapV1Received(object sender, TrapV1MessageReceivedEventArgs e)
        {
            Console.WriteLine("TRAP version {0}; {1}", e.TrapV1Message.Version, e.TrapV1Message);
            foreach (var variable in e.TrapV1Message.Variables())
            {
                Console.WriteLine(variable);
            }
        }
    }
}
