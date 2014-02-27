using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using log4net;
using Nest;

namespace Overseer
{
    public class FileReader : IFileReader
    {
        private readonly ILog log = LogManager.GetLogger(typeof (FileReader));

        private readonly Uri ftp;
        private readonly ElasticClient elastic;

        public FileReader(Uri ftp)
        {
            this.ftp = ftp;
            elastic = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex("overseer"));
            log.InfoFormat("using {0}", ftp);
        }

        public IEnumerable<SourceFile> ReadFiles()
        {
            var regionNames = ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()).ToList();
            foreach (var regionName in regionNames)
            {
                log.InfoFormat("importing region {0}", regionName);
                foreach (var zipUri in ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName)))
                {
                    var importEntryId = zipUri.ToString();
                    if (elastic.Get<ImportEntry>(importEntryId) != null)
                    {
                        log.InfoFormat("already imported, skipping {0}", zipUri);
                        continue;
                    }

                    log.InfoFormat("importing file {0}", zipUri);
                    foreach (var zipEntry in new ZipArchive(new MemoryStream(GetFile(zipUri))).Entries)
                        yield return new SourceFile {Path = zipUri + "/" + zipEntry, Content = new StreamReader(zipEntry.Open()).ReadToEnd()};
                    elastic.Index(new ImportEntry {Id = importEntryId});
                }
            }
        }

        public void Reset()
        {
            elastic.DeleteIndex<ImportEntry>();
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

        public void MarkImported(string src)
        {
            elastic.Index(new ImportEntry {Id = src});
            elastic.Refresh<ImportEntry>();
        }
    }
}