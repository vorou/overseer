using System;
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

        public TenderImporter(IFileReader reader, ITenderRepository repo)
        {
            this.reader = reader;
            this.repo = repo;
        }

        public void Import()
        {
            foreach (var file in reader.ReadNewFiles())
            {
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

                log.InfoFormat("imported {0}", file.Path);
            }
        }
    }
}