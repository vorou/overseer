using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Overseer
{
    public class FileReader : IFileReader
    {
        private readonly string dir;

        public FileReader(string dir)
        {
            this.dir = dir;
        }

        public IEnumerable<SourceFile> ReadFiles()
        {
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var zipPath in files)
                using (var zip = ZipFile.OpenRead(zipPath))
                    foreach (var entry in zip.Entries)
                        yield return
                            new SourceFile
                            {
                                Path = Path.Combine(zipPath.Substring(dir.Length + 1), entry.Name),
                                Content = new StreamReader(entry.Open()).ReadToEnd()
                            };
        }
    }
}