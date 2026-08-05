#if NETSTANDARD2_0
namespace PhoneNumbers
{
    /// <summary>
    /// TEMPORARY. Public on netstandard2.0 only, so the net8.0 and net10.0 assets are missing a
    /// member the netstandard2.0 asset has. Package validation must fail on this; the commit is
    /// reverted once it has.
    /// </summary>
    public static class PackageValidationProbe
    {
        public static int Value => 1;
    }
}
#endif
