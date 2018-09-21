using System;
using Leniency=PhoneNumbers.PhoneNumberUtil.Leniency;
namespace PhoneNumbers
{
    public static class LeniencyExtensions
    {
        public static bool Verify(this Leniency leniency, PhoneNumber number, string candidate, PhoneNumberUtil util)
        {
            switch (leniency)
            {
                case Leniency.POSSIBLE:
                    return util.IsPossibleNumber(number);
                case Leniency.VALID:
                {
                    if (!util.IsValidNumber(number) ||
                        !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util))
                        return false;
                    return PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util);
                }
                case Leniency.STRICT_GROUPING:
                {
                    if (!util.IsValidNumber(number) ||
                        !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util) ||
                        PhoneNumberMatcher.ContainsMoreThanOneSlash(candidate) ||
                        !PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util))
                    {
                        return false;
                    }
                    return PhoneNumberMatcher.CheckNumberGroupingIsValid(
                        number, candidate, util, PhoneNumberMatcher.AllNumberGroupsRemainGrouped);
                }
                case Leniency.EXACT_GROUPING:
                {
                    if (!util.IsValidNumber(number) ||
                        !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util) ||
                        PhoneNumberMatcher.ContainsMoreThanOneSlash(candidate) ||
                        !PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util))
                    {
                        return false;
                    }
                    return PhoneNumberMatcher.CheckNumberGroupingIsValid(
                        number, candidate, util, PhoneNumberMatcher.AllNumberGroupsAreExactlyPresent);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(leniency), leniency, null);
            }
        }
    }
}