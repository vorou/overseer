using System;
using System.Collections.Generic;
using System.Linq;
using CsQuery;
using log4net;

namespace Overseer
{
    public class RegionNameService : IRegionNameService
    {
        private readonly ILog log = LogManager.GetLogger(typeof (RegionNameService));
        private readonly Dictionary<string, string> regionIdToName = new Dictionary<string, string>();

        public void Fetch()
        {
            var regionPage =
                CQ.CreateFromUrl(
                                 "http://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%81%D1%83%D0%B1%D1%8A%D0%B5%D0%BA%D1%82%D0%BE%D0%B2_%D0%A0%D0%BE%D1%81%D1%81%D0%B8%D0%B9%D1%81%D0%BA%D0%BE%D0%B9_%D0%A4%D0%B5%D0%B4%D0%B5%D1%80%D0%B0%D1%86%D0%B8%D0%B8");
            var regionTableRows = regionPage["table.sortable>tbody>tr"];
            foreach (var row in regionTableRows.Skip(1))
            {
                var cq = row.Cq();
                var regionName = cq.Find("td").First().Text();
                var regionId = cq.Find("td")[1].InnerText;
                regionIdToName.Add(regionId, regionName);
            }
        }

        public string GetName(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (!regionIdToName.Any())
                throw new InvalidOperationException("no services were fetched");
            if (!regionIdToName.ContainsKey(id))
            {
                log.WarnFormat("cant find region id={0}", id);
                return id;
            }
            return regionIdToName[id];
        }
    }
}