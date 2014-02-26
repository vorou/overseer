using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
    public class FileReaderTests
    {
        private readonly string FtpMountDir = @"D:\code\Overseer\src\Overseer.Tests\ftp";
        private readonly Uri FtpUri = new Uri("ftp://localhost");

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

        [Theory]
        [InlineData(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", @"fcs_regions/Adygeja_Resp/notifications/currMonth/archive.zip/")]
        [InlineData(@"fcs_regions\Panda_Country\notifications\currMonth\", @"fcs_regions/Panda_Country/notifications/currMonth/archive.zip/")]
        public void Read_ZipInNotificationsCurrentMonth_PathIsUriPlusEntryName(string targetDirectory, string targetDirUri)
        {
            var subDirPath = Path.Combine(FtpMountDir, targetDirectory);
            Directory.CreateDirectory(subDirPath);
            var fileName = "fileName";
            using (var zip = ZipFile.Open(Path.Combine(subDirPath, "archive.zip"), ZipArchiveMode.Create))
                zip.CreateEntry(fileName);
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(FtpUri + targetDirUri + fileName);
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

        [Fact]
        public void UnitOfWork_StateUnderTest_ExpectedBehavior()
        {
            var request = new WebClient {Credentials = new NetworkCredential("free", "free")};
            var newFileData =
                request.DownloadData(
                                     new Uri(
                                         "ftp://ftp.zakupki.gov.ru/fcs_regions/Adygeja_Resp/notifications/currMonth/notification_Adygeja_Resp_2014011100_2014011200_001.xml.zip"));
        }

        private FileReader CreateSut()
        {
            return new FileReader(FtpUri);
        }
    }
}