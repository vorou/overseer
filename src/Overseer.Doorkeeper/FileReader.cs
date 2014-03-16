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
        private readonly bool readFromCache;
        private readonly ElasticClient elastic;
        private readonly Dictionary<Uri, List<string>> zipToEntries = new Dictionary<Uri, List<string>>();

        public FileReader(Uri ftp, bool readFromCache = false)
        {
            this.ftp = ftp;
            this.readFromCache = readFromCache;
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
                foreach (var zipUri in GetZipUris(regionName))
                {
                    if (elastic.Get<ImportEntry>(zipUri.ToString()) != null)
                    {
                        log.InfoFormat("already imported, skipping {0}", zipUri);
                        continue;
                    }

                    if (readFromCache && IsCached(zipUri))
                        foreach (var sourceFile in GetSourceFilesFromCache(zipUri))
                            yield return sourceFile;

                    var content = GetFile(zipUri);
                    if (content == null)
                        continue;
                    if (content.Length == 0)
                    {
                        log.InfoFormat("empty, skipping {0}", zipUri);
                        MarkZipImported(zipUri);
                        continue;
                    }

                    ZipArchive zip;
                    try
                    {
                        zip = new ZipArchive(new MemoryStream(content));
                    }
                    catch (InvalidDataException)
                    {
                        log.ErrorFormat("bad zip {0}", zipUri);
                        continue;
                    }

                    var entries = 0;
                    foreach (var zipEntry in zip.Entries)
                    {
                        log.DebugFormat("processing entry {0}", zipEntry.FullName);

                        entries++;
                        AddEntry(zipUri, zipEntry);

                        var entryContent = new StreamReader(zipEntry.Open()).ReadToEnd();
                        var sourceFile = CreateSourceFile(zipUri, zipEntry.ToString(), entryContent);
                        SaveToCache(zipUri.ToString(), zipEntry.ToString(), entryContent);
                        yield return sourceFile;
                    }
                    if (entries == 0)
                    {
                        log.InfoFormat("no entries {0}", zipUri);
                        MarkZipImported(zipUri);
                    }
                }
            }
        }

        private static bool IsCached(Uri zipUri)
        {
            var cacheDirPath = GetCacheDirPath(zipUri.ToString());
            return Directory.Exists(cacheDirPath);
        }

        private static IEnumerable<SourceFile> GetSourceFilesFromCache(Uri zipUri)
        {
            var cacheDirPath = GetCacheDirPath(zipUri.ToString());
            foreach (var file in Directory.GetFiles(cacheDirPath))
                yield return CreateSourceFile(zipUri, Path.GetFileName(file), File.ReadAllText(file));
        }

        private static SourceFile CreateSourceFile(Uri zipUri, string entryName, string entryContent)
        {
            var fullUri = zipUri + "/" + entryName;
            var sourceFile = new SourceFile {Uri = new Uri(fullUri), Content = entryContent};
            return sourceFile;
        }

        private static void SaveToCache(string zipUri, string entryName, string entryContent)
        {
            var cacheDirPath = GetCacheDirPath(zipUri);
            if (!Directory.Exists(cacheDirPath))
                Directory.CreateDirectory(cacheDirPath);
            File.WriteAllText(Path.Combine(cacheDirPath, ConvertUriToFileName(entryName)), entryContent);
        }

        private static string GetCacheDirPath(string zipUri)
        {
            return Path.Combine(Path.GetTempPath(), ConvertUriToFileName(zipUri));
        }

        private static string ConvertUriToFileName(string uri)
        {
            return uri.Replace(':', '_').Replace('/', '_');
        }

        private void AddEntry(Uri zipUri, ZipArchiveEntry zipEntry)
        {
            if (!zipToEntries.ContainsKey(zipUri))
                zipToEntries.Add(zipUri, new List<string>());
            zipToEntries[zipUri].Add(zipEntry.Name);
        }

        private IEnumerable<Uri> GetZipUris(string regionName)
        {
            return ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName))
                .Union(ListDirectory(string.Format("fcs_regions/{0}/notifications/prevMonth/", regionName)))
                .Where(p => Path.GetExtension(p.ToString()) == ".zip");
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

            using (var streamReader = new StreamReader(response.GetResponseStream()))
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

        public void MarkImported(Uri src)
        {
            var entryName = src.Segments.Last();
            var zipUri = new Uri(src.ToString().Substring(0, src.ToString().Length - (entryName.Length + 1)));
            zipToEntries[zipUri].Remove(entryName);
            if (!zipToEntries[zipUri].Any())
                MarkZipImported(zipUri);
        }

        private void MarkZipImported(Uri zipUri)
        {
            elastic.Index(new ImportEntry {Id = zipUri.ToString()});
            elastic.Refresh<ImportEntry>();
        }
    }
}