using System;
using System.Runtime.InteropServices;

namespace KnowYourDevice {
    public sealed class LiveReading {
        public double? CpuPercent { get; set; }
        public double? MemoryPercent { get; set; }
        public ulong TotalMemory { get; set; }
        public ulong AvailableMemory { get; set; }
    }
    public sealed class LiveMetrics {
        [StructLayout(LayoutKind.Sequential)] private struct Times { public uint Low; public uint High; public ulong Value { get { return ((ulong)High<<32)|Low; } } }
        [StructLayout(LayoutKind.Sequential)] private class Memory {
            public uint Length=64; public uint Load; public ulong TotalPhysical; public ulong AvailablePhysical;
            public ulong TotalPage; public ulong AvailablePage; public ulong TotalVirtual; public ulong AvailableVirtual; public ulong Extended;
        }
        [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GetSystemTimes(out Times idle,out Times kernel,out Times user);
        [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GlobalMemoryStatusEx([In,Out] Memory memory);
        private ulong previousIdle,previousTotal; private bool initialized;
        public LiveReading Read() {
            var r=new LiveReading(); var mem=new Memory();
            if(GlobalMemoryStatusEx(mem) && mem.TotalPhysical>0) { r.TotalMemory=mem.TotalPhysical; r.AvailableMemory=mem.AvailablePhysical; r.MemoryPercent=100.0*(mem.TotalPhysical-mem.AvailablePhysical)/mem.TotalPhysical; }
            Times idle,kernel,user;
            if(GetSystemTimes(out idle,out kernel,out user)) {
                var total=kernel.Value+user.Value;
                if(initialized && total>previousTotal && idle.Value>=previousIdle) r.CpuPercent=Math.Max(0,Math.Min(100,100.0*(1.0-(double)(idle.Value-previousIdle)/(total-previousTotal))));
                previousIdle=idle.Value; previousTotal=total; initialized=true;
            }
            return r;
        }
    }
}
