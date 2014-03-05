using System;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using log4net;

namespace Overseer
{
    public class TenderImporter
    {
        private readonly ILog log = LogManager.GetLogger(typeof (TenderImporter));
        private readonly IFileReader reader;
        private readonly ITenderRepository repo;
        private int countImported = 0;

        public TenderImporter(IFileReader reader, ITenderRepository repo)
        {
            this.reader = reader;
            this.repo = repo;
        }

        public void Import()
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var file in reader.ReadNewFiles())
            {
                log.InfoFormat("importing file {0}", file.Path);
                var result = new Tender();
                XDocument xDoc = null;
                try
                {
                    xDoc = XDocument.Parse(file.Content);
                }
                catch (XmlException)
                {
                }
                if (xDoc == null)
                    continue;
                var tenderIdElement = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseNumber");
                if (tenderIdElement == null)
                    continue;

                var name = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseObjectInfo");
                if (name != null)
                    result.Name = name.Value;

                var priceElements = xDoc.Descendants().Where(el => el.Name.LocalName == "maxPrice");
                if (priceElements.Any())
                    result.TotalPrice = priceElements.Sum(el => decimal.Parse(el.Value));

                var firstOrDefault = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "docPublishDate");
                if (firstOrDefault != null)
                {
                    DateTimeOffset publishDate;
                    if (DateTimeOffset.TryParse(firstOrDefault.Value, out publishDate))
                        result.PublishDate = publishDate.UtcDateTime;
                }

                result.Id = tenderIdElement.Value;
                result.Type = xDoc.Root.Name.LocalName;
                repo.Save(result);
                reader.MarkImported(file.Path);
                countImported++;

                log.InfoFormat("imported {0}", file.Path);
            }
            log.InfoFormat("{0} tenders imported", countImported);
            stopwatch.Stop();
            log.InfoFormat("{0:hh\\:mm\\:ss} elapsed", stopwatch.Elapsed);
        }
    }
}