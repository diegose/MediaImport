using CommandLine;

namespace MediaImport
{
    static class Program
    {
        static void Main(string[] args)
        {
            var options = new ImportOptions();
            if (Parser.Default.ParseArguments(args, options))
            {
                var importer = new Importer(options);
                importer.Import();
            }
        }
    }
}
