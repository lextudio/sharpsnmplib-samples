using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SnmpD
{
    internal static class MibHelper
    {
        internal sealed class Ipv4AddressRow
        {
            public required string Index { get; init; }
            public required IPAddress Address { get; init; }
            public required int IfIndex { get; init; }
            public required IPAddress NetMask { get; init; }
            public int BroadcastBit { get; init; } = 1;
            public int ReasmMaxSize { get; init; } = 65535;
        }

        public static IReadOnlyList<Ipv4AddressRow> GetIpv4AddressRows()
        {
            var result = new List<Ipv4AddressRow>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var networkInterface = interfaces[i];
                var ipProperties = networkInterface.GetIPProperties();
                var ipv4Properties = ipProperties.GetIPv4Properties();
                var ifIndex = ipv4Properties?.Index ?? (i + 1);

                foreach (var address in ipProperties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    result.Add(new Ipv4AddressRow
                    {
                        Index = address.Address.ToString(),
                        Address = address.Address,
                        IfIndex = ifIndex,
                        NetMask = GetSubnetMask(address),
                    });
                }
            }

            return result
                .OrderBy(item => ToSortableAddress(item.Address))
                .ThenBy(item => item.IfIndex)
                .ToArray();
        }

        private static IPAddress GetSubnetMask(UnicastIPAddressInformation address)
        {
            if (address.IPv4Mask is { } mask)
            {
                return mask;
            }

            return TryCreateMask(address.PrefixLength) ?? IPAddress.Any;
        }

        private static IPAddress TryCreateMask(int prefixLength)
        {
            if (prefixLength <= 0)
            {
                return IPAddress.Any;
            }

            if (prefixLength >= 32)
            {
                return IPAddress.Parse("255.255.255.255");
            }

            var bits = uint.MaxValue << (32 - prefixLength);
            var bytes = BitConverter.GetBytes(bits);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return new IPAddress(bytes);
        }

        private static uint ToSortableAddress(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return ((uint)bytes[0] << 24)
                | ((uint)bytes[1] << 16)
                | ((uint)bytes[2] << 8)
                | bytes[3];
        }
    }
}
