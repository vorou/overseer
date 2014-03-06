using System;
using System.Collections.Generic;
using System.Linq;
using Nest;

namespace Overseer.Common
{
    public class TenderRepository : ITenderRepository
    {
        private readonly ElasticClient elastic;

        public TenderRepository()
        {
            elastic = ElasticClientFactory.Create();
            elastic.MapFromAttributes<Tender>();
        }

        public void Save(Tender tender)
        {
            elastic.Index(tender);
        }

        public DateTime GetMostRecentTenderDate()
        {
            return elastic.Search<Tender>(q => q.MatchAll().SortDescending(d => d.PublishDate)).Documents.First().PublishDate;
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
                                       q.Filter(f => f.Range(r => r.OnField(d => d.PublishDate).GreaterOrEquals(DateTime.Today.AddDays(-7).ToUniversalTime())))
                                        .SortDescending(t => t.TotalPrice)).Documents.Take(limit).ToList();
        }
    }
}