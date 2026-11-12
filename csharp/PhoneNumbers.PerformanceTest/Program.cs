using System;
using BenchmarkDotNet.Running;
namespace PhoneNumbers.PerformanceTest
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Not a benchmark: it measures memory retained after static initialization, which
            // BenchmarkDotNet's MemoryDiagnoser cannot see. See RetainedMemoryAudit.
            if (Array.Exists(args, a => string.Equals(a, "--retained-memory", StringComparison.Ordinal)))
                return RetainedMemoryAudit.Run();

            // Not a benchmark either: it counts what a cold start allocates, including the transient
            // bytes RetainedMemoryAudit collects away. See ColdAllocationAudit.
            if (Array.Exists(args, a => string.Equals(a, "--cold-allocations", StringComparison.Ordinal)))
                return ColdAllocationAudit.Run();

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            return 0;
        }
    }
}
