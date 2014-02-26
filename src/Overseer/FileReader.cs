using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using log4net;

namespace Overseer
{
    public class FileReader : IFileReader
    {
        private readonly ILog log = LogManager.GetLogger(typeof (FileReader));

        private readonly Uri ftp;

        public FileReader(Uri ftp)
        {
            this.ftp = ftp;
            log.InfoFormat("using {0}", ftp);
        }

        public IEnumerable<SourceFile> ReadFiles()
        {
            var regionNames = ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()).ToList();
            foreach (var regionName in regionNames)
            {
                log.InfoFormat("importing region {0}", regionName);
                foreach (var fileUri in ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName)))
                {
                    log.InfoFormat("importing file {0}", fileUri);
                    foreach (var zipEntry in new ZipArchive(new MemoryStream(GetFile(fileUri))).Entries)
                        yield return new SourceFile {Path = fileUri + "/" + zipEntry, Content = new StreamReader(zipEntry.Open()).ReadToEnd()};
                }
            }
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
                log.WarnFormat("failed to list directory {0}", path);
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

        private byte[] GetFile(Uri uri)
        {
            var request = new WebClient {Credentials = new NetworkCredential("free", "free")};
            byte[] newFileData;
            try
            {
                newFileData = request.DownloadData(uri);
            }
            catch (WebException)
            {
                log.WarnFormat("failed to download file {0}", uri);
                throw;
            }
            return newFileData;
        }
    }
}