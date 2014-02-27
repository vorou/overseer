﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Shouldly;
using Xunit;
using Xunit.Extensions;

namespace Overseer.Tests
{
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
            CreateZipOnFtp("", "archive.zip", "fileName");
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Count().ShouldBe(0);
        }

        [Theory]
        [InlineData(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", @"fcs_regions/Adygeja_Resp/notifications/currMonth/archive.zip/")]
        [InlineData(@"fcs_regions\Panda_Country\notifications\currMonth\", @"fcs_regions/Panda_Country/notifications/currMonth/archive.zip/")]
        public void Read_ZipInNotificationsCurrentMonth_PathIsUriPlusEntryName(string targetDirectory, string targetDirUri)
        {
            var fileName = "fileName";
            CreateZipOnFtp(targetDirectory, "archive.zip", fileName);
            var sut = CreateSut();

            var actual = sut.ReadFiles();

            actual.Single().Path.ShouldBe(new Uri("ftp://localhost") + targetDirUri + fileName);
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

            var actual = sut.ReadFiles();

            actual.Single().Content.ShouldBe(content);
        }

        [Fact]
        public void Read_ZipAlreadyImported_ReturnsEmpty()
        {
            CreateZipOnFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", Path.GetRandomFileName());
            var sut = CreateSut();
            sut.ReadFiles().ToList();

            var actual = sut.ReadFiles();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void Read_ReaderWasReset_ReadsAgain()
        {
            CreateZipOnFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", Path.GetRandomFileName());
            var sut = CreateSut();
            sut.ReadFiles().ToList();
            sut.Reset();

            var actual = sut.ReadFiles();

            actual.ShouldNotBeEmpty();
        }

        [Fact]
        public void MarkImported_ZipMarkedAsImported_ReturnsEmpty()
        {
            CreateZipOnFtp(@"fcs_regions\Adygeja_Resp\notifications\currMonth\", "panda.zip", Path.GetRandomFileName());
            var sut = CreateSut();

            sut.MarkImported("ftp://localhost/fcs_regions/Adygeja_Resp/notifications/currMonth/panda.zip");

            var actual = sut.ReadFiles();
            actual.ShouldBeEmpty();
        }

        private FileReader CreateSut()
        {
            return new FileReader(new Uri("ftp://localhost"));
        }

        private void CreateZipOnFtp(string dirPath, string zipName, string zipEntryName)
        {
            var fullDirPath = Path.Combine(FtpMountDir, dirPath);
            Directory.CreateDirectory(fullDirPath);
            using (var zip = ZipFile.Open(Path.Combine(FtpMountDir, fullDirPath, zipName), ZipArchiveMode.Create))
                zip.CreateEntry(zipEntryName);
        }
    }
}