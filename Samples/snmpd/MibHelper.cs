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
        internal sealed class InterfaceRow
        {
            public required NetworkInterface Interface { get; init; }
            public required int IfIndex { get; init; }
            public required string Index { get; init; }
            public required string Description { get; init; }
            public required int TypeValue { get; init; }
            public required int Mtu { get; init; }
            public required long Speed { get; init; }
            public required byte[] PhysicalAddress { get; init; }
            public required int AdminStatus { get; init; }
            public required int OperStatus { get; init; }
            public required long InOctets { get; init; }
            public required long InUcastPackets { get; init; }
            public required long InNonUcastPackets { get; init; }
            public required long InDiscards { get; init; }
            public required long InErrors { get; init; }
            public required long InUnknownProtocols { get; init; }
            public required long OutOctets { get; init; }
            public required long OutUcastPackets { get; init; }
            public required long OutNonUcastPackets { get; init; }
            public required long OutDiscards { get; init; }
            public required long OutErrors { get; init; }
            public required int OutQueueLength { get; init; }
            public required string SpecificOid { get; init; }
        }

        internal sealed class Ipv4AddressRow
        {
            public required NetworkInterface Interface { get; init; }
            public required string Index { get; init; }
            public required IPAddress Address { get; init; }
            public required int IfIndex { get; init; }
            public required IPAddress NetMask { get; init; }
            public int BroadcastBit { get; init; } = 1;
            public int ReasmMaxSize { get; init; } = 65535;
        }

        public static IReadOnlyList<InterfaceRow> GetInterfaceRows()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var result = new List<InterfaceRow>(interfaces.Length);

            for (var i = 0; i < interfaces.Length; i++)
            {
                var networkInterface = interfaces[i];
                var stats = TryGetStatistics(networkInterface);
                var ifIndex = i + 1;

                result.Add(new InterfaceRow
                {
                    Interface = networkInterface,
                    IfIndex = ifIndex,
                    Index = ifIndex.ToString(),
                    Description = networkInterface.Description,
                    TypeValue = (int)networkInterface.NetworkInterfaceType,
                    Mtu = GetMtu(networkInterface),
                    Speed = GetSpeed(networkInterface),
                    PhysicalAddress = networkInterface.GetPhysicalAddress().GetAddressBytes(),
                    AdminStatus = (int)networkInterface.OperationalStatus,
                    OperStatus = (int)networkInterface.OperationalStatus,
                    InOctets = stats == null ? 0L : stats.BytesReceived,
                    InUcastPackets = stats == null ? 0L : stats.UnicastPacketsReceived,
                    InNonUcastPackets = GetNonUnicastPacketsReceived(stats),
                    InDiscards = stats == null ? 0L : stats.IncomingPacketsDiscarded,
                    InErrors = stats == null ? 0L : stats.IncomingPacketsWithErrors,
                    InUnknownProtocols = GetIncomingUnknownProtocols(stats),
                    OutOctets = stats == null ? 0L : stats.BytesSent,
                    OutUcastPackets = stats == null ? 0L : stats.UnicastPacketsSent,
                    OutNonUcastPackets = GetNonUnicastPacketsSent(stats),
                    OutDiscards = GetOutgoingPacketsDiscarded(stats),
                    OutErrors = stats == null ? 0L : stats.OutgoingPacketsWithErrors,
                    OutQueueLength = GetOutputQueueLength(stats),
                    SpecificOid = "0.0",
                });
            }

            return result;
        }

        public static IReadOnlyList<Ipv4AddressRow> GetIpv4AddressRows()
        {
            var result = new List<Ipv4AddressRow>();
            foreach (var interfaceRow in GetInterfaceRows())
            {
                var networkInterface = interfaceRow.Interface;
                var ipProperties = networkInterface.GetIPProperties();

                foreach (var address in ipProperties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    result.Add(new Ipv4AddressRow
                    {
                        Interface = networkInterface,
                        Index = address.Address.ToString(),
                        Address = address.Address,
                        IfIndex = interfaceRow.IfIndex,
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

            return TryCreateMask(address.PrefixLength);
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

        private static IPInterfaceStatistics TryGetStatistics(NetworkInterface networkInterface)
        {
            try
            {
                return networkInterface.GetIPStatistics();
            }
            catch (NetworkInformationException)
            {
                return null;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        private static int GetMtu(NetworkInterface networkInterface)
        {
            try
            {
                if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
                {
                    return networkInterface.GetIPProperties().GetIPv4Properties()?.Mtu ?? -1;
                }

                if (networkInterface.Supports(NetworkInterfaceComponent.IPv6))
                {
                    return networkInterface.GetIPProperties().GetIPv6Properties()?.Mtu ?? 0;
                }
            }
            catch (NetworkInformationException)
            {
                return 0;
            }
            catch (NotImplementedException)
            {
                return 0;
            }
            catch (PlatformNotSupportedException)
            {
                return 0;
            }

            return 0;
        }

        private static long GetSpeed(NetworkInterface networkInterface)
        {
            try
            {
                return networkInterface.Speed;
            }
            catch (PlatformNotSupportedException)
            {
                return 0L;
            }
        }

        private static long GetIncomingUnknownProtocols(IPInterfaceStatistics stats)
        {
            if (stats is null)
            {
                return 0L;
            }

            try
            {
                return stats.IncomingUnknownProtocolPackets;
            }
            catch (PlatformNotSupportedException)
            {
                return 0L;
            }
        }

        private static long GetNonUnicastPacketsReceived(IPInterfaceStatistics stats)
        {
            if (stats is null)
            {
                return 0L;
            }

            try
            {
                return stats.NonUnicastPacketsReceived;
            }
            catch (PlatformNotSupportedException)
            {
                return 0L;
            }
        }

        private static long GetNonUnicastPacketsSent(IPInterfaceStatistics stats)
        {
            if (stats is null)
            {
                return 0L;
            }

            try
            {
                return stats.NonUnicastPacketsSent;
            }
            catch (PlatformNotSupportedException)
            {
                return 0L;
            }
        }

        private static long GetOutgoingPacketsDiscarded(IPInterfaceStatistics stats)
        {
            if (stats is null)
            {
                return 0L;
            }

            try
            {
                return stats.OutgoingPacketsDiscarded;
            }
            catch (PlatformNotSupportedException)
            {
                return 0L;
            }
        }

        private static int GetOutputQueueLength(IPInterfaceStatistics stats)
        {
            if (stats is null)
            {
                return 0;
            }

            try
            {
                return checked((int)stats.OutputQueueLength);
            }
            catch (PlatformNotSupportedException)
            {
                return 0;
            }
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
