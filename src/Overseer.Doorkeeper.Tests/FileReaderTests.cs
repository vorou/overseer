using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Doorkeeper.Tests
{
    //TODO: extract files-related stuff to separate class
    public class FileReaderTests
    {
        private readonly string FtpMountDir = @"D:\code\Overseer\src\Overseer.Tests\ftp";

        public FileReaderTests()
        {
            CreateSut().Reset();
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
            CreateZipAtFtp("", "archive.zip", "fileName");
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.Count().ShouldBe(0);
        }

        [Theory]
        [InlineData(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "archive.zip", "entry",
            @"ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/archive.zip/entry")]
        [InlineData(@"fcs_regions\Panda_Country\notifications\currMonth\", "archive.zip", "entry",
            @"ftp://localhost/fcs_regions/Panda_Country/notifications/currMonth/archive.zip/entry")]
        public void Read_ZipInNotificationsCurrentMonth_PathIsUriPlusEntryName(string targetDirectory, string zipName, string entryName, string expected)
        {
            CreateZipAtFtp(targetDirectory, zipName, entryName);
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.Single().Path.ShouldBe(expected);
        }

        [Theory]
        [InlineData(@"fcs_regions\Adygeja_Resp\notifications\prevMonth\", "archive.zip", "entry",
            @"ftp://localhost/fcs_regions/Adygeja_Resp/notifications/prevMonth/archive.zip/entry")]
        public void Read_ZipInNotificationsPrevMonth_PathIsUriPlusEntryName(string targetDirectory, string zipName, string entryName, string expected)
        {
            CreateZipAtFtp(targetDirectory, zipName, entryName);
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.Single().Path.ShouldBe(expected);
        }

        [Fact]
        public void Read_ZipInNotificationsCurrentMonth_ReadsItsContent()
        {
            var subDirPath = Path.Combine(FtpMountDir, @"fcs_regions\Adygeja_Resp\notifications\currMonth\");
            Directory.CreateDirectory(subDirPath);
            var content = "<привет></привет>";
            using (var zip = ZipFile.Open(Path.Combine(subDirPath, @"panda.zip"), ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry(Path.GetRandomFileName());
                using (var stream = new StreamWriter(entry.Open()))
                    stream.Write(content);
            }
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.Single().Content.ShouldBe(content);
        }

        [Fact]
        public void Read_ZipWasntMarkedAsImported_ReadsItAgain()
        {
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", Path.GetRandomFileName());
            var sut = CreateSut();
            sut.ReadNewFiles().ToList();

            var actual = sut.ReadNewFiles();

            actual.ShouldNotBeEmpty();
        }

        [Fact]
        public void Read_ReaderWasReset_ReadsAgain()
        {
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", Path.GetRandomFileName());
            var sut = CreateSut();
            sut.ReadNewFiles().ToList();
            sut.Reset();

            var actual = sut.ReadNewFiles();

            actual.ShouldNotBeEmpty();
        }

        [Fact]
        public void MarkImported_ZipEntryMarkedAsImported_ReturnsEmpty()
        {
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", "entry");
            var sut = CreateSut();

            sut.MarkImported("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip/entry");

            var actual = sut.ReadNewFiles();
            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_BadZip_ReturnsEmpty()
        {
            var fullDirPath = Path.Combine(FtpMountDir, @"fcs_regions\Adygeja_Resp\notifications\currMonth\");
            Directory.CreateDirectory(fullDirPath);
            File.WriteAllText(Path.Combine(fullDirPath, "bad.zip"), "bad zip content");
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_BadZipAndGoodZip_ReturnsGoodZip()
        {
            var currMonth = @"fcs_regions\Adygeja_Resp\notifications\currMonth\";
            CreateZipAtFtp(currMonth, "good.zip", Path.GetRandomFileName());
            var fullDirPath = Path.Combine(FtpMountDir, currMonth);
            Directory.CreateDirectory(fullDirPath);
            File.WriteAllText(Path.Combine(fullDirPath, "bad.zip"), "bad zip content");
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.Count().ShouldBe(1);
        }

        [Fact]
        public void Read_Always_IgnoresLogsDir()
        {
            var logsDir = @"fcs_regions\_logs\notifications\currMonth\";
            CreateZipAtFtp(logsDir, "some.zip", Path.GetRandomFileName());
            var sut = CreateSut();

            var actual = sut.ReadNewFiles();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_FailedToDownloadFile_ReturnsEmpty()
        {
            var sut = new FileReaderTestable(new Uri("ftp://localhost"));
            sut.GetFileCoreBody = (client, uri) =>
                                  {
                                      throw new WebException();
                                  };
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "some.zip", Path.GetRandomFileName());

            var actual = sut.ReadNewFiles();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_FailedToDownloadFile_ContinueToNextFile()
        {
            var sut = new FileReaderTestable(new Uri("ftp://localhost"));
            var shouldThrow = true;
            sut.GetFileCoreBody = (client, uri) =>
                                  {
                                      if (shouldThrow)
                                      {
                                          shouldThrow = false;
                                          throw new WebException();
                                      }
                                      return GetValidNonEmptyZipContent();
                                  };
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "some1.zip", Path.GetRandomFileName());
            CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "some2.zip", Path.GetRandomFileName());

            var actual = sut.ReadNewFiles();

            actual.Count().ShouldBe(1);
        }

        private static byte[] GetValidNonEmptyZipContent()
        {
            using (var zipContent = new MemoryStream())
            {
                using (var zip = new ZipArchive(zipContent, ZipArchiveMode.Create))
                {
                    zip.CreateEntry("entry");
                }
                return zipContent.ToArray();
            }
        }

        private FileReader CreateSut()
        {
            return new FileReader(new Uri("ftp://localhost"));
        }

        private void CreateZipAtFtp(string dirPath, string zipName, string zipEntryName)
        {
            var fullDirPath = Path.Combine(FtpMountDir, dirPath);
            Directory.CreateDirectory(fullDirPath);
            using (var zip = ZipFile.Open(Path.Combine(FtpMountDir, fullDirPath, zipName), ZipArchiveMode.Create))
                zip.CreateEntry(zipEntryName);
        }
    }
}