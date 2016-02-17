using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaImport
{
    class Importer
    {
        readonly ImportOptions Options;
        int Counter;
        int FileCount;
        static readonly double MiB = Math.Pow(2, 20);

        public Importer(ImportOptions options)
        {
            Options = options;
        }

        public void Import()
        {
            var files = Options.SourceFolder.GetFiles($"*.{Options.Extension}", SearchOption.AllDirectories);
            FileCount = files.Length;
            Console.ForegroundColor = ConsoleColor.White;
            var operation = Options.Copy ? "Copying" : "Moving";
            var dryRunLegend = Options.DryRun ? " (not really!)" : "";
            Console.WriteLine($"{operation} {FileCount} files{dryRunLegend}, {files.Sum(f => f.Length / MiB):N0} MB");
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
            var yearFolder = new DirectoryInfo(Path.Combine(Options.TargetFolder.FullName,
                                                            day.Key.Year.ToString(CultureInfo.InvariantCulture)));
            var prefix = day.Key.ToString("yyyy-MM-dd-");
            var suffix = 0;
            if (yearFolder.Exists)
            {
                var existingFiles = yearFolder.GetFiles($"{prefix}???.{Options.Extension}");
                suffix = existingFiles
                             .Select(f => (int?)Convert.ToInt32(f.Name.Substring(prefix.Length, 3)))
                             .Max() ?? 0;
            }
            else if (!Options.DryRun)
                yearFolder.Create();
            foreach (var fileInfo in day)
            {
                suffix++;
                Counter++;
                var targetFullName = Path.Combine(yearFolder.FullName, $"{prefix}{suffix:000}.{Options.Extension}");
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
                if (!Options.DryRun)
                    if (Options.Copy)
                        fileInfo.CopyTo(targetFullName);
                    else
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