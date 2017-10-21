using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tools.Test")]


namespace Tools
{
    class Program
    {
        public static void Main()
        {
            new GeneratePhonePrefixData("fix").Run();
            new GenerateTimeZonesMapData("fix").Run();
        }
    }
}
