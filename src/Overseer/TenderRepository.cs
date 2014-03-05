using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Nest;

namespace Overseer
{
    public class TenderRepository : ITenderRepository
    {
        private readonly ILog log = LogManager.GetLogger(typeof (TenderRepository));
        private readonly ElasticClient elastic;

        public TenderRepository()
        {
            elastic = ElasticClientFactory.Create();
        }

        public void Save(Tender tender)
        {
            var response = elastic.Index(tender);
            if (!response.OK)
                log.ErrorFormat("failed to save tender: {0}", tender.Id);
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