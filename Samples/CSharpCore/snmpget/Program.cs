// typical usage
// snmpget -c=public -v=1 localhost 1.3.6.1.2.1.1.1.0
// snmpget -c=public -v=2 localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=noAuthNoPriv -u=neither localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=authNoPriv -a=MD5 -A=authentication -u=authen localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=authPriv -a=MD5 -A=authentication -x=DES -X=privacyphrase -u=privacy localhost 1.3.6.1.2.1.1.1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Security;
using Lextm.SharpSnmpLib.Messaging;
using Mono.Options;
using System.Reflection;

namespace SnmpGet 
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            string community = "public";
            bool showHelp   = false;
            bool showVersion = false;
            VersionCode version = VersionCode.V1;
            int timeout = 1000;
            int retry = 0;
            Levels level = Levels.Reportable;
            string user = string.Empty;
            string contextName = string.Empty;
            string authentication = string.Empty;
            string authPhrase = string.Empty;
            string privacy = string.Empty;
            string privPhrase = string.Empty;
            bool dump = false;

            OptionSet p = new OptionSet()
                .Add("c:", "Community name, (default is public)", delegate (string v) { if (v != null) community = v; })
                .Add("l:", "Security level, (default is noAuthNoPriv)", delegate (string v)
                                                                                   {
                                                                                       if (v.ToUpperInvariant() == "NOAUTHNOPRIV")
                                                                                       {
                                                                                           level = Levels.Reportable;
                                                                                       }
                                                                                       else if (v.ToUpperInvariant() == "AUTHNOPRIV")
                                                                                       {
                                                                                           level = Levels.Authentication | Levels.Reportable;
                                                                                       }
                                                                                       else if (v.ToUpperInvariant() == "AUTHPRIV")
                                                                                       {
                                                                                           level = Levels.Authentication | Levels.Privacy | Levels.Reportable;
                                                                                       }
                                                                                       else
                                                                                       {
                                                                                           throw new ArgumentException("no such security mode: " + v);
                                                                                       }
                                                                                   })
                .Add("a:", "Authentication method (MD5, SHA, SHA256, SHA384, or SHA512)", delegate (string v) { authentication = v; })
                .Add("A:", "Authentication passphrase", delegate (string v) { authPhrase = v; })
                .Add("x:", "Privacy method (DES, 3DES, AES, AES192, or AES256)", delegate (string v) { privacy = v; })
                .Add("X:", "Privacy passphrase", delegate (string v) { privPhrase = v; })
                .Add("u:", "Security name", delegate (string v) { user = v; })
                .Add("C:", "Context name", delegate (string v) { contextName = v; })
                .Add("h|?|help", "Print this help information.", delegate (string v) { showHelp = v != null; })
                .Add("V", "Display version number of this application.", delegate (string v) { showVersion = v != null; })
                .Add("d", "Display message dump", delegate (string v) { dump = true; })
                .Add("t:", "Timeout value (unit is second).", delegate (string v) { timeout = int.Parse(v) * 1000; })
                .Add("r:", "Retry count (default is 0)", delegate (string v) { retry = int.Parse(v); })
                .Add("v|version:", "SNMP version (1, 2, and 3 are currently supported)", delegate (string v)
                                                                                               {
                                                                                                   if (v == "2c")
                                                                                                   {
                                                                                                       v = "2";
                                                                                                   }

                                                                                                   switch (int.Parse(v))
                                                                                                   {
                                                                                                       case 1:
                                                                                                           version = VersionCode.V1;
                                                                                                           break;
                                                                                                       case 2:
                                                                                                           version = VersionCode.V2;
                                                                                                           break;
                                                                                                       case 3:
                                                                                                           version = VersionCode.V3;
                                                                                                           break;
                                                                                                       default:
                                                                                                           throw new ArgumentException("no such version: " + v);
                                                                                                   }
                                                                                               });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (extra.Count < 2)
            {
                Console.WriteLine("invalid variable number: " + extra.Count);
                return;
            }  
            
            if (showVersion)
            {
                Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyVersionAttribute>().Version);
                return;
            }

            IPAddress ip;
            int port = 161; // Default SNMP port
            string hostNameOrAddress = extra[0];
            
            // Handle host:port format
            if (hostNameOrAddress.Contains(':'))
            {
                string[] parts = hostNameOrAddress.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int parsedPort))
                {
                    hostNameOrAddress = parts[0];
                    port = parsedPort;
                }
            }
            
            bool parsed = IPAddress.TryParse(hostNameOrAddress, out ip);
            if (!parsed)
            {
                var addresses = Dns.GetHostAddressesAsync(hostNameOrAddress);
                addresses.Wait();
                foreach (IPAddress address in 
                    addresses.Result.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
                {
                    ip = address;
                    break;
                }

                if (ip == null)
                {
                    Console.WriteLine("invalid host or wrong IP address found: " + hostNameOrAddress);
                    return;
                }
            }

            try
            {
                List<Variable> vList = new List<Variable>();
                for (int i = 1; i < extra.Count; i++)
                {
                    Variable test = new Variable(new ObjectIdentifier(extra[i]));
                    vList.Add(test);
                }

                IPEndPoint receiver = new IPEndPoint(ip, port);
                if (version != VersionCode.V3)
                {
                    foreach (
                        Variable variable in
                            Messenger.Get(version, receiver, new OctetString(community), vList, timeout))
                    {
                        Console.WriteLine(variable);
                    }

                    return;
                }
                
                if (string.IsNullOrEmpty(user))
                {
                    Console.WriteLine("User name need to be specified for v3.");
                    return;
                }

                IAuthenticationProvider auth = (level & Levels.Authentication) == Levels.Authentication
                                                   ? GetAuthenticationProviderByName(authentication, authPhrase)
                                                   : DefaultAuthenticationProvider.Instance;

                IPrivacyProvider priv;
                if ((level & Levels.Privacy) == Levels.Privacy)
                {
                    priv = GetPrivacyProviderByName(privacy, privPhrase, auth);
                }
                else
                {
                    priv = new DefaultPrivacyProvider(auth);
                }

                Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
                ReportMessage report = discovery.GetResponse(timeout, receiver, dump);

                GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(user), new OctetString(string.IsNullOrWhiteSpace(contextName) ? string.Empty : contextName), vList, priv, Messenger.MaxMessageSize, report);
                ISnmpMessage reply = request.GetResponse(timeout, receiver, dump);
                if (reply is ReportMessage)
                {
                    if (reply.Pdu().Variables.Count == 0)
                    {
                        Console.WriteLine("wrong report message received");
                        return;
                    }

                    var id = reply.Pdu().Variables[0].Id;
                    if (id != Messenger.NotInTimeWindow)
                    {
                        var error = id.GetErrorMessage();
                        Console.WriteLine(error);
                        return;
                    }

                    // according to RFC 3414, send a second request to sync time.
                    request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(user), new OctetString(string.IsNullOrWhiteSpace(contextName) ? string.Empty : contextName), vList, priv, Messenger.MaxMessageSize, reply);
                    reply = request.GetResponse(timeout, receiver, dump);
                }
                else if (reply.Pdu().ErrorStatus.ToInt32() != 0) // != ErrorCode.NoError
                {
                    throw ErrorException.Create(
                        "error in response",
                        receiver.Address,
                        reply);
                }

                foreach (Variable v in reply.Pdu().Variables)
                {
                    Console.WriteLine(v);
                }
            }
            catch (SnmpException ex)
            {
                Console.WriteLine(ex);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("#SNMP is available at https://sharpsnmp.com");
            Console.WriteLine("snmpget [Options] IP-address|host-name OID [OID] ...");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }

        private static IPrivacyProvider GetPrivacyProviderByName(string privacy, string phrase, IAuthenticationProvider auth)
        {
            if (string.IsNullOrEmpty(privacy))
            {
                return new DefaultPrivacyProvider(auth);
            }

            switch (privacy.ToUpperInvariant())
            {
                case "DES":
                    if (DESPrivacyProvider.IsSupported)
                    {
                        return new DESPrivacyProvider(new OctetString(phrase), auth);
                    }
                    
                    throw new ArgumentException("DES privacy is not supported in this system");

                case "3DES":
                    return new TripleDESPrivacyProvider(new OctetString(phrase), auth);

                case "AES":
                    if (AESPrivacyProvider.IsSupported)
                    {
                        return new AESPrivacyProvider(new OctetString(phrase), auth);;
                    }
                    
                    throw new ArgumentException("AES privacy is not supported in this system");

                case "AES192":
                    if (AESPrivacyProvider.IsSupported)
                    {
                        return new AES192PrivacyProvider(new OctetString(phrase), auth);
                    }
                    
                    throw new ArgumentException("AES192 privacy is not supported in this system");

                case "AES256":
                    if (AESPrivacyProvider.IsSupported)
                    {
                        return new AES256PrivacyProvider(new OctetString(phrase), auth);
                    }
                    
                    throw new ArgumentException("AES256 privacy is not supported in this system");
                    
                default:
                    throw new ArgumentException("unknown privacy name: " + privacy);
            }
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static IAuthenticationProvider GetAuthenticationProviderByName(string authentication, string phrase)
        {
            if (authentication.ToUpperInvariant() == "MD5")
            {
                return new MD5AuthenticationProvider(new OctetString(phrase));
            }
            
            if (authentication.ToUpperInvariant() == "SHA")
            {
                return new SHA1AuthenticationProvider(new OctetString(phrase));
            }

            if (authentication.ToUpperInvariant() == "SHA256")
            {
                return new SHA256AuthenticationProvider(new OctetString(phrase));
            }

            if (authentication.ToUpperInvariant() == "SHA384")
            {
                return new SHA384AuthenticationProvider(new OctetString(phrase));
            }

            if (authentication.ToUpperInvariant() == "SHA512")
            {
                return new SHA512AuthenticationProvider(new OctetString(phrase));
            }

            throw new ArgumentException("unknown name", nameof(authentication));
        }
    }
}
