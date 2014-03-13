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
        private readonly string FtpMountDir = @"D:\code\Overseer\src\Overseer.Doorkeeper.Tests\ftp";
        private readonly string SomeRegionDir = @"fcs_regions\Adygeja_Resp\notifications\currMonth\";
        private static readonly Uri FtpUri = new Uri("ftp://localhost");

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

        public class FileRetrieving : FileReaderTests
        {
            [Fact]
            public void Read_ZipInRootDir_IgnoresIt()
            {
                CreateZipAtFtp(@".", GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
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

                actual.Single().Uri.ShouldBe(expected);
            }

            [Theory]
            [InlineData(@"fcs_regions\Adygeja_Resp\notifications\prevMonth\", "archive.zip", "entry",
                @"ftp://localhost/fcs_regions/Adygeja_Resp/notifications/prevMonth/archive.zip/entry")]
            public void Read_ZipInNotificationsPrevMonth_PathIsUriPlusEntryName(string targetDirectory, string zipName, string entryName, string expected)
            {
                CreateZipAtFtp(targetDirectory, zipName, entryName);
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.Single().Uri.ShouldBe(expected);
            }

            [Fact]
            public void Read_ZipInNotificationsCurrentMonth_ReadsItsContent()
            {
                var content = "<привет></привет>";
                CreateZipAtFtp(SomeRegionDir,
                               GetRandomZipName(),
                               zip =>
                               {
                                   var entry = zip.CreateEntry(Path.GetRandomFileName());
                                   using (var stream = new StreamWriter(entry.Open()))
                                       stream.Write(content);
                               });
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.Single().Content.ShouldBe(content);
            }
        }

        public class ImportControl : FileReaderTests
        {
            [Fact]
            public void ImportControl_SecondReadZipWasntMarkedAsImported_ReadsItAgain()
            {
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();
                sut.ReadNewFiles().ToList();

                var actual = sut.ReadNewFiles();

                actual.ShouldNotBeEmpty();
            }

            [Fact]
            public void ImportControl_ReaderWasReset_ReadsAgain()
            {
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();
                sut.ReadNewFiles().ToList();
                sut.Reset();

                var actual = sut.ReadNewFiles();

                actual.ShouldNotBeEmpty();
            }

            [Fact]
            public void ImportControl_ZipHasOnlyOneEntryItsMarkedAsImported_ReturnsEmpty()
            {
                CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", "entry");
                var sut = CreateSut();
                sut.ReadNewFiles().ToList();
                sut.MarkImported("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip/entry");

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void ImportControl_ZipHasTwoEntriesOneMarkedAsImported_ReadsMarkedEntryAgain()
            {
                CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\",
                               "panda.zip",
                               zip =>
                               {
                                   zip.CreateEntry("yo");
                                   zip.CreateEntry(Path.GetRandomFileName());
                               });

                var entryUrl = "ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip/yo";
                var sut = CreateSut();
                sut.ReadNewFiles().ToList();
                sut.MarkImported(entryUrl);

                var actual = sut.ReadNewFiles().ToList();

                actual.ShouldContain(f => f.Uri == entryUrl);
            }

            [Fact]
            public void ImportControl_ZipHadZeroLengthThenEntryWasAdded_WontReadItAgain()
            {
                var dir = SomeRegionDir;
                var fileName = GetRandomZipName();
                var path = PrepareForFile(dir, fileName);
                File.WriteAllBytes(path, new byte[0]);
                var sut = CreateSut();
                sut.ReadNewFiles().ToList();

                CreateZipAtFtp(dir, fileName, zip => zip.CreateEntry(Path.GetRandomFileName()));

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
            }
        }

        public class BadStuff : FileReaderTests
        {
            [Fact]
            public void Read_BadZip_ReturnsEmpty()
            {
                var path = PrepareForFile(SomeRegionDir, GetRandomZipName());
                File.WriteAllText(path, "bad zip content");
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_BadZipAndGoodZip_ReturnsOne()
            {
                var currMonth = SomeRegionDir;
                CreateZipAtFtp(currMonth, GetRandomZipName(), Path.GetRandomFileName());
                var path = PrepareForFile(currMonth, GetRandomZipName());
                File.WriteAllText(path, "bad zip content");
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.Count().ShouldBe(1);
            }

            [Fact]
            public void Read_Always_IgnoresLogsDir()
            {
                var logsDir = @"fcs_regions\_logs\notifications\currMonth\";
                CreateZipAtFtp(logsDir, GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_FailedToDownloadFile_ReturnsEmpty()
            {
                var sut = CreateTestableSut();
                sut.GetFileCoreBody = (client, uri) => { throw new WebException(); };
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());

                var actual = sut.ReadNewFiles();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_FailedToDownloadFile_ContinueToNextFile()
            {
                var sut = CreateTestableSut();
                var shouldThrow = true;
                sut.GetFileCoreBody = (client, uri) =>
                                      {
                                          if (shouldThrow)
                                          {
                                              shouldThrow = false;
                                              throw new WebException();
                                          }
                                          return GetValidNonEmptyZipBytes();
                                      };
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());

                var actual = sut.ReadNewFiles();

                actual.Count().ShouldBe(1);
            }

            private static FileReaderTestable CreateTestableSut()
            {
                return new FileReaderTestable(FtpUri);
            }
        }

        private static string GetRandomZipName()
        {
            return Path.ChangeExtension(Path.GetRandomFileName(), "zip");
        }

        private static byte[] GetValidNonEmptyZipBytes()
        {
            using (var zipContent = new MemoryStream())
            {
                using (var zip = new ZipArchive(zipContent, ZipArchiveMode.Create))
                    zip.CreateEntry(Path.GetRandomFileName());
                return zipContent.ToArray();
            }
        }

        private static FileReader CreateSut()
        {
            return new FileReader(FtpUri);
        }

        private void CreateZipAtFtp(string dirPath, string zipName, string zipEntryName)
        {
            CreateZipAtFtp(dirPath, zipName, z => z.CreateEntry(zipEntryName));
        }

        private void CreateZipAtFtp(string dirPath, string zipName, Action<ZipArchive> zipFiller)
        {
            var fullPath = PrepareForFile(dirPath, zipName);
            using (var zip = ZipFile.Open(fullPath, ZipArchiveMode.Update))
                zipFiller(zip);
        }

        private string PrepareForFile(string dirPath, string zipName)
        {
            var fullDirPath = Path.Combine(FtpMountDir, dirPath);
            Directory.CreateDirectory(fullDirPath);
            var fullPath = Path.Combine(FtpMountDir, fullDirPath, zipName);
            return fullPath;
        }
    }
}