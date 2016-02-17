using System.IO;
using CommandLine;
using CommandLine.Text;

namespace MediaImport
{
    public class ImportOptions
    {
        public DirectoryInfo SourceFolder { get; set; }
        public DirectoryInfo TargetFolder { get; set; }

        [ValueOption(0)]
        public string SourceFolderName
        {
            get { return SourceFolder.FullName; }
            set { SourceFolder = new DirectoryInfo(value); }
        }

        [ValueOption(1)]
        public string TargetFolderName
        {
            get { return TargetFolder.FullName; }
            set { TargetFolder = new DirectoryInfo(value); }
        }

        [Option('e', "ext", HelpText = "File Extension", DefaultValue = "jpg")]
        public string Extension { get; set; }

        [Option('d', "dry-run", HelpText = "Do not actually move files")]
        public bool DryRun { get; set; }

        [Option('c', "copy", HelpText = "Copy files instead of moving them")]
        public bool Copy { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return "Usage: MediaImport source target" +
                   HelpText.AutoBuild(this,
                                      current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}