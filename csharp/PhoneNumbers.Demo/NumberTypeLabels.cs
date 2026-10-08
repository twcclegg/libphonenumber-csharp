namespace PhoneNumbers.Demo;

/// <summary>Human-readable labels for <see cref="PhoneNumberType"/>, shared by every page that shows one.</summary>
public static class NumberTypeLabels
{
    public static string For(PhoneNumberType type) => type switch
    {
        PhoneNumberType.FIXED_LINE => "Fixed Line",
        PhoneNumberType.MOBILE => "Mobile",
        PhoneNumberType.FIXED_LINE_OR_MOBILE => "Fixed/Mobile",
        PhoneNumberType.TOLL_FREE => "Toll Free",
        PhoneNumberType.PREMIUM_RATE => "Premium Rate",
        PhoneNumberType.SHARED_COST => "Shared Cost",
        PhoneNumberType.VOIP => "VoIP",
        PhoneNumberType.PERSONAL_NUMBER => "Personal",
        PhoneNumberType.PAGER => "Pager",
        PhoneNumberType.UAN => "UAN",
        PhoneNumberType.VOICEMAIL => "Voicemail",
        _ => "Unknown"
    };
}
