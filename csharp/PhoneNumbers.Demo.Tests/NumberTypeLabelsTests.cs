using PhoneNumbers.Demo;
using Xunit;

namespace PhoneNumbers.Demo.Tests;

public class NumberTypeLabelsTests
{
    [Theory]
    [InlineData(PhoneNumberType.FIXED_LINE_OR_MOBILE, "Fixed/Mobile")]
    [InlineData(PhoneNumberType.VOIP, "VoIP")]
    [InlineData(PhoneNumberType.UNKNOWN, "Unknown")]
    public void labels_each_number_type_for_display(PhoneNumberType type, string label)
    {
        Assert.Equal(label, NumberTypeLabels.For(type));
    }

    [Fact]
    public void every_number_type_has_a_label()
    {
        var unlabelled = Enum.GetValues<PhoneNumberType>()
            .Where(t => t != PhoneNumberType.UNKNOWN && NumberTypeLabels.For(t) == "Unknown");

        Assert.Empty(unlabelled);
    }
}
