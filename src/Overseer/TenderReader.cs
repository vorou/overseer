using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Overseer
{
    public class TenderReader
    {
        private readonly IFileReader fileReader;

        public TenderReader(IFileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public IEnumerable<Tender> Read()
        {
            foreach (var file in fileReader.ReadFiles())
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
                yield return result;
            }
        }
    }
}