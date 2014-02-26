using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace Overseer
{
    public class FileReader : IFileReader
    {
        private readonly Uri ftp;

        public FileReader(Uri ftp)
        {
            this.ftp = ftp;
        }

        public IEnumerable<SourceFile> ReadFiles()
        {
            foreach (var regionName in ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()))
                foreach (var fileUri in ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName)))
                    foreach (var zipEntry in new ZipArchive(new MemoryStream(GetFile(fileUri))).Entries)
                        yield return new SourceFile {Path = fileUri + "/" + zipEntry, Content = new StreamReader(zipEntry.Open()).ReadToEnd()};
        }

        private IEnumerable<Uri> ListDirectory(string path)
        {
            var baseUri = new Uri(ftp, path);
            var ftpRequest = (FtpWebRequest) WebRequest.Create(baseUri);
            ftpRequest.Credentials = new NetworkCredential("free", "free");
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse response;
            try
            {
                response = (FtpWebResponse) ftpRequest.GetResponse();
            }
            catch (WebException)
            {
                yield break;
            }
            var streamReader = new StreamReader(response.GetResponseStream());

            while (true)
            {
                var line = streamReader.ReadLine();
                if (line == null)
                    break;
                yield return new Uri(baseUri, line);
            }
        }

        private static byte[] GetFile(Uri uri)
        {
            var request = new WebClient {Credentials = new NetworkCredential("free", "free")};
            var newFileData = request.DownloadData(uri);
            return newFileData;
        }
    }
}