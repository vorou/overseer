using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using log4net;
using Nest;
using Overseer.Common;

namespace Overseer.Doorkeeper
{
    public class GoldenRetriever : IGoldenRetriever
    {
        private readonly ILog log = LogManager.GetLogger(typeof (GoldenRetriever));

        public FtpClient FtpClient { private get; set; }
        private readonly bool readFromCache;
        private readonly ElasticClient elastic;
        private readonly Dictionary<Uri, HashSet<string>> zipToEntries = new Dictionary<Uri, HashSet<string>>();

        public GoldenRetriever(Uri ftp, bool readFromCache = false)
        {
            FtpClient = new FtpClient(ftp);
            this.readFromCache = readFromCache;
            elastic = ElasticClientFactory.Create();
            elastic.MapFromAttributes<ImportEntry>();
            log.InfoFormat("using {0}", ftp);
        }

        public IEnumerable<Raw> GetNewRaws()
        {
            // .ToList() is to fetch the dirs and release the ftp connection
            var regionNames = FtpClient.ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()).Except(new[] {"_logs"}).ToList();
#if TEST
            regionNames = regionNames.GetRange(0, 1);
#endif
            foreach (var regionName in regionNames)
            {
                log.InfoFormat("importing region {0}", regionName);
                foreach (var zipUri in GetZipUris(regionName))
                {
                    if (IsImported(zipUri))
                    {
                        log.InfoFormat("already imported, skipping {0}", zipUri);
                        continue;
                    }

                    if (readFromCache && IsCached(zipUri))
                        foreach (var cachedRaw in GetCachedRaws(zipUri))
                            yield return cachedRaw;

                    var content = FtpClient.Download(zipUri);
                    if (content == null)
                        continue;
                    if (content.Length == 0)
                    {
                        log.InfoFormat("skipping empty zip {0}", zipUri);
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

                        var rawContent = new StreamReader(zipEntry.Open()).ReadToEnd();
                        var raw = new Raw(zipUri, zipEntry.FullName, rawContent);

                        entries++;
                        RememberEntry(zipUri, zipEntry);

                        SaveToCache(zipUri.ToString(), zipEntry.FullName, rawContent);

                        yield return raw;
                    }
                    if (entries == 0)
                    {
                        log.InfoFormat("no entries {0}", zipUri);
                        MarkZipImported(zipUri);
                    }
                }
            }
        }

        private bool IsImported(Uri zipUri)
        {
            return elastic.Get<ImportEntry>(zipUri.ToString()) != null;
        }

        private static bool IsCached(Uri zipUri)
        {
            var cacheDirPath = GetCacheDirPath(zipUri.ToString());
            return Directory.Exists(cacheDirPath);
        }

        private static IEnumerable<Raw> GetCachedRaws(Uri zipUri)
        {
            var cacheDirPath = GetCacheDirPath(zipUri.ToString());
            foreach (var file in Directory.GetFiles(cacheDirPath))
                yield return new Raw(zipUri, Path.GetFileName(file), File.ReadAllText(file));
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

        private void RememberEntry(Uri zipUri, ZipArchiveEntry zipEntry)
        {
            if (!zipToEntries.ContainsKey(zipUri))
                zipToEntries.Add(zipUri, new HashSet<string>());
            zipToEntries[zipUri].Add(zipEntry.Name);
        }

        private IEnumerable<Uri> GetZipUris(string regionName)
        {
            return FtpClient.ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName))
                            .Union(FtpClient.ListDirectory(string.Format("fcs_regions/{0}/notifications/prevMonth/", regionName)))
                            .Where(p => Path.GetExtension(p.ToString()) == ".zip");
        }

        public void Reset()
        {
            elastic.DeleteMapping<ImportEntry>();
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
        }
    }
}