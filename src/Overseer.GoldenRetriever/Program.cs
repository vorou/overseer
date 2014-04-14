using System;
using System.IO;
using System.IO.Compression;
using EasyNetQ;
using Overseer.Common;

namespace Overseer.GoldenRetriever
{
    public class Program
    {
        private static readonly FtpClient FtpClient = new FtpClient(new Uri("ftp://ftp.zakupki.gov.ru"));
        private static readonly IBus Bus = RabbitHutch.CreateBus("host=localhost");

        private static void Main(string[] args)
        {
            Bus.Subscribe<FileWasSeen>("panda", DownloadAndExtract);
        }

        private static void DownloadAndExtract(FileWasSeen fileWasSeen)
        {
            var zipContent = FtpClient.Download(fileWasSeen.Uri);
            if (zipContent == null || zipContent.Length == 0)
                return;

            var zip = new ZipArchive(new MemoryStream(zipContent));
            foreach (var zipEntry in zip.Entries)
            {
                var xmlContent = new StreamReader(zipEntry.Open()).ReadToEnd();
                Bus.Publish(new XmlDownloaded { Uri = new Uri(fileWasSeen.Uri + "/" + zipEntry.Name), Content = xmlContent });
            }
        }
    }
}