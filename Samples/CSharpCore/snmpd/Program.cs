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
using Samples.Objects;
using Samples.Pipeline;
using Lextm.SharpSnmpLib.Security;
using System;
using System.Net;
using Listener = Samples.Pipeline.Listener;
using MessageReceivedEventArgs = Samples.Pipeline.MessageReceivedEventArgs;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

// USE_SOURCE_GENERATOR conditional symbol is automatically defined in the project file
// when MibSourceGenerator is detected
#if USE_SOURCE_GENERATOR
using SNMPv2_MIB;
#endif

namespace SnmpD
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            // Default port value
            int port = 161;
            bool showHelp = false;
            
            // Parse command line options
            var options = new OptionSet
            {
                { "p|port=", "SNMP agent port number (default: 161)", (int p) => port = p },
                { "h|help", "Show this help message and exit", h => showHelp = h != null }
            };
            
            try
            {
                options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ShowHelp(options);
                return;
            }
            
            if (showHelp)
            {
                ShowHelp(options);
                return;
            }
            
            var idEngine161 = ByteTool.Convert("80004fb805636c6f75644dab22cc");
            var store = new ObjectStore();

#if USE_SOURCE_GENERATOR
            // Use the source-generated MIB classes from SNMPv2-MIB
            store.Add(new sysDescr());
            store.Add(new sysObjectID());
            store.Add(new sysUpTime());
            store.Add(new sysContact());
            store.Add(new sysName());
            store.Add(new sysLocation());
            store.Add(new sysServices());
            store.Add(new sysORLastChange());
            store.Add(new sysORTable());
#else
            store.Add(new SysDescr());
            store.Add(new SysObjectId());
            store.Add(new SysUpTime());
            store.Add(new SysContact());
            store.Add(new SysName());
            store.Add(new SysLocation());
            store.Add(new SysServices());
            store.Add(new SysORLastChange());
            store.Add(new SysORTable());

#endif

            store.Add(new IfNumber());
            store.Add(new IfTable());
            // store.Add(new ipNetToMediaTable());
            // store.Add(new IpNetToMediaTable());
            // //store.Add(new EntPhysicalTable());
            // // store.Add(new Counter64Test());
            // store.Add(new CompDescr());
            // store.Add(new PowerVoltage());

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

            var getv1 = new GetV1MessageHandler();
            var getv1Mapping = new HandlerMapping("v1", "GET", getv1);

            var getv23 = new GetMessageHandler();
            var getv23Mapping = new HandlerMapping("v2,v3", "GET", getv23);

            var setv1 = new SetV1MessageHandler();
            var setv1Mapping = new HandlerMapping("v1", "SET", setv1);

            var setv23 = new SetMessageHandler();
            var setv23Mapping = new HandlerMapping("v2,v3", "SET", setv23);

            var getnextv1 = new GetNextV1MessageHandler();
            var getnextv1Mapping = new HandlerMapping("v1", "GETNEXT", getnextv1);

            var getnextv23 = new GetNextMessageHandler();
            var getnextv23Mapping = new HandlerMapping("v2,v3", "GETNEXT", getnextv23);

            var getbulk = new GetBulkMessageHandler();
            var getbulkMapping = new HandlerMapping("v2,v3", "GETBULK", getbulk);

            var v1 = new Version1MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v2 = new Version2MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v3 = new Version3MembershipProvider();
            var membership = new ComposedMembershipProvider(new IMembershipProvider[] { v1, v2, v3 });
            var handlerFactory = new MessageHandlerFactory(new[]
            {
                getv1Mapping,
                getv23Mapping,
                setv1Mapping,
                setv23Mapping,
                getnextv1Mapping,
                getnextv23Mapping,
                getbulkMapping
            });

            var pipelineFactory = new SnmpApplicationFactory(store, membership, handlerFactory);
            using var engine = new SnmpEngine(pipelineFactory, new Listener { Users = users }, new EngineGroup(idEngine161));
            engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, port));
            engine.Listener.ExceptionRaised += Engine_ExceptionRaised;
            engine.Listener.MessageReceived += RequestReceived;
            engine.Start();
            Console.WriteLine("#SNMP is available at https://sharpsnmp.com");
            Console.WriteLine($"SNMP agent listening on port {port}");

            Console.WriteLine("Press Ctrl+C to stop . . . ");
            var cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
            await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { });
            engine.Stop();
        }

        private static void Engine_ExceptionRaised(object sender, ExceptionRaisedEventArgs e)
        {
            Console.WriteLine("Exception occurred: {0}", e.Exception);
        }

        private static void RequestReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Message version {0}: {1}", e.Message.Version, e.Message);
        }
        
        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Usage: snmpd [OPTIONS]");
            Console.WriteLine("SNMP agent sample using #SNMP Library.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}
