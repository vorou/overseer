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
    public class FileReaderTests
    {
        private readonly string FtpMountDir = @"D:\code\Overseer\src\Overseer.Doorkeeper.Tests\ftp";
        private readonly string SomeRegionDir = @"fcs_regions\Adygeja_Resp\notifications\currMonth\";
        private static readonly Uri FtpUri = new Uri("ftp://localhost");

        public FileReaderTests()
        {
            ClearImport();
            ClearFtp();
        }

        private static void ClearImport()
        {
            CreateSut().Reset();
        }

        private void ClearFtp()
        {
            if (Directory.Exists(FtpMountDir))
                Directory.Delete(FtpMountDir, true);
            Directory.CreateDirectory(FtpMountDir);
        }

        public class FileRetrieving : FileReaderTests
        {
            [Fact]
            public void Read_ZipInRootDir_IgnoresIt()
            {
                CreateZipAtFtp(@".", GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_WrongFileExtension_IgnoresIt()
            {
                CreateZipAtFtp(SomeRegionDir, Path.GetRandomFileName(), Path.GetRandomFileName());
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

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

                var actual = sut.GetNewRaws();

                actual.Single().Uri.ToString().ShouldBe(expected);
            }

            [Theory]
            [InlineData(@"fcs_regions\Adygeja_Resp\notifications\prevMonth\", "archive.zip", "entry",
                @"ftp://localhost/fcs_regions/Adygeja_Resp/notifications/prevMonth/archive.zip/entry")]
            public void Read_ZipInNotificationsPrevMonth_PathIsUriPlusEntryName(string targetDirectory, string zipName, string entryName, string expected)
            {
                CreateZipAtFtp(targetDirectory, zipName, entryName);
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

                actual.Single().Uri.ToString().ShouldBe(expected);
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

                var actual = sut.GetNewRaws();

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
                sut.GetNewRaws().ToList();

                var actual = sut.GetNewRaws();

                actual.ShouldNotBeEmpty();
            }

            [Fact]
            public void ImportControl_ReaderWasReset_ReadsAgain()
            {
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();
                sut.GetNewRaws().ToList();
                sut.Reset();

                var actual = sut.GetNewRaws();

                actual.ShouldNotBeEmpty();
            }

            [Fact]
            public void ImportControl_ZipHasOnlyOneEntryItsMarkedAsImported_ReturnsEmpty()
            {
                CreateZipAtFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", "entry");
                var sut = CreateSut();
                sut.GetNewRaws().ToList();
                sut.MarkImported(new Uri("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip/entry"));

                var actual = sut.GetNewRaws();

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
                sut.GetNewRaws().ToList();
                sut.MarkImported(new Uri(entryUrl));

                var actual = sut.GetNewRaws().ToList();

                actual.ShouldContain(f => f.Uri.ToString() == entryUrl);
            }

            [Fact]
            public void ImportControl_ZipHadZeroLengthThenEntryWasAdded_WontReadItAgain()
            {
                var dir = SomeRegionDir;
                var fileName = GetRandomZipName();
                CreateFileAtFtp(dir, fileName, "");
                var sut = CreateSut();
                sut.GetNewRaws().ToList();

                CreateZipAtFtp(dir, fileName, zip => zip.CreateEntry(Path.GetRandomFileName()));

                var actual = sut.GetNewRaws();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void ImportControl_ZipHadNoEntriesThenEntryWasAdded_WontReadItAgain()
            {
                var dir = SomeRegionDir;
                var fileName = GetRandomZipName();
                CreateZipAtFtp(dir, fileName, _ => { });
                var sut = CreateSut();
                sut.GetNewRaws().ToList();

                CreateZipAtFtp(dir, fileName, zip => zip.CreateEntry(Path.GetRandomFileName()));

                var actual = sut.GetNewRaws();

                actual.ShouldBeEmpty();
            }
        }

        public class BadStuff : FileReaderTests
        {
            [Fact]
            public void Read_BadZip_ReturnsEmpty()
            {
                CreateFileAtFtp(SomeRegionDir, GetRandomZipName(), "bad zip content");
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_BadZipAndGoodZip_ReturnsOne()
            {
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());
                CreateFileAtFtp(SomeRegionDir, GetRandomZipName(), "bad zip content");
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

                actual.Count().ShouldBe(1);
            }

            [Fact]
            public void Read_Always_IgnoresLogsDir()
            {
                var logsDir = @"fcs_regions\_logs\notifications\currMonth\";
                CreateZipAtFtp(logsDir, GetRandomZipName(), Path.GetRandomFileName());
                var sut = CreateSut();

                var actual = sut.GetNewRaws();

                actual.ShouldBeEmpty();
            }

            [Fact]
            public void Read_FailedToDownloadFile_ReturnsEmpty()
            {
                var sut = CreateTestableSut();
                sut.GetFileCoreBody = (client, uri) => { throw new WebException(); };
                CreateZipAtFtp(SomeRegionDir, GetRandomZipName(), Path.GetRandomFileName());

                var actual = sut.GetNewRaws();

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

                var actual = sut.GetNewRaws();

                actual.Count().ShouldBe(1);
            }

            private static GoldenRetrieverTestable CreateTestableSut()
            {
                return new GoldenRetrieverTestable(FtpUri);
            }
        }

        public class Cache : FileReaderTests
        {
            [Fact]
            public void Cache_FileHadAnEntryThenEntryWasRemoved_ShouldStillReturnTheEntry()
            {
                var dir = SomeRegionDir;
                var fileName = GetRandomZipName();
                var entryName = Path.GetRandomFileName();
                CreateZipAtFtp(dir, fileName, entryName);
                var sut = CreateCachingSut();
                sut.GetNewRaws().ToList();

                CreateZipAtFtp(dir, fileName, zip => zip.GetEntry(entryName).Delete());
                var actual = sut.GetNewRaws();

                actual.ShouldNotBeEmpty();
            }

            [Fact]
            public void Cache_FileHadAnEntryThenEntryWasRemoved_ShouldStillReturnProperContent()
            {
                var dir = SomeRegionDir;
                var fileName = GetRandomZipName();
                var entryName = Path.GetRandomFileName();
                var content = "content";
                CreateZipAtFtp(dir,
                               fileName,
                               zip =>
                               {
                                   var entry = zip.CreateEntry(entryName);
                                   using (var stream = new StreamWriter(entry.Open()))
                                       stream.Write(content);
                               });
                var sut = CreateCachingSut();
                sut.GetNewRaws().ToList();

                CreateZipAtFtp(dir, fileName, zip => zip.GetEntry(entryName).Delete());
                var actual = sut.GetNewRaws();

                actual.Single().Content.ShouldBe(content);
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

        private static GoldenRetriever CreateSut()
        {
            return new GoldenRetriever(FtpUri);
        }

        private static GoldenRetriever CreateCachingSut()
        {
            return new GoldenRetriever(FtpUri, readFromCache: true);
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

        private void CreateFileAtFtp(string dirPath, string fileName, string content)
        {
            var fullPath = PrepareForFile(dirPath, fileName);
            File.WriteAllText(fullPath, content);
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