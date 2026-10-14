// Parses, validates and formats one number, then reports whether the optional data sets are
// present. Kept to the core API so the trimmer can remove everything the opt-out asks it to.
using PhoneNumbers;

var util = PhoneNumberUtil.GetInstance();
var number = util.Parse("+14155552671", "US");
Console.WriteLine($"core: {util.IsValidNumber(number)} {util.Format(number, PhoneNumberFormat.INTERNATIONAL)}");

Report("geocoding", () => PhoneNumberOfflineGeocoder.GetInstance()
    .GetDescriptionForNumber(number, new Locale("en", "US")));
Report("localenames", () => new Locale("", "DE").GetDisplayCountry("en"));

static void Report(string dataSet, Func<string> use)
{
    try { Console.WriteLine($"{dataSet}: present ({use()})"); }
    catch (MissingMetadataException) { Console.WriteLine($"{dataSet}: absent"); }
}
