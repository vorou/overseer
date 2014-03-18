using System;
using System.Collections.Generic;
using System.Linq;
using Nest;

namespace Overseer.Doorkeeper
{
    public class ImportJournal
    {
        private readonly ElasticClient elastic;
        private readonly Dictionary<Uri, HashSet<string>> zipToEntries = new Dictionary<Uri, HashSet<string>>();

        public ImportJournal(ElasticClient elastic)
        {
            this.elastic = elastic;
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

        public void MarkZipImported(Uri zipUri)
        {
            elastic.Index(new ImportEntry { Id = zipUri.ToString() });
        }

        public void RememberEntry(Uri zipUri, string entryName)
        {
            if (!zipToEntries.ContainsKey(zipUri))
                zipToEntries.Add(zipUri, new HashSet<string>());
            zipToEntries[zipUri].Add(entryName);
        }

        public bool IsImported(Uri zipUri)
        {
            return elastic.Get<ImportEntry>(zipUri.ToString()) != null;
        }
    }
}