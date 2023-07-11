using System.Text.RegularExpressions;

namespace PhoneNumbers
{

#if NET7_0_OR_GREATER
    public static partial class RegexHelper
#else
    public static class RegexHelper
#endif
    {

#if NET7_0_OR_GREATER

        [GeneratedRegex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", RegexOptions.CultureInvariant)]
        private static partial Regex PubPagesGenerated();

        [GeneratedRegex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", RegexOptions.CultureInvariant)]
        private static partial Regex SlashSeparatedDatesGenerated();

        [GeneratedRegex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", RegexOptions.CultureInvariant)]
        private static partial Regex TimeStampsGenerated();

        [GeneratedRegex(":[0-5]\\d", RegexOptions.CultureInvariant)]
        private static partial Regex TimeStampsSuffixGenerated();

        [GeneratedRegex("^(?::[0-5]\\d)$", RegexOptions.CultureInvariant)]
        private static partial Regex TimeStampsSuffixAllGenerated();

        [GeneratedRegex("^(?::[0-5]\\d)", RegexOptions.CultureInvariant)]
        private static partial Regex TimeStampsSuffixBeginGenerated();

        [GeneratedRegex("(?:[(\\[（［])?(?:[^(\\[（［)\\]）］]+[)\\]）］])?[^(\\[（［)\\]）］]+(?:[(\\[（［][^(\\[（［)\\]）］]+[)\\]）］]){0,3}[^(\\[（［)\\]）］]*", RegexOptions.CultureInvariant)]
        private static partial Regex MissingBracketsGenerated();

        [GeneratedRegex("^(?:(?:[(\\[（［])?(?:[^(\\[（［)\\]）］]+[)\\]）］])?[^(\\[（［)\\]）］]+(?:[(\\[（［][^(\\[（［)\\]）］]+[)\\]）］]){0,3}[^(\\[（［)\\]）］]*)", RegexOptions.CultureInvariant)]
        private static partial Regex MissingBracketsBeginGenerated();

        [GeneratedRegex("^(?:(?:[(\\[（［])?(?:[^(\\[（［)\\]）］]+[)\\]）］])?[^(\\[（［)\\]）］]+(?:[(\\[（［][^(\\[（［)\\]）］]+[)\\]）］]){0,3}[^(\\[（［)\\]）］]*)$", RegexOptions.CultureInvariant)]
        private static partial Regex MissingBracketsAllGenerated();

        [GeneratedRegex("[(\\[（［+＋]")]
        private static partial Regex LeadClassGenerated();

        [GeneratedRegex("(?:[(\\[（［+＋])")]
        private static partial Regex LeadClassBeginGenerated();

        [GeneratedRegex("(?:[(\\[（［+＋])$")]
        private static partial Regex LeadClassAllGenerated();

        [GeneratedRegex("\\p{Z}[^(\\[（［+＋\\p{Nd}]*")]
        private static partial Regex GroupSeparatorGenerator();

        [GeneratedRegex("(?:[(\\[（［+＋][-x‐-―−ー－-／  ­​⁠　()（）［］.\\[\\]/~⁓∼～]{0,4}){0,2}\\p{Nd}{1, 20}(?:[-x‐-―−ー－-／  ­​⁠　()（）［］.\\[\\]/~⁓∼～]{0,4}\\p{Nd}{1, 20}){0, 20}(?:;ext=(\\p{Nd}{1,20})|[  \\t,]*(?:e?xt(?:ensi(?:ó?|ó))?n?|ｅ?ｘｔｎ?|доб|anexo)[:\\.．]?[  \\t,-]*(\\p{Nd}{1,20})#?|[  \\t,]*(?:[xｘ#＃~～]|int|ｉｎｔ)[:\\.．]?[  \\t,-]*(\\p{Nd}{1,9})#?|[- ]+(\\p{Nd}{1,6})#)?")]
        private static partial Regex PatternGenerator();
#endif

        public static Regex PubPagesRegex()
        {
#if NET7_0_OR_GREATER
            return PubPagesGenerated();
#else
            return new Regex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", InternalRegexOptions.Default);
#endif
        }

        public static Regex SlashSeparatedDatesRegex()
        {
#if NET7_0_OR_GREATER
            return SlashSeparatedDatesGenerated();
#else
            return new Regex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", InternalRegexOptions.Default);
#endif
        }

        public static Regex TimeStampsRegex()
        {
#if NET7_0_OR_GREATER
            return TimeStampsGenerated();
#else
            return new Regex("\\d{1,5}-+\\d{1,5}\\s{0,4}\\(\\d{1,4}", InternalRegexOptions.Default);
#endif
        }

        public static PhoneRegex TimeStampsSuffixPhoneRegex()
        {
#if NET7_0_OR_GREATER
            return new PhoneRegex(TimeStampsSuffixGenerated(), TimeStampsSuffixAllGenerated(),
                TimeStampsSuffixBeginGenerated());
#else
            var pattern = ":[0-5]\\d";
            return new PhoneRegex(pattern, InternalRegexOptions.Default);
#endif
        }

        public static PhoneRegex MissingBracketsRegex()
        {
#if NET7_0_OR_GREATER
            return new PhoneRegex(MissingBracketsGenerated(), MissingBracketsBeginGenerated(),
                MissingBracketsAllGenerated());
#else
            const string openingParens = "(\\[\uFF08\uFF3B";
            const string closingParens = ")\\]\uFF09\uFF3D";
            const string nonParens = "[^" + openingParens + closingParens + "]";
            const string bracketPairLimit = "{0,3}";
            const string pattern = "(?:[" + openingParens + "])?" + "(?:" + nonParens + "+" +
                "[" + closingParens + "])?" + nonParens + "+" + "(?:[" + openingParens + "]" + nonParens +
                "+[" + closingParens + "])" + bracketPairLimit + nonParens + "*";
            return new PhoneRegex(pattern, InternalRegexOptions.Default);
#endif
        }
        
        
        internal static PhoneRegex LeadRegex()
        {
#if NET7_0_OR_GREATER
            return new PhoneRegex(LeadClassGenerated(), LeadClassBeginGenerated(), LeadClassAllGenerated());
#else
            const string openingParens = "(\\[\uFF08\uFF3B";
            const string leadClassChars = openingParens + PhoneNumberUtil.PLUS_CHARS;
            const string leadClass = "[" + leadClassChars + "]";
            return new PhoneRegex(leadClass, InternalRegexOptions.Default);
#endif
        }

        internal static Regex GroupSeparatorRegex()
        {
#if NET7_0_OR_GREATER
            return GroupSeparatorGenerator();
#else
            const string openingParens = "(\\[\uFF08\uFF3B";
            const string leadClassChars = openingParens + PhoneNumberUtil.PLUS_CHARS;
            return new Regex("\\p{Z}" + "[^" + leadClassChars + "\\p{Nd}]*", InternalRegexOptions.Default);
#endif
        }

        internal static Regex PatternRegex()
        {
#if NET7_0_OR_GREATER
            return PatternGenerator();
#else
            const string openingParens = "(\\[\uFF08\uFF3B";
            const string leadClassChars = openingParens + PhoneNumberUtil.PLUS_CHARS;
            const string leadClass = "[" + leadClassChars + "]";
            /* Limit on the number of leading (plus) characters. */
            const string leadLimit = "{0,2}";
            /* Limit on the number of consecutive punctuation characters. */
            const string punctuationLimit = "{0,4}";
            /* A punctuation sequence allowing white space. */
            /* The maximum number of digits allowed in a digit-separated block. As we allow all digits in a
            * single block, set high enough to accommodate the entire national number and the international
            * country code. */
            const string punctuation = "[" + PhoneNumberUtil.VALID_PUNCTUATION + "]" + punctuationLimit;
            const string digitSequence = "\\p{Nd}" + $"{{1, {PhoneNumberUtil.MAX_LENGTH_FOR_NUMBER_STR}}}";
            /* Limit on the number of blocks separated by punctuation. Uses digitBlockLimit since some
            * formats use spaces to separate each digit. */
            const string blockLimit = $"{{0, {PhoneNumberUtil.MAX_LENGTH_FOR_NUMBER_STR}}}";
            return new Regex(
                "(?:" + leadClass + punctuation + ")" + leadLimit +
                digitSequence + "(?:" + punctuation + digitSequence + ")" + blockLimit +
                "(?:" + PhoneNumberUtil.ExtnPatternsForMatching + ")?");
#endif
        }

        public static PhoneRegex Rfc3966Regex()
        {
            const string DIGITS = "\\p{Nd}";
            const string PLUS_SIGN_STR = "+";
            // Regular expression of valid global-number-digits for the phone-context parameter, following the
            // syntax defined in RFC3966.
            const string RFC3966_VISUAL_SEPARATOR = "[\\-\\.\\(\\)]?";
            const string RFC3966_PHONE_DIGIT =
                "(" + DIGITS + "|" + RFC3966_VISUAL_SEPARATOR + ")";
            const string RFC3966_GLOBAL_NUMBER_DIGITS =
                "^\\" + PLUS_SIGN_STR + RFC3966_PHONE_DIGIT + "*" + DIGITS + RFC3966_PHONE_DIGIT + "*$";
#if NET7_0_OR_GREATER
            return new PhoneRegex(new Regex(RFC3966_GLOBAL_NUMBER_DIGITS), new Regex("(?:" + RFC3966_GLOBAL_NUMBER_DIGITS + ")$"), new Regex("(?:" +RFC3966_GLOBAL_NUMBER_DIGITS + ")$"));
#else
            return new PhoneRegex(RFC3966_GLOBAL_NUMBER_DIGITS);
#endif
        }
    }
}
