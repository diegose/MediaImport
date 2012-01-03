using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
            var filesToCopy = (from file in SourceFolder.EnumerateFiles("*." + Extension, SearchOption.AllDirectories)
                               let dateTaken = GetDateTaken(file.FullName)
                               orderby dateTaken
                               select Tuple.Create(file, dateTaken.Date))
                .ToList();
            var filesByDate = filesToCopy.GroupBy(x => x.Item2, x => x.Item1);
            FileCount = filesToCopy.Count();
            Console.WriteLine("Copying {0} files, {1:N0} MB",
                              FileCount,
                              filesToCopy.Sum(f => f.Item1.Length / MiB));
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

        DateTime GetDateTaken(string fileName)
        {
            try
            {
                using (var image = new Bitmap(fileName))
                {
                    var dateItem = image.GetPropertyItem(0x9003);
                    if (dateItem != null)
                    {
                        var dateText = Encoding.ASCII.GetString(dateItem.Value);
                        return DateTime.ParseExact(dateText, "yyyy:MM:dd HH:mm:ss\0", CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Can't parse EXIF data for {0}", fileName);
            }
            return File.GetLastWriteTime(fileName);
        }
    }
}