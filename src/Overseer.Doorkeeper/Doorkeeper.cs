using System;
using System.Threading.Tasks;
using System.Timers;
using log4net;

namespace Overseer.Doorkeeper
{
    public class Doorkeeper
    {
        private readonly ILog log = LogManager.GetLogger(typeof (Doorkeeper));
        private readonly Timer timer;
        private readonly TenderImporter importer;

        public Doorkeeper()
        {
            var index = "overseer";
            importer = new TenderImporter(new FileReader(new Uri("ftp://ftp.zakupki.gov.ru"), index), new TenderRepository(index));
            timer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds) {AutoReset = false};
            timer.Elapsed += (s, a) => RunImport();
        }

        private void RunImport()
        {
            log.InfoFormat("time to import");
            importer.Import();
            timer.Start();
        }

        public void Start()
        {
            Task.Run(() => RunImport());
        }

        public void Stop()
        {
            timer.Stop();
        }
    }
}