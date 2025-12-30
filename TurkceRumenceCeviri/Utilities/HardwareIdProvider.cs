using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace TurkceRumenceCeviri.Utilities
{
    public static class HardwareIdProvider
    {
        public static string GetRawHardwareInfo()
        {
            var sb = new StringBuilder();

            // MAC addresses
            var macs = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up)
                .Select(n => n.GetPhysicalAddress().ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct();

            foreach (var m in macs) sb.Append(m);

            // Fallback information that does not require WMI
            try { sb.Append(Environment.MachineName); } catch { }
            try { sb.Append(Environment.ProcessorCount); } catch { }
            try { sb.Append(Environment.OSVersion.VersionString); } catch { }

            return sb.ToString();
        }

        public static string GetHardwareId()
        {
            var raw = GetRawHardwareInfo();
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw ?? string.Empty));
            return string.Concat(hash.Select(b => b.ToString("X2")));
        }
    }
}
