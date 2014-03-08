using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using log4net;
using Nest;
using Overseer.Common;

namespace Overseer.Doorkeeper
{
    public class FileReader : IFileReader
    {
        private readonly ILog log = LogManager.GetLogger(typeof (FileReader));

        private readonly Uri ftp;
        private readonly ElasticClient elastic;

        public FileReader(Uri ftp)
        {
            this.ftp = ftp;
            elastic = ElasticClientFactory.Create();
            elastic.MapFromAttributes<ImportEntry>();
            log.InfoFormat("using {0}", ftp);
        }

        public IEnumerable<SourceFile> ReadNewFiles()
        {
            // .ToList() is to fetch the dirs and release the ftp connection
            var regionNames = ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()).Except(new[] {"_logs"}).ToList();
#if TEST
            regionNames = regionNames.GetRange(0, 1);
#endif
            foreach (var regionName in regionNames)
            {
                log.InfoFormat("importing region {0}", regionName);
                foreach (var zipUri in
                    ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName))
                        .Union(ListDirectory(string.Format("fcs_regions/{0}/notifications/prevMonth/", regionName))))
                {
                    ZipArchive zip;
                    var content = GetFile(zipUri);
                    if (content == null)
                        continue;

                    try
                    {
                        zip = new ZipArchive(new MemoryStream(content));
                    }
                    catch (InvalidDataException)
                    {
                        log.ErrorFormat("Bad zip: {0}", zipUri);
                        continue;
                    }

                    foreach (var zipEntry in zip.Entries)
                    {
                        var fullUri = zipUri + "/" + zipEntry;
                        if (elastic.Get<ImportEntry>(fullUri) != null)
                        {
                            log.InfoFormat("already imported, skipping {0}", fullUri.Substring(fullUri.Length - 10));
                            continue;
                        }

                        yield return new SourceFile {Uri = fullUri, Content = new StreamReader(zipEntry.Open()).ReadToEnd()};
                    }
                }
            }
        }

        public void Reset()
        {
            elastic.DeleteMapping<ImportEntry>();
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
                newFileData = GetFileCore(request, uri);
            }
            catch (WebException)
            {
                log.ErrorFormat("failed to download file {0}", uri);
                return null;
            }
            return newFileData;
        }

        protected virtual byte[] GetFileCore(WebClient request, Uri uri)
        {
            return request.DownloadData(uri);
        }

        public void MarkImported(string src)
        {
            elastic.Index(new ImportEntry {Id = src});
            elastic.Refresh<ImportEntry>();
        }
    }
}