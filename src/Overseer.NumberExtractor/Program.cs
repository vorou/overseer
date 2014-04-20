using System;
using System.Linq;
using System.Xml.Linq;
using EasyNetQ;
using Overseer.Common.Messages;

namespace Overseer.NumberExtractor
{
    public class Program
    {
        private static readonly IBus Bus = RabbitHutch.CreateBus("host=localhost");

        private static void Main(string[] args)
        {
            Bus.Subscribe<XmlDownloaded>("panda", ExtractNumber);
        }

        private static void ExtractNumber(XmlDownloaded xmlDownloaded)
        {
            var xDoc = XDocument.Parse(xmlDownloaded.Content);
            var tenderIdElement = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseNumber");
            var number = tenderIdElement.Value;
            Console.Out.WriteLine("tender number was seen: {0}", number);
            Bus.Publish(new TenderNumberWasSeen {Number = number});
        }
    }
}