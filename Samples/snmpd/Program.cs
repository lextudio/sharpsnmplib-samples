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
using RequestProcessedEventArgs = Samples.Pipeline.RequestProcessedEventArgs;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using System.Globalization;
using System.Linq;

namespace SnmpD
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            // Default port value
            int port = 161;
            int? tcpPort = null;
            bool enableTcp = false;
            bool showHelp = false;
            
            // Parse command line options
            var options = new OptionSet
            {
                { "p|port=", "SNMP agent port number (default: 161)", (int p) => port = p },
                { "t|tcp", "Enable SNMP over TCP (uses --port by default)", t => enableTcp = t != null },
                { "tcp-port=", "SNMP over TCP port number (implies --tcp)", (int p) => { tcpPort = p; enableTcp = true; } },
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

            store.Add(new SysDescr());
            store.Add(new SysObjectId());
            store.Add(new SysUpTime());
            store.Add(new SysContact());
            store.Add(new SysName());
            store.Add(new SysLocation());
            store.Add(new SysServices());
            store.Add(new SysORLastChange());
            store.Add(new SysORTable());
#if USE_MIB_SOURCE_GENERATOR
            Lextm.SharpSnmpPro.Mib.ModuleRegister.RegisterIF_MIB(store);
#else
            store.Add(new IfNumber());
            store.Add(new IfTable());
#endif

            store.Add(new IP_MIB.ipAddrTable());
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

            var v1 = new Version1MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v2 = new Version2MembershipProvider(new OctetString("public"), new OctetString("public"));
            var v3 = new Version3MembershipProvider();
            var membership = new ComposedMembershipProvider(new IMembershipProvider[] { v1, v2, v3 });
            using var engine = new SnmpEngine(new Listener { Users = users }, new EngineGroup(idEngine161), store, membership);
            engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, port));
            if (enableTcp)
            {
                engine.Listener.AddTcpBinding(new IPEndPoint(IPAddress.Any, tcpPort ?? port));
            }
            engine.Listener.ExceptionRaised += Engine_ExceptionRaised;
            engine.RequestProcessed += RequestProcessed;
            engine.Start();
            Console.WriteLine("#SNMP is available at https://sharpsnmp.com");
            Console.WriteLine($"SNMP/UDP agent listening on port {port}");
            if (enableTcp)
            {
                Console.WriteLine($"SNMP/TCP agent listening on port {tcpPort ?? port}");
            }

            Console.WriteLine("#Fields: date time c-ip c-port s-ip s-port transport version principal pdu req-oids status error-index res-oids duration-ms note exception");
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

        private static void RequestProcessed(object? sender, RequestProcessedEventArgs e)
        {
            try
            {
                Console.WriteLine(FormatRequestLogLine(e));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "{0} {1} - - - - - {2} - - - - {3} {4} logging-failed:{5}",
                    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    InferTransportName(e.Binding),
                    Math.Round(e.Duration.TotalMilliseconds, 3).ToString("0.###", CultureInfo.InvariantCulture),
                    e.Exception is null ? "-" : SanitizeLogField($"{e.Exception.GetType().Name}:{e.Exception.Message}"),
                    SanitizeLogField($"{ex.GetType().Name}:{ex.Message}"));
            }
        }

        private static string FormatRequestLogLine(RequestProcessedEventArgs e)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var request = e.Request;
            var response = e.Response;
            var remote = e.Sender;
            var local = e.Binding.Endpoint;
            var exception = e.Exception?.GetType().Name + ":" + e.Exception?.Message;
            var note = string.IsNullOrWhiteSpace(e.ProcessingNote) ? "-" : SanitizeLogField(e.ProcessingNote);
            var requestScope = GetScopeWithPdu(request);
            var responseScope = response is null ? null : GetScopeWithPdu(response);
            var requestVariables = requestScope?.VariableBindings ?? [];
            var responseVariables = responseScope?.VariableBindings ?? [];
            var errorStatus = responseScope?.Pdu.ErrorStatus.ToString() ?? "-";
            var errorIndex = responseScope is null ? "-" : responseScope.Pdu.ErrorIndex.ToString(CultureInfo.InvariantCulture);

            return string.Join(' ', new[]
            {
                timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                remote.Address.ToString(),
                remote.Port.ToString(CultureInfo.InvariantCulture),
                local.Address.ToString(),
                local.Port.ToString(CultureInfo.InvariantCulture),
                InferTransportName(e.Binding),
                request.Version.ToString(),
                FormatPrincipal(request),
                FormatPduType(requestScope),
                FormatOidList(requestVariables),
                errorStatus,
                errorIndex,
                FormatOidList(responseVariables),
                Math.Round(e.Duration.TotalMilliseconds, 3).ToString("0.###", CultureInfo.InvariantCulture),
                note,
                string.IsNullOrWhiteSpace(exception) ? "-" : SanitizeLogField(exception),
            });
        }

        private static string InferTransportName(IListenerBinding binding)
        {
            var typeName = binding.GetType().Name;
            return typeName.Contains("Tcp", StringComparison.OrdinalIgnoreCase) ? "tcp" : "udp";
        }

        private static string FormatPrincipal(ISnmpMessage message)
        {
            try
            {
                var principal = message.Parameters.UserName.ToString();
                return string.IsNullOrWhiteSpace(principal) ? "-" : SanitizeLogField(principal);
            }
            catch
            {
                return "-";
            }
        }

        private static DotNetSnmp.Common.Definitions.IScope? GetScopeWithPdu(ISnmpMessage message)
        {
            var scope = message.Scope;
            return scope?.Pdu is null ? null : scope;
        }

        private static string FormatPduType(DotNetSnmp.Common.Definitions.IScope? scope)
        {
            return scope?.Pdu.TypeCode.ToString() ?? "-";
        }

        private static string FormatOidList(System.Collections.Generic.IEnumerable<Variable> variables)
        {
            if (variables == null)
            {
                return "-";
            }

            var oids = variables.Select(variable => variable.Id.ToString()).ToArray();
            if (oids.Length == 0)
            {
                return "-";
            }

            return SanitizeLogField(string.Join(',', oids));
        }

        private static string SanitizeLogField(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Replace(' ', '_');
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
