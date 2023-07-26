using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("PhoneNumbers.Test.AssemblyInit", "PhoneNumbers.Test")]

namespace PhoneNumbers.Test
{
    public sealed class AssemblyInit : XunitTestFramework
    {
        public AssemblyInit(IMessageSink messageSink) : base(messageSink)
        {
            // enable regex validation
            BuildMetadataFromXml.ValidPatterns = new HashSet<string>();
        }
    }
}