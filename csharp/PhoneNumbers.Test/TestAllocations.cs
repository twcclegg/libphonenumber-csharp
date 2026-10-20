using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    /// <summary>
    /// Pins how much the read-only paths allocate per call on a warm instance, for every region's
    /// example number. The benchmark comparison on PRs only flags a case that moves by 20% or more,
    /// so a path that should allocate nothing can start allocating a few bytes per call without any
    /// check noticing; these fail on the first byte.
    /// <para>
    /// Budgets are measured, not guessed: each path is allowed exactly the string it returns (sized by
    /// copying that string under the same counter) and nothing else. A failure lists every region over
    /// budget and by how much.
    /// </para>
    /// <para>
    /// <see cref="GC.GetAllocatedBytesForCurrentThread"/> is per thread, so parallel test classes do not
    /// disturb it; each call is measured after warm-up so lazy metadata loads and regex construction are
    /// already paid. Uses the production metadata, since that is what ships.
    /// </para>
    /// </summary>
    public class TestAllocations
    {
        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        private static readonly (string Region, PhoneNumber Number)[] ExampleNumbers =
            PhoneUtil.GetSupportedRegions()
                .OrderBy(r => r, StringComparer.Ordinal)
                .Select(r => (r, PhoneUtil.GetExampleNumber(r)))
                .Where(x => x.Item2 is not null)
                .ToArray();

        // Warm-up calls before measuring, and measured attempts per case, of which the smallest counts:
        // a tier-up or a static initializer elsewhere in the runtime can land on this thread once.
        private const int WarmUpCalls = 3;
        private const int Attempts = 3;

        private static long Measure(Action action)
        {
            for (var i = 0; i < WarmUpCalls; i++)
                action();
            var least = long.MaxValue;
            for (var i = 0; i < Attempts; i++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                action();
                least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
            }
            return least;
        }

        /// <summary>Bytes one fresh copy of <paramref name="value"/> takes on the heap.</summary>
        private static long SizeOf(string value) => Measure(() => GC.KeepAlive(new string(value.AsSpan())));

        private static void AssertWithinBudget(string api, Func<PhoneNumber, long> budget, Action<PhoneNumber> call)
        {
            var overBudget = new List<string>();
            foreach (var (region, number) in ExampleNumbers)
            {
                var allowed = budget(number);
                var allocated = Measure(() => call(number));
                if (allocated > allowed)
                    overBudget.Add($"{region} (+{number.CountryCode} {number.NationalNumber}): {allocated} B, budget {allowed} B");
            }
            Assert.True(
                overBudget.Count == 0,
                $"{api} allocated over budget for {overBudget.Count} of {ExampleNumbers.Length} regions:\n" +
                string.Join("\n", overBudget));
        }

        [Fact]
        public void IsPossibleNumberDoesNotAllocate()
        {
            AssertWithinBudget(nameof(PhoneNumberUtil.IsPossibleNumber), _ => 0,
                n => PhoneUtil.IsPossibleNumber(n));
            AssertWithinBudget(nameof(PhoneNumberUtil.IsPossibleNumberWithReason), _ => 0,
                n => PhoneUtil.IsPossibleNumberWithReason(n));
            AssertWithinBudget(nameof(PhoneNumberUtil.IsPossibleNumberForType), _ => 0,
                n => PhoneUtil.IsPossibleNumberForType(n, PhoneNumberType.MOBILE));
        }

        [Fact]
        public void GetNationalSignificantNumberAllocatesOnlyItsResult()
        {
            AssertWithinBudget(nameof(PhoneNumberUtil.GetNationalSignificantNumber),
                n => SizeOf(PhoneUtil.GetNationalSignificantNumber(n)),
                n => PhoneUtil.GetNationalSignificantNumber(n));
        }

        /// <summary>
        /// Validation and typing materialise the national significant number to match it against the
        /// metadata patterns, and nothing else. Lowering these to zero means matching on a span; when
        /// that lands, tighten the budget here to 0.
        /// </summary>
        [Fact]
        public void ValidationAllocatesAtMostTheNationalSignificantNumber()
        {
            long Nsn(PhoneNumber n) => SizeOf(PhoneUtil.GetNationalSignificantNumber(n));
            AssertWithinBudget(nameof(PhoneNumberUtil.IsValidNumber), Nsn,
                n => PhoneUtil.IsValidNumber(n));
            AssertWithinBudget(nameof(PhoneNumberUtil.GetNumberType), Nsn,
                n => PhoneUtil.GetNumberType(n));
            AssertWithinBudget(nameof(PhoneNumberUtil.GetRegionCodeForNumber), Nsn,
                n => PhoneUtil.GetRegionCodeForNumber(n));
        }

        [Fact]
        public void FormatE164AllocatesOnlyItsResult()
        {
            AssertWithinBudget($"{nameof(PhoneNumberUtil.Format)}(E164)",
                n => SizeOf(PhoneUtil.Format(n, PhoneNumberFormat.E164)),
                n => PhoneUtil.Format(n, PhoneNumberFormat.E164));
        }
    }
}
