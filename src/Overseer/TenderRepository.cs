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

        public DateTime GetMostRecentTenderDate()
        {
            return elastic.Search<Tender>(q=>q.MatchAll().SortDescending(d=>d.PublishDate)).Documents.First().PublishDate;
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
            for (var daysAgo = 0; daysAgo <= 5; daysAgo++)
            {
                var resultThisDay =
                    elastic.Search<Tender>(
                                           q =>
                                           q.Filter(
                                                    f =>
                                                    f.Range(
                                                            r =>
                                                            r.OnField(d => d.PublishDate)
                                                             .GreaterOrEquals(DateTime.Today.AddDays(-daysAgo).ToUniversalTime())))
                                            .SortDescending(t => t.TotalPrice)).Documents.Take(limit).ToList();
                if (resultThisDay.Any())
                    return resultThisDay;
            }
            return Enumerable.Empty<Tender>();
        }
    }
}