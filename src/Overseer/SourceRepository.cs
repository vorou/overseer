using System;
using System.Collections.Generic;
using Nest;

namespace Overseer
{
    public class SourceRepository
    {
        private readonly ElasticClient elastic;

        public SourceRepository(string index)
        {
            elastic = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex(index));
        }

        public void Save(Source source)
        {
            elastic.Index(source);
        }

        public Source GetById(string id)
        {
            return elastic.Get<Source>(id);
        }

        public void Clear()
        {
            elastic.DeleteIndex<Source>();
        }

        public IEnumerable<Source> GetMostExpensive(int limit = 5)
        {
            return elastic.Search<Source>(q => q.MatchAll()).Documents;
        }
    }
}