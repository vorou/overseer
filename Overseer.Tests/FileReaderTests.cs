using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Overseer.Tests
{
    public class FileReaderTests
    {
        private string SourceDir;

        [SetUp]
        public void SetUp()
        {
            SourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(SourceDir);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(SourceDir, true);
        }

        [Test]
        public void Read_ZipFileInRootDir_ReturnsArchiveNamePlusFileName()
        {
            using (var zip = ZipFile.Open(Path.Combine(SourceDir, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry("fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(@"archive.zip\fileName");
        }

        [Test]
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

        [Test]
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