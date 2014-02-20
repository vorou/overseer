using System;
using Nest;

namespace Overseer
{
    public class SourceRepository
    {
        private readonly ElasticClient elastic;

        public SourceRepository()
        {
            elastic = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex("overseer"));
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
    }
}