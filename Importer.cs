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
            var files = SourceFolder.GetFiles($"*.{Extension}", SearchOption.AllDirectories);
            FileCount = files.Length;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Moving {FileCount} files, {files.Sum(f => f.Length / MiB):N0} MB");
            var filesWithMetadata = (from file in files
                                     let dateTaken = GetDateTaken(file.FullName)
                                     orderby dateTaken
                                     select new {file, dateTaken.Date})
                .ToList();
            var filesByDate = filesWithMetadata.GroupBy(x => x.Date, x => x.file);
            Counter = 0;
            foreach (var group in filesByDate)
                ImportDay(group);
            Console.ResetColor();
        }

        void ImportDay(IGrouping<DateTime, FileInfo> day)
        {
            var yearFolder = new DirectoryInfo(Path.Combine(TargetFolder.FullName,
                                                            day.Key.Year.ToString(CultureInfo.InvariantCulture)));
            var prefix = day.Key.ToString("yyyy-MM-dd-");
            var suffix = 0;
            if (yearFolder.Exists)
            {
                var existingFiles = yearFolder.GetFiles($"{prefix}???.{Extension}");
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
                var targetFullName = Path.Combine(yearFolder.FullName, $"{prefix}{suffix:000}.{Extension}");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{Counter}/{FileCount}] ");
                Console.ResetColor();
                Console.Write(fileInfo.FullName);
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(" -> ");
                Console.ResetColor();
                Console.Write(targetFullName);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" ({fileInfo.Length / MiB:N0} MB) ");
                Console.ResetColor();
                if (!Test)
                    fileInfo.MoveTo(targetFullName);
                Console.WriteLine("[OK]");
            }
        }

        static DateTime GetDateTaken(string fileName)
        {
            Console.ResetColor();
            Console.Write($"Reading {fileName} date ");
            try
            {
                using (var image = new Bitmap(fileName))
                {
                    var dateItem = image.GetPropertyItem(0x9003);
                    if (dateItem != null)
                    {
                        var dateText = Encoding.ASCII.GetString(dateItem.Value);
                        var dateTaken = DateTime.ParseExact(dateText, "yyyy:MM:dd HH:mm:ss\0", CultureInfo.InvariantCulture);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[EXIF]");
                        return dateTaken;
                    }
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Last Write]");
            }
            Console.ResetColor();
            return File.GetLastWriteTime(fileName);
        }
    }
}