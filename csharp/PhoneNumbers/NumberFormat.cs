#nullable disable
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PhoneNumbers
{
    public sealed class NumberFormat
    {
        public const int PatternFieldNumber = 1;
        public const int FormatFieldNumber = 2;
        public const int LeadingDigitsPatternFieldNumber = 3;
        public const int NationalPrefixFormattingRuleFieldNumber = 4;
        public const int NationalPrefixOptionalWhenFormattingFieldNumber = 6;
        public const int DomesticCarrierCodeFormattingRuleFieldNumber = 5;

        internal NumberFormat Clone() => (NumberFormat)MemberwiseClone();

        internal readonly List<string> leadingDigitsPattern_ = new List<string>();

        public static NumberFormat DefaultInstance { get; } = new();

        public NumberFormat DefaultInstanceForType => DefaultInstance;

        public bool HasPattern => Pattern?.Length > 0;

        public string Pattern { get; internal set; } = "";

        public bool HasFormat => Format?.Length > 0;

        public string Format { get; internal set; } = "";

        public IList<string> LeadingDigitsPatternList => leadingDigitsPattern_;

        public int LeadingDigitsPatternCount => leadingDigitsPattern_.Count;

        public bool HasNationalPrefixFormattingRule => NationalPrefixFormattingRule?.Length > 0;

        public string NationalPrefixFormattingRule { get; internal set; } = "";

        public bool HasNationalPrefixOptionalWhenFormatting => NationalPrefixOptionalWhenFormatting;

        public bool NationalPrefixOptionalWhenFormatting { get; internal set; }

        public bool HasDomesticCarrierCodeFormattingRule => DomesticCarrierCodeFormattingRule?.Length > 0;

        public string DomesticCarrierCodeFormattingRule { get; internal set; } = "";

        public bool IsInitialized => HasPattern && HasFormat;

        public string GetLeadingDigitsPattern(int index) => leadingDigitsPattern_[index];

        public static Builder CreateBuilder() => new Builder();

        public Builder ToBuilder() => CreateBuilder(this);

        public Builder CreateBuilderForType() => new Builder();

        public static Builder CreateBuilder(NumberFormat prototype)
        {
            return new Builder().MergeFrom(prototype);
        }

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public class Builder
        {
            protected Builder ThisBuilder => this;

            internal protected NumberFormat MessageBeingBuilt { get; private set; } = new NumberFormat();

            public NumberFormat DefaultInstanceForType => DefaultInstance;


            public bool HasPattern => MessageBeingBuilt.HasPattern;

            public string Pattern
            {
                get => MessageBeingBuilt.Pattern;
                set => SetPattern(value);
            }

            public bool HasFormat => MessageBeingBuilt.HasFormat;

            public string Format
            {
                get => MessageBeingBuilt.Format;
                set => SetFormat(value);
            }

            public IList<string> LeadingDigitsPatternList => MessageBeingBuilt.leadingDigitsPattern_;

            public int LeadingDigitsPatternCount => MessageBeingBuilt.LeadingDigitsPatternCount;

            public bool HasNationalPrefixFormattingRule => MessageBeingBuilt.HasNationalPrefixFormattingRule;

            public string NationalPrefixFormattingRule
            {
                get => MessageBeingBuilt.NationalPrefixFormattingRule;
                set => SetNationalPrefixFormattingRule(value);
            }

            public bool HasNationalPrefixOptionalWhenFormatting => MessageBeingBuilt
                .HasNationalPrefixOptionalWhenFormatting;

            public bool NationalPrefixOptionalWhenFormatting
            {
                get => MessageBeingBuilt.NationalPrefixOptionalWhenFormatting;
                set => SetNationalPrefixOptionalWhenFormatting(value);
            }

            public bool HasDomesticCarrierCodeFormattingRule => MessageBeingBuilt.HasDomesticCarrierCodeFormattingRule;

            public string DomesticCarrierCodeFormattingRule
            {
                get => MessageBeingBuilt.DomesticCarrierCodeFormattingRule;
                set => SetDomesticCarrierCodeFormattingRule(value);
            }

            public Builder Clear()
            {
                MessageBeingBuilt = new NumberFormat();
                return this;
            }

            public Builder Clone()
            {
                return new Builder().MergeFrom(MessageBeingBuilt);
            }

            public NumberFormat Build()
            {
                return BuildPartial();
            }

            public NumberFormat BuildPartial()
            {
                if (MessageBeingBuilt == null)
                    throw new InvalidOperationException("build() has already been called on this Builder");

                var returnMe = MessageBeingBuilt;
                MessageBeingBuilt = null;
                return returnMe;
            }


            public Builder MergeFrom(NumberFormat other)
            {
                if (other == DefaultInstance) return this;
                if (other.HasPattern) Pattern = other.Pattern;
                if (other.HasFormat) Format = other.Format;
                if (other.leadingDigitsPattern_.Count != 0)
                    MessageBeingBuilt.leadingDigitsPattern_.AddRange(other.leadingDigitsPattern_);
                if (other.HasNationalPrefixFormattingRule)
                    NationalPrefixFormattingRule = other.NationalPrefixFormattingRule;
                if (other.HasNationalPrefixOptionalWhenFormatting)
                    NationalPrefixOptionalWhenFormatting = other.NationalPrefixOptionalWhenFormatting;
                if (other.HasDomesticCarrierCodeFormattingRule)
                    DomesticCarrierCodeFormattingRule = other.DomesticCarrierCodeFormattingRule;
                return this;
            }

            public Builder SetPattern(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Pattern = value;
                return this;
            }

            public Builder ClearPattern()
            {
                MessageBeingBuilt.Pattern = "";
                return this;
            }

            public Builder SetFormat(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.Format = value;
                return this;
            }

            public Builder ClearFormat()
            {
                MessageBeingBuilt.Format = "";
                return this;
            }

            public string GetLeadingDigitsPattern(int index)
            {
                return MessageBeingBuilt.GetLeadingDigitsPattern(index);
            }

            public Builder SetLeadingDigitsPattern(int index, string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.leadingDigitsPattern_[index] = value;
                return this;
            }

            public Builder AddLeadingDigitsPattern(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.leadingDigitsPattern_.Add(value);
                return this;
            }

            public Builder AddRangeLeadingDigitsPattern(IEnumerable<string> values)
            {
                MessageBeingBuilt.leadingDigitsPattern_.AddRange(values);
                return this;
            }

            public Builder ClearLeadingDigitsPattern()
            {
                MessageBeingBuilt.leadingDigitsPattern_.Clear();
                return this;
            }

            public Builder SetNationalPrefixFormattingRule(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.NationalPrefixFormattingRule = value;
                return this;
            }

            public Builder ClearNationalPrefixFormattingRule()
            {
                MessageBeingBuilt.NationalPrefixFormattingRule = "";
                return this;
            }

            public Builder SetNationalPrefixOptionalWhenFormatting(bool value)
            {
                MessageBeingBuilt.NationalPrefixOptionalWhenFormatting = value;
                return this;
            }

            public Builder ClearNationalPrefixOptionalWhenFormatting()
            {
                MessageBeingBuilt.NationalPrefixOptionalWhenFormatting = false;
                return this;
            }

            public Builder SetDomesticCarrierCodeFormattingRule(string value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.DomesticCarrierCodeFormattingRule = value;
                return this;
            }

            public Builder ClearDomesticCarrierCodeFormattingRule()
            {
                MessageBeingBuilt.DomesticCarrierCodeFormattingRule = "";
                return this;
            }
        }


        #region Lite runtime methods

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            if (HasPattern) hash ^= Pattern.GetHashCode();
            if (HasFormat) hash ^= Format.GetHashCode();
            foreach (var i in leadingDigitsPattern_)
                hash ^= i.GetHashCode();
            if (HasNationalPrefixFormattingRule) hash ^= NationalPrefixFormattingRule.GetHashCode();
            if (HasNationalPrefixOptionalWhenFormatting) hash ^= NationalPrefixOptionalWhenFormatting.GetHashCode();
            if (HasDomesticCarrierCodeFormattingRule) hash ^= DomesticCarrierCodeFormattingRule.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as NumberFormat;
            if (other == null) return false;
            if (HasPattern != other.HasPattern || HasPattern && !Pattern.Equals(other.Pattern)) return false;
            if (HasFormat != other.HasFormat || HasFormat && !Format.Equals(other.Format)) return false;
            if (leadingDigitsPattern_.Count != other.leadingDigitsPattern_.Count) return false;
            for (var ix = 0; ix < leadingDigitsPattern_.Count; ix++)
                if (!leadingDigitsPattern_[ix].Equals(other.leadingDigitsPattern_[ix])) return false;
            if (HasNationalPrefixFormattingRule != other.HasNationalPrefixFormattingRule ||
                HasNationalPrefixFormattingRule &&
                !NationalPrefixFormattingRule.Equals(other.NationalPrefixFormattingRule)) return false;
            if (HasNationalPrefixOptionalWhenFormatting != other.HasNationalPrefixOptionalWhenFormatting ||
                HasNationalPrefixOptionalWhenFormatting &&
                !NationalPrefixOptionalWhenFormatting.Equals(other.NationalPrefixOptionalWhenFormatting)) return false;
            if (HasDomesticCarrierCodeFormattingRule != other.HasDomesticCarrierCodeFormattingRule ||
                HasDomesticCarrierCodeFormattingRule &&
                !DomesticCarrierCodeFormattingRule.Equals(other.DomesticCarrierCodeFormattingRule)) return false;
            return true;
        }

        #endregion
    }
}
