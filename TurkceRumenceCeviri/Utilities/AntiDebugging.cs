using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;

namespace TurkceRumenceCeviri.Utilities
{
    public static class AntiDebugging
    {
        [DllImport("kernel32.dll")]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll")]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isPresent);

        public static bool IsBeingDebugged()
        {
            if (Debugger.IsAttached) return true;
            try
            {
                if (IsDebuggerPresent()) return true;
            }
            catch { }

            try
            {
                bool isPresent = false;
                CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isPresent);
                if (isPresent) return true;
            }
            catch { }

            var suspicious = new[] { "dnspy", "x64dbg", "ida", "ollydbg", "procexp", "dbg" };
            try
            {
                var procs = Process.GetProcesses().Select(p => p.ProcessName.ToLowerInvariant());
                if (procs.Any(name => suspicious.Any(s => name.Contains(s)))) return true;
            }
            catch { }

            return false;
        }
    }
}
