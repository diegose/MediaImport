using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaImport
{
    class Importer
    {
        readonly DirectoryInfo SourceFolder;
        readonly DirectoryInfo TargetFolder;
        readonly string Extension;
        readonly bool Test;
        int Counter;
        int FileCount;
        static readonly double MiB = Math.Pow(2, 20);

        public Importer(DirectoryInfo sourceFolder, DirectoryInfo targetFolder, string extension, bool test)
        {
            SourceFolder = sourceFolder;
            TargetFolder = targetFolder;
            Extension = extension;
            Test = test;
        }

        public void Import()
        {
            var filesToCopy = SourceFolder.GetFiles("*." + Extension, SearchOption.AllDirectories)
                .OrderBy(f => f.LastWriteTime)
                .ToList();
            var filesByDate = filesToCopy.GroupBy(f => f.LastWriteTime.Date);
            FileCount = filesToCopy.Count();
            Console.WriteLine("Copying {0} files, {1:N0} MB",
                              FileCount,
                              filesToCopy.Sum(f => f.Length / MiB));
            Counter = 0;
            foreach (var group in filesByDate)
                ImportDay(group);
        }

        void ImportDay(IGrouping<DateTime, FileInfo> day)
        {
            var yearFolder = new DirectoryInfo(Path.Combine(TargetFolder.FullName,
                                                            day.Key.Year.ToString(CultureInfo.InvariantCulture)));
            var prefix = day.Key.ToString("yyyy-MM-dd-");
            var suffix = 0;
            if (yearFolder.Exists)
            {
                var existingFiles = yearFolder.GetFiles(prefix + "???." + Extension);
                suffix = existingFiles
                             .Select(f => (int?)Convert.ToInt32(f.Name.Substring(prefix.Length, 3)))
                             .Max() ?? 0;
            }
            else if (!Test)
                yearFolder.Create();
            foreach (var fileInfo in day)
            {
                suffix++;
                Counter++;
                var targetName = prefix + suffix.ToString("000") + "." + Extension;
                var targetFullName = Path.Combine(yearFolder.FullName, targetName);
                Console.Write("[{0}/{1}] {2} -> {3} ({4:N0} MB)",
                              Counter,
                              FileCount,
                              fileInfo.FullName,
                              targetFullName,
                              fileInfo.Length / MiB);
                if (!Test)
                    fileInfo.MoveTo(targetFullName);
                Console.WriteLine("[OK]");
            }
        }
    }
}