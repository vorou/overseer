using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Overseer
{
    public class SourceIndexer
    {
        public IEnumerable<Source> Index(string path)
        {
            foreach (var s in Directory.EnumerateFiles(path, "*.xml.zip", SearchOption.AllDirectories))
            {
                
                var xmlStream = ZipFile.OpenRead(s).Entries.First().Open();
                var xDoc = XDocument.Load(xmlStream);
                var rootLocalName = xDoc.Root.Name.LocalName;
                var tenderType = (TenderType) Enum.Parse(typeof (TenderType), rootLocalName);
//                var tenderId = xDoc.Descendants((XNamespace) "http://zakupki.gov.ru/oos/printform/1" + "purchaseNumber").Single().Value;
                var tenderId = xDoc.Descendants().First(el => el.Name.LocalName == "purchaseNumber").Value;
                yield return new Source {Id = s.Remove(0, path.Length + 1), Type = tenderType, TenderId = tenderId};
            }
        }
    }
}