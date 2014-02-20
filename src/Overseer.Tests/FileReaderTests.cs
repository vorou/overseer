using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class FileReaderTests : IDisposable
    {
        private readonly string SourceDir;

        public FileReaderTests()
        {
            SourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(SourceDir);
        }
        
        public void Dispose()
        {
            Directory.Delete(SourceDir, true);
        }

        [Fact]
        public void Read_ZipFileInRootDir_ReturnsArchiveNamePlusFileName()
        {
            using (var zip = ZipFile.Open(Path.Combine(SourceDir, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry("fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(@"archive.zip\fileName");
        }

        [Fact]
        public void Read_FileInSubDir_ReturnedPathIsRelativeToSourceDir()
        {
            var subDirPath = Path.Combine(SourceDir, "dir");
            Directory.CreateDirectory(subDirPath);
            using (var zip = ZipFile.Open(Path.Combine(subDirPath, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry("fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(@"dir\archive.zip\fileName");
        }

        [Fact]
        public void Read_Always_ReturnsUnzippedContent()
        {
            var content = "<привет></привет>";
            using (var zip = ZipFile.Open(Path.Combine(SourceDir, Path.GetRandomFileName()), ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry(Path.GetRandomFileName());
                using (var stream = new StreamWriter(entry.Open()))
                    stream.Write(content);
            }
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Content.ShouldBe(content);
        }

        private FileReader CreateSut()
        {
            return new FileReader(SourceDir);
        }
    }
}