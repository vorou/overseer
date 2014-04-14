using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyNetQ;
using log4net;
using Overseer.Common;

namespace Overseer.Watchdog
{
    //TODO: finish copypasting all the stuff to make it work
    public class Program
    {
        private static readonly FtpClient FtpClient = new FtpClient(new Uri("ftp://ftp.zakupki.gov.ru"));
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));
        private static readonly IBus Bus = RabbitHutch.CreateBus("host=localhost");

        private static void Main(string[] args)
        {
            var regionNames = FtpClient.ListDirectory("fcs_regions/").Select(uri => uri.Segments.Last()).Except(new[] {"_logs"}).ToList();
#if TEST
            regionNames = regionNames.GetRange(0, 1);
#endif
            foreach (var regionName in regionNames)
            {
                Log.InfoFormat(regionName);
                foreach (var zipUri in GetZipUris(regionName))
                    Bus.Publish(new FileWasSeen {Uri = zipUri});
            }
        }

        private static IEnumerable<Uri> GetZipUris(string regionName)
        {
            return
                FtpClient.ListDirectory(string.Format("fcs_regions/{0}/notifications/currMonth/", regionName))
                         .Union(FtpClient.ListDirectory(string.Format("fcs_regions/{0}/notifications/prevMonth/", regionName)))
                         .Where(p => Path.GetExtension(p.ToString()) == ".zip");
        }
    }
}