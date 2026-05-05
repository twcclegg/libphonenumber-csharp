#nullable disable
/*
 * Copyright (C) 2026 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// Binary serializer/deserializer for <see cref="PhoneMetadata"/> and its child messages.
    /// Mirrors Java's <c>Externalizable</c> approach: at build time the XML metadata is parsed and
    /// serialized to a compact binary representation; at runtime we read the binary directly so we
    /// avoid the cost (CPU + allocations) of XML parsing on first use.
    /// </summary>
    /// <remarks>
    /// The format is C#-specific (not wire-compatible with Java's <c>ObjectOutput</c>) because
    /// Java's framing buys us nothing here and complicates the writer. The format is versioned so
    /// it can evolve without breaking older consumers if a binary is shipped without rebuilding.
    /// </remarks>
    public static class BuildMetadataFromBin
    {
        // 4-byte magic + 1-byte version. Bumped if the schema below changes incompatibly.
        internal const int FormatMagic = 0x504E4D42; // 'P','N','M','B' big-endian
        internal const byte FormatVersion = 1;

        public static PhoneMetadata ReadMetadata(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var magic = reader.ReadInt32();
            if (magic != FormatMagic)
                throw new InvalidDataException($"Unexpected magic 0x{magic:X8}, expected 0x{FormatMagic:X8}.");
            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException($"Unsupported binary metadata version {version} (expected {FormatVersion}).");
            return ReadMetadataBody(reader);
        }

        public static void WriteMetadata(Stream stream, PhoneMetadata metadata)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(FormatMagic);
            writer.Write(FormatVersion);
            WriteMetadataBody(writer, metadata);
        }

        // --- PhoneMetadata --------------------------------------------------------------

        // Bits in the desc-presence bitmask. Order matches the field-number declarations on
        // PhoneMetadata so a future reader can drop the highest bits without breaking older files.
        [Flags]
        private enum DescFlags : uint
        {
            None                   = 0,
            GeneralDesc            = 1u << 0,
            FixedLine              = 1u << 1,
            Mobile                 = 1u << 2,
            TollFree               = 1u << 3,
            PremiumRate            = 1u << 4,
            SharedCost             = 1u << 5,
            PersonalNumber         = 1u << 6,
            Voip                   = 1u << 7,
            Pager                  = 1u << 8,
            Uan                    = 1u << 9,
            Emergency              = 1u << 10,
            Voicemail              = 1u << 11,
            ShortCode              = 1u << 12,
            StandardRate           = 1u << 13,
            CarrierSpecific        = 1u << 14,
            SmsServices            = 1u << 15,
            NoInternationalDialling = 1u << 16,
        }

        internal static void WriteMetadataBody(BinaryWriter writer, PhoneMetadata m)
        {
            // Compute presence mask first so the reader knows which descs to expect.
            var flags = DescFlags.None;
            if (m.HasGeneralDesc) flags |= DescFlags.GeneralDesc;
            if (m.HasFixedLine) flags |= DescFlags.FixedLine;
            if (m.HasMobile) flags |= DescFlags.Mobile;
            if (m.HasTollFree) flags |= DescFlags.TollFree;
            if (m.HasPremiumRate) flags |= DescFlags.PremiumRate;
            if (m.HasSharedCost) flags |= DescFlags.SharedCost;
            if (m.HasPersonalNumber) flags |= DescFlags.PersonalNumber;
            if (m.HasVoip) flags |= DescFlags.Voip;
            if (m.HasPager) flags |= DescFlags.Pager;
            if (m.HasUan) flags |= DescFlags.Uan;
            if (m.HasEmergency) flags |= DescFlags.Emergency;
            if (m.HasVoicemail) flags |= DescFlags.Voicemail;
            if (m.HasShortCode) flags |= DescFlags.ShortCode;
            if (m.HasStandardRate) flags |= DescFlags.StandardRate;
            if (m.HasCarrierSpecific) flags |= DescFlags.CarrierSpecific;
            if (m.HasSmsServices) flags |= DescFlags.SmsServices;
            if (m.HasNoInternationalDialling) flags |= DescFlags.NoInternationalDialling;
            writer.Write((uint)flags);

            if ((flags & DescFlags.GeneralDesc) != 0) WriteDesc(writer, m.GeneralDesc);
            if ((flags & DescFlags.FixedLine) != 0) WriteDesc(writer, m.FixedLine);
            if ((flags & DescFlags.Mobile) != 0) WriteDesc(writer, m.Mobile);
            if ((flags & DescFlags.TollFree) != 0) WriteDesc(writer, m.TollFree);
            if ((flags & DescFlags.PremiumRate) != 0) WriteDesc(writer, m.PremiumRate);
            if ((flags & DescFlags.SharedCost) != 0) WriteDesc(writer, m.SharedCost);
            if ((flags & DescFlags.PersonalNumber) != 0) WriteDesc(writer, m.PersonalNumber);
            if ((flags & DescFlags.Voip) != 0) WriteDesc(writer, m.Voip);
            if ((flags & DescFlags.Pager) != 0) WriteDesc(writer, m.Pager);
            if ((flags & DescFlags.Uan) != 0) WriteDesc(writer, m.Uan);
            if ((flags & DescFlags.Emergency) != 0) WriteDesc(writer, m.Emergency);
            if ((flags & DescFlags.Voicemail) != 0) WriteDesc(writer, m.Voicemail);
            if ((flags & DescFlags.ShortCode) != 0) WriteDesc(writer, m.ShortCode);
            if ((flags & DescFlags.StandardRate) != 0) WriteDesc(writer, m.StandardRate);
            if ((flags & DescFlags.CarrierSpecific) != 0) WriteDesc(writer, m.CarrierSpecific);
            if ((flags & DescFlags.SmsServices) != 0) WriteDesc(writer, m.SmsServices);
            if ((flags & DescFlags.NoInternationalDialling) != 0) WriteDesc(writer, m.NoInternationalDialling);

            writer.Write(m.Id ?? "");
            writer.Write(m.CountryCode);
            writer.Write(m.InternationalPrefix ?? "");
            writer.Write(m.PreferredInternationalPrefix ?? "");
            writer.Write(m.NationalPrefix ?? "");
            writer.Write(m.PreferredExtnPrefix ?? "");
            writer.Write(m.NationalPrefixForParsing ?? "");
            writer.Write(m.NationalPrefixTransformRule ?? "");
            writer.Write(m.SameMobileAndFixedLinePattern);
            writer.Write(m.MainCountryForCode);
            writer.Write(m.LeadingDigits ?? "");
            writer.Write(m.MobileNumberPortableRegion);

            writer.Write(m.NumberFormatList.Count);
            for (var i = 0; i < m.NumberFormatList.Count; i++)
                WriteFormat(writer, m.NumberFormatList[i]);
            writer.Write(m.IntlNumberFormatList.Count);
            for (var i = 0; i < m.IntlNumberFormatList.Count; i++)
                WriteFormat(writer, m.IntlNumberFormatList[i]);
        }

        internal static PhoneMetadata ReadMetadataBody(BinaryReader reader)
        {
            var m = new PhoneMetadata();
            var flags = (DescFlags)reader.ReadUInt32();

            if ((flags & DescFlags.GeneralDesc) != 0) m.GeneralDesc = ReadDesc(reader);
            if ((flags & DescFlags.FixedLine) != 0) m.FixedLine = ReadDesc(reader);
            if ((flags & DescFlags.Mobile) != 0) m.Mobile = ReadDesc(reader);
            if ((flags & DescFlags.TollFree) != 0) m.TollFree = ReadDesc(reader);
            if ((flags & DescFlags.PremiumRate) != 0) m.PremiumRate = ReadDesc(reader);
            if ((flags & DescFlags.SharedCost) != 0) m.SharedCost = ReadDesc(reader);
            if ((flags & DescFlags.PersonalNumber) != 0) m.PersonalNumber = ReadDesc(reader);
            if ((flags & DescFlags.Voip) != 0) m.Voip = ReadDesc(reader);
            if ((flags & DescFlags.Pager) != 0) m.Pager = ReadDesc(reader);
            if ((flags & DescFlags.Uan) != 0) m.Uan = ReadDesc(reader);
            if ((flags & DescFlags.Emergency) != 0) m.Emergency = ReadDesc(reader);
            if ((flags & DescFlags.Voicemail) != 0) m.Voicemail = ReadDesc(reader);
            if ((flags & DescFlags.ShortCode) != 0) m.ShortCode = ReadDesc(reader);
            if ((flags & DescFlags.StandardRate) != 0) m.StandardRate = ReadDesc(reader);
            if ((flags & DescFlags.CarrierSpecific) != 0) m.CarrierSpecific = ReadDesc(reader);
            if ((flags & DescFlags.SmsServices) != 0) m.SmsServices = ReadDesc(reader);
            if ((flags & DescFlags.NoInternationalDialling) != 0) m.NoInternationalDialling = ReadDesc(reader);

            m.Id = reader.ReadString();
            m.CountryCode = reader.ReadInt32();
            m.InternationalPrefix = reader.ReadString();
            m.PreferredInternationalPrefix = reader.ReadString();
            m.NationalPrefix = reader.ReadString();
            m.PreferredExtnPrefix = reader.ReadString();
            m.NationalPrefixForParsing = reader.ReadString();
            m.NationalPrefixTransformRule = reader.ReadString();
            m.SameMobileAndFixedLinePattern = reader.ReadBoolean();
            m.MainCountryForCode = reader.ReadBoolean();
            m.LeadingDigits = reader.ReadString();
            m.MobileNumberPortableRegion = reader.ReadBoolean();

            var numberFormatCount = reader.ReadInt32();
            for (var i = 0; i < numberFormatCount; i++)
                m.numberFormat_.Add(ReadFormat(reader));
            var intlNumberFormatCount = reader.ReadInt32();
            for (var i = 0; i < intlNumberFormatCount; i++)
                m.intlNumberFormat_.Add(ReadFormat(reader));

            return m;
        }

        // --- PhoneNumberDesc ------------------------------------------------------------

        // Bit flags for the optional string fields on PhoneNumberDesc. We can't rely on
        // empty-string sentinels because BuildMetadataFromXml distinguishes "absent" (null) from
        // "empty" (set to ""), and that distinction is observable via HasNationalNumberPattern.
        [Flags]
        private enum DescFieldFlags : byte
        {
            None                = 0,
            NationalNumberPattern = 1 << 0,
            ExampleNumber       = 1 << 1,
        }

        internal static void WriteDesc(BinaryWriter writer, PhoneNumberDesc desc)
        {
            var flags = DescFieldFlags.None;
            if (desc.HasNationalNumberPattern) flags |= DescFieldFlags.NationalNumberPattern;
            if (desc.HasExampleNumber) flags |= DescFieldFlags.ExampleNumber;
            writer.Write((byte)flags);

            if ((flags & DescFieldFlags.NationalNumberPattern) != 0)
                writer.Write(desc.NationalNumberPattern);
            if ((flags & DescFieldFlags.ExampleNumber) != 0)
                writer.Write(desc.ExampleNumber);

            writer.Write(desc.PossibleLengthList.Count);
            for (var i = 0; i < desc.PossibleLengthList.Count; i++)
                writer.Write(desc.PossibleLengthList[i]);
            writer.Write(desc.PossibleLengthLocalOnlyList.Count);
            for (var i = 0; i < desc.PossibleLengthLocalOnlyList.Count; i++)
                writer.Write(desc.PossibleLengthLocalOnlyList[i]);
        }

        internal static PhoneNumberDesc ReadDesc(BinaryReader reader)
        {
            var desc = new PhoneNumberDesc();
            var flags = (DescFieldFlags)reader.ReadByte();
            if ((flags & DescFieldFlags.NationalNumberPattern) != 0)
                desc.NationalNumberPattern = reader.ReadString();
            if ((flags & DescFieldFlags.ExampleNumber) != 0)
                desc.ExampleNumber = reader.ReadString();

            var possibleLengthCount = reader.ReadInt32();
            for (var i = 0; i < possibleLengthCount; i++)
                desc.possibleLength_.Add(reader.ReadInt32());
            var localOnlyCount = reader.ReadInt32();
            for (var i = 0; i < localOnlyCount; i++)
                desc.possibleLengthLocalOnly_.Add(reader.ReadInt32());

            return desc;
        }

        // --- NumberFormat ---------------------------------------------------------------

        internal static void WriteFormat(BinaryWriter writer, NumberFormat format)
        {
            // All NumberFormat string fields use empty-string-as-absent semantics already, and the
            // bool field is straightforward, so no presence bitmask is needed.
            writer.Write(format.Pattern ?? "");
            writer.Write(format.Format ?? "");
            writer.Write(format.LeadingDigitsPatternList.Count);
            for (var i = 0; i < format.LeadingDigitsPatternList.Count; i++)
                writer.Write(format.LeadingDigitsPatternList[i] ?? "");
            writer.Write(format.NationalPrefixFormattingRule ?? "");
            writer.Write(format.NationalPrefixOptionalWhenFormatting);
            writer.Write(format.DomesticCarrierCodeFormattingRule ?? "");
        }

        internal static NumberFormat ReadFormat(BinaryReader reader)
        {
            var format = new NumberFormat
            {
                Pattern = reader.ReadString(),
                Format = reader.ReadString(),
            };
            var leadingDigitsCount = reader.ReadInt32();
            for (var i = 0; i < leadingDigitsCount; i++)
                format.leadingDigitsPattern_.Add(reader.ReadString());
            format.NationalPrefixFormattingRule = reader.ReadString();
            format.NationalPrefixOptionalWhenFormatting = reader.ReadBoolean();
            format.DomesticCarrierCodeFormattingRule = reader.ReadString();
            return format;
        }
    }
}
