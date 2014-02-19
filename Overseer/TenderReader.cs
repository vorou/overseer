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

        public IEnumerable<Source> Read()
        {
            foreach (var file in fileReader.ReadFiles())
            {
                var result = new Source {Id = file.Path, Success = false};
                XDocument xDoc = null;
                try
                {
                    xDoc = XDocument.Parse(file.Content);
                }
                catch (XmlException)
                {
                }
                if (xDoc == null)
                {
                    yield return result;
                    continue;
                }
                var tenderIdElement = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseNumber");
                if (tenderIdElement == null)
                {
                    yield return result;
                    continue;
                }

                result.TenderId = tenderIdElement.Value;
                result.Type = xDoc.Root.Name.LocalName;
                result.Success = true;
                yield return result;
            }
        }
    }
}