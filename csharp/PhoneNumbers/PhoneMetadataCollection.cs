using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PhoneNumbers
{
    [DebuggerNonUserCode]
    [CompilerGenerated]
    [GeneratedCode("ProtoGen", "2.3.0.277")]
    public class PhoneMetadataCollection
    {
        public const int MetadataFieldNumber = 1;
        private readonly List<PhoneMetadata> metadata_ = new List<PhoneMetadata>();

        public static PhoneMetadataCollection DefaultInstance { get; } = new Builder().BuildPartial();

        public PhoneMetadataCollection DefaultInstanceForType => DefaultInstance;

        protected PhoneMetadataCollection ThisMessage => this;

        public IList<PhoneMetadata> MetadataList => metadata_;

        public int MetadataCount => metadata_.Count;

        public bool IsInitialized => MetadataList.All(element => element.IsInitialized);

        public PhoneMetadata GetMetadata(int index)
        {
            return metadata_[index];
        }

        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        public Builder ToBuilder()
        {
            return CreateBuilder(this);
        }

        public Builder CreateBuilderForType()
        {
            return new Builder();
        }

        public static Builder CreateBuilder(PhoneMetadataCollection prototype)
        {
            return new Builder().MergeFrom(prototype);
        }

        [DebuggerNonUserCode]
        [CompilerGenerated]
        [GeneratedCode("ProtoGen", "2.3.0.277")]
        public class Builder
        {
            protected Builder ThisBuilder => this;

            protected PhoneMetadataCollection MessageBeingBuilt { get; private set; } = new PhoneMetadataCollection();

            public PhoneMetadataCollection DefaultInstanceForType => DefaultInstance;


            public IList<PhoneMetadata> MetadataList => MessageBeingBuilt.metadata_;

            public int MetadataCount => MessageBeingBuilt.MetadataCount;

            public Builder Clear()
            {
                MessageBeingBuilt = new PhoneMetadataCollection();
                return this;
            }

            public Builder Clone()
            {
                return new Builder().MergeFrom(MessageBeingBuilt);
            }

            public PhoneMetadataCollection Build()
            {
                return BuildPartial();
            }

            public PhoneMetadataCollection BuildPartial()
            {
                if (MessageBeingBuilt == null)
                    throw new InvalidOperationException("build() has already been called on this Builder");

                var returnMe = MessageBeingBuilt;
                MessageBeingBuilt = null;
                return returnMe;
            }


            public Builder MergeFrom(PhoneMetadataCollection other)
            {
                if (other == DefaultInstance) return this;
                if (other.metadata_.Count != 0) MessageBeingBuilt.metadata_.AddRange(other.metadata_);
                return this;
            }

            public PhoneMetadata GetMetadata(int index)
            {
                return MessageBeingBuilt.GetMetadata(index);
            }

            public Builder SetMetadata(int index, PhoneMetadata value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.metadata_[index] = value;
                return this;
            }

            public Builder SetMetadata(int index, PhoneMetadata.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.metadata_[index] = builderForValue.Build();
                return this;
            }

            public Builder AddMetadata(PhoneMetadata value)
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                MessageBeingBuilt.metadata_.Add(value);
                return this;
            }

            public Builder AddMetadata(PhoneMetadata.Builder builderForValue)
            {
                if (builderForValue == null) throw new ArgumentNullException(nameof(builderForValue));
                MessageBeingBuilt.metadata_.Add(builderForValue.Build());
                return this;
            }

            public Builder AddRangeMetadata(IEnumerable<PhoneMetadata> values)
            {
                MessageBeingBuilt.metadata_.AddRange(values);
                return this;
            }

            public Builder ClearMetadata()
            {
                MessageBeingBuilt.metadata_.Clear();
                return this;
            }
        }


        #region Lite runtime methods

        public override int GetHashCode()
        {
            var hash = GetType().GetHashCode();
            foreach (var i in metadata_)
                hash ^= i.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PhoneMetadataCollection;
            if (metadata_.Count != other?.metadata_.Count) return false;
            for (var ix = 0; ix < metadata_.Count; ix++)
                if (!metadata_[ix].Equals(other.metadata_[ix])) return false;
            return true;
        }

        #endregion
    }
}