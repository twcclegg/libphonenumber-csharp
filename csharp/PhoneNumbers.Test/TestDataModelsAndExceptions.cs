using System;
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestDataModelsAndExceptions
    {
        [Fact]
        public void TestMissingMetadataException_PreservesMessageAndInnerException()
        {
            var innerEx = new Exception("Inner connection failed.");
            var ex = new MissingMetadataException("Metadata not found", innerEx);

            Assert.Equal("Metadata not found", ex.Message);
            Assert.Same(innerEx, ex.InnerException);

            var simpleEx = new MissingMetadataException("Just a message");
            Assert.Equal("Just a message", simpleEx.Message);
            Assert.Null(simpleEx.InnerException);
        }

        [Fact]
        public void TestPhoneMetadataCollection_CanAddAndClearMetadata()
        {
            var collection = new PhoneMetadataCollection.Builder()
                .AddMetadata(new PhoneMetadata.Builder().SetId("US").Build())
                .AddMetadata(new PhoneMetadata.Builder().SetId("CA").Build())
                .Build();

            Assert.Equal(2, collection.MetadataList.Count);
            Assert.Equal("US", collection.MetadataList[0].Id);
            Assert.Equal("CA", collection.MetadataList[1].Id);

            var clearedBuilder = collection.ToBuilder().Clear();
            Assert.Empty(clearedBuilder.Build().MetadataList);
        }

        [Fact]
        public void TestPhoneNumberBuilder_SupportsUpdatesAndClearing()
        {
            var builder = new PhoneNumber.Builder()
                .SetCountryCode(1)
                .SetNationalNumber(5551234567UL)
                .SetExtension("123")
                .SetNumberOfLeadingZeros(1)
                .SetRawInput("15551234567 ext 123");

            var number = builder.Build();
            Assert.Equal(1, number.CountryCode);
            Assert.Equal(5551234567UL, number.NationalNumber);
            Assert.Equal("123", number.Extension);
            Assert.Equal(1, number.NumberOfLeadingZeros);
            Assert.Equal("15551234567 ext 123", number.RawInput);

            // Verify mutating via ToBuilder creates logically equivalent structures
            var clonedBuilder = number.ToBuilder();
            var mergedBuilder = new PhoneNumber.Builder().MergeFrom(number);

            Assert.Equal(clonedBuilder.Build(), mergedBuilder.Build());

            // Clear should zero everything out
            clonedBuilder.Clear();
            var empty = clonedBuilder.Build();
            Assert.False(empty.HasCountryCode);
            Assert.False(empty.HasNationalNumber);
            Assert.False(empty.HasExtension);
        }

        [Fact]
        public void TestNumberFormatBuilder_ConfigurationWorkflow()
        {
            var format = new NumberFormat.Builder()
                .SetPattern("(\\d{3})(\\d{4})")
                .SetFormat("$1-$2")
                .AddLeadingDigitsPattern("^[2-9]")
                .SetNationalPrefixFormattingRule("0$1")
                .SetNationalPrefixOptionalWhenFormatting(true)
                .SetDomesticCarrierCodeFormattingRule("$CC $1")
                .Build();

            Assert.Equal("(\\d{3})(\\d{4})", format.Pattern);
            Assert.Equal("$1-$2", format.Format);
            Assert.Single(format.LeadingDigitsPatternList);
            Assert.Equal("^[2-9]", format.LeadingDigitsPatternList[0]);
            Assert.Equal("0$1", format.NationalPrefixFormattingRule);
            Assert.True(format.NationalPrefixOptionalWhenFormatting);
            Assert.Equal("$CC $1", format.DomesticCarrierCodeFormattingRule);

            var clonedFormat = format.ToBuilder().Build();
            var mergedFormat = new NumberFormat.Builder().MergeFrom(format).Build();

            Assert.Equal(format.Pattern, clonedFormat.Pattern);
            Assert.Equal(format.Pattern, mergedFormat.Pattern);

            Assert.Empty(format.ToBuilder().Clear().Build().LeadingDigitsPatternList);
        }
    }
}
