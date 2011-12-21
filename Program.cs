using System;
using System.IO;

namespace MediaImport
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine(@"Usage: MediaImport source target extension [/test]");
            }
            else
            {
                var sourceFolder = new DirectoryInfo(args[0]);
                var targetFolder = new DirectoryInfo(args[1]);
                var extension = args[2];
                var test = args.Length == 4 && "/test".Equals(args[3], StringComparison.InvariantCultureIgnoreCase);
                var importer = new Importer(sourceFolder, targetFolder, extension, test);
                importer.Import();
            }
        }
    }
}
