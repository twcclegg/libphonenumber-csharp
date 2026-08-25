using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace PhoneNumbers.PerformanceTest.Benchmarks;

/// <summary>
/// Geocoding had no benchmark coverage at all. Two measurements: the end-to-end description
/// lookup a consumer actually calls, and <see cref="Locale.GetDisplayCountry"/> on its own,
/// which isolates the country-name table behind the fallback path.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class PhoneNumberOfflineGeocoderBenchmark
{
    private PhoneNumberOfflineGeocoder _geocoder = null!;
    private PhoneNumber[] _numbers = null!;
    private Locale[] _locales = null!;

    [Params(1000)]
    public int PhoneNumberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var phoneNumberUtil = PhoneNumberUtil.GetInstance();
        _geocoder = PhoneNumberOfflineGeocoder.GetInstance();

        var numbers = new List<PhoneNumber>(PhoneNumberCount);
        foreach (var benchmarkCase in PhoneNumberBenchmarkData.Create(phoneNumberUtil, PhoneNumberCount))
        {
            try
            {
                numbers.Add(phoneNumberUtil.Parse(benchmarkCase.NumberToParse, benchmarkCase.DefaultRegion));
            }
            catch (NumberParseException)
            {
                // Measuring geocoding, not exception throwing.
            }
        }

        _numbers = numbers.ToArray();

        // One locale per region that actually resolves, so the display-country benchmark walks
        // the whole table instead of repeatedly hitting one warm entry. AC and XK are phone
        // regions with no localised name, and they return empty rather than resolving, so the
        // Length check drops them.
        _locales = phoneNumberUtil.GetSupportedRegions()
            .OrderBy(regionCode => regionCode, StringComparer.Ordinal)
            .Select(regionCode => new Locale("en", regionCode))
            .Where(locale => locale.GetDisplayCountry("en").Length > 0)
            .ToArray();

        // Warm the lazily-loaded prefix maps so the benchmarks measure steady-state lookups.
        foreach (var number in _numbers)
        {
            _geocoder.GetDescriptionForNumber(number, Locale.English);
        }
    }

    [Benchmark]
    public int GetDescriptionForNumber()
    {
        var checksum = 0;
        for (var i = 0; i < _numbers.Length; i++)
            checksum += _geocoder.GetDescriptionForNumber(_numbers[i], Locale.English).Length;
        return checksum;
    }

    [Benchmark]
    public int GetDisplayCountry()
    {
        var checksum = 0;
        for (var i = 0; i < _locales.Length; i++)
            checksum += _locales[i].GetDisplayCountry("en").Length;
        return checksum;
    }
}
