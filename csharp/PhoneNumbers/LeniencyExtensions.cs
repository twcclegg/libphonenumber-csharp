using System;
using Leniency=PhoneNumbers.PhoneNumberUtil.Leniency;
namespace PhoneNumbers
{
    public static class LeniencyExtensions
    {
        public static bool Verify(
            this Leniency leniency,
            PhoneNumber number,
            string candidate,
            PhoneNumberUtil util,
            PhoneNumberMatcher matcher)=>
            util.Verify(leniency, number, candidate, util, matcher);
    }
}