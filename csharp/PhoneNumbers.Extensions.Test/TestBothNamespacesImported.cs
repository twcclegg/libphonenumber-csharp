// ToE164 and IsValid below are the obsolete helpers this test sets the new ones against.
#pragma warning disable PNX0002
using PhoneNumbers.Extensions;
using Xunit;

namespace PhoneNumbers.Test.Extensions
{
    /// <summary>
    /// A file that imports both <c>PhoneNumbers</c> and <c>PhoneNumbers.Extensions</c>, as code
    /// written against the old API does. The new formatting helpers must not collide with the old
    /// ones there: had they reused a name such as <c>ToE164</c>, every call would be CS0121.
    /// (The simple name <c>PhoneNumber</c> is still ambiguous in such a file, CS0104, which is why
    /// nothing here spells it.)
    /// </summary>
    public class TestBothNamespacesImported
    {
        [Fact]
        public void OldAndNewFormattingHelpersCoexist()
        {
            var number = PhoneNumberUtil.GetInstance().Parse("+442070313000", null);

            Assert.Equal(number.ToE164(), number.Format(PhoneNumberFormat.E164));
            Assert.Equal(number.IsValid(), number.IsValidNumber());
        }
    }
}
