using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class FileReaderTests
    {
        private readonly string FtpMountDir = @"D:\code\Overseer\src\Overseer.Tests\ftp";
        private readonly Uri Ftp = new Uri("ftp://localhost");

        public FileReaderTests()
        {
            RemoveDirectoryIfExists();
            Directory.CreateDirectory(FtpMountDir);
        }

        private void RemoveDirectoryIfExists()
        {
            if (Directory.Exists(FtpMountDir))
                Directory.Delete(FtpMountDir, true);
        }

        [Fact]
        public void Read_ZipInRootDir_IgnoresIt()
        {
            using (var zip = ZipFile.Open(Path.Combine(FtpMountDir, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry("fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Count().ShouldBe(0);
        }

        [Fact]
        public void Read_ZipInNotificationsCurrentMonth_PathIsUriPlusEntryName()
        {
            var subDirPath = Path.Combine(FtpMountDir, @"fcs_regions\Adygeja_Resp\notifications\currMonth\");
            Directory.CreateDirectory(subDirPath);
            using (var zip = ZipFile.Open(Path.Combine(subDirPath, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry("fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(Ftp + @"fcs_regions/Adygeja_Resp/notifications/currMonth/archive.zip/fileName");
        }

        [Fact]
        public void Read_ZipInNotificationsCurrentMonth_ReadsItsContent()
        {
            var subDirPath = Path.Combine(FtpMountDir, @"fcs_regions\Adygeja_Resp\notifications\currMonth\");
            Directory.CreateDirectory(subDirPath);
            var content = "<привет></привет>";
            using (var zip = ZipFile.Open(Path.Combine(FtpMountDir, @"fcs_regions\Adygeja_Resp\notifications\currMonth\panda.zip"), ZipArchiveMode.Create))
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
            return new FileReader(Ftp);
        }
    }
}