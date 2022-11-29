namespace PhoneNumbers.Extensions
{
    public static class PhoneNumber
    {
        private static readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

        public static bool TryParse(string input, string region, out PhoneNumbers.PhoneNumber number)
        {
            try
            {
                number = PhoneNumberUtil.Parse(input, region);
                return true;
            }
            catch
            {
                number = null;
                return false;
            }
        }

        public static bool TryParseValid(string input, string region, out PhoneNumbers.PhoneNumber number)
        {
            try
            {
                number = PhoneNumberUtil.Parse(input, region);
                return PhoneNumberUtil.IsValidNumber(number);
            }
            catch
            {
                number = null;
                return false;
            }
        }
    }
}
