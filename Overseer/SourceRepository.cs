using System;
using System.Collections.Generic;
using Nest;

namespace Overseer
{
    public class SourceRepository
    {
        private readonly ElasticClient elastic;

        public SourceRepository()
        {
            elastic = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")));
        }

        public void Save(Source source)
        {
            elastic.Index(source);
        }

        public IEnumerable<Source> FindAll()
        {
            return elastic.Search<Source>(s=>s.AllTypes()).Documents;
        }
    }
}