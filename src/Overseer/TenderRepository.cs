using System;
using System.Collections.Generic;
using System.Linq;
using Nest;

namespace Overseer
{
    public class TenderRepository : ITenderRepository
    {
        private readonly ElasticClient elastic;

        public TenderRepository(string index)
        {
            elastic = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex(index));
        }

        public void Save(Tender tender)
        {
            elastic.Index(tender);
        }

        public Tender GetById(string id)
        {
            return elastic.Get<Tender>(id);
        }

        public void Clear()
        {
            elastic.DeleteIndex<Tender>();
        }

        public IEnumerable<Tender> GetMostExpensive(int limit = 5)
        {
            return
                elastic.Search<Tender>(
                                       q =>
                                       q.Filter(f => f.Range(r => r.OnField(d => d.PublishDate).GreaterOrEquals(DateTime.Today.ToUniversalTime())))
                                        .SortDescending(t => t.TotalPrice)).Documents.Take(limit);
        }
    }
}