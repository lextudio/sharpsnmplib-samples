using System;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib
{
    internal static class DumpHelper
    {
        public static ReportMessage GetResponse(this Discovery discovery, int timeout, IPEndPoint receiver, bool dump)
        {
            if (dump)
            {
                var bytes = discovery.ToBytes();
                Console.WriteLine($"Sending {bytes.Length} bytes to UDP:");
                Console.WriteLine(ByteTool.Convert(bytes));
            }

            var response = discovery.GetResponse(timeout, receiver);
            if (dump)
            {
                var bytes = response.ToBytes();
                Console.WriteLine($"Received {bytes.Length} bytes from UDP:");
                Console.WriteLine(ByteTool.Convert(bytes));
            }

            return response;
        }

        public static ISnmpMessage GetResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, bool dump)
        {
            if (dump)
            {
                var bytes = request.ToBytes();
                Console.WriteLine($"Sending {bytes.Length} bytes to UDP:");
                Console.WriteLine(ByteTool.Convert(bytes));
            }

            var response = request.GetResponse(timeout, receiver);
            if (dump)
            {
                var bytes = response.ToBytes();
                Console.WriteLine($"Received {bytes.Length} bytes from UDP:");
                Console.WriteLine(ByteTool.Convert(bytes));
            }

            return response;
        }
    }
}