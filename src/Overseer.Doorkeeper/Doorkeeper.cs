using System;
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
            importer = new TenderImporter(new FileReader(new Uri("ftp://ftp.zakupki.gov.ru")), new TenderRepository("overseer"));
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
            RunImport();
        }

        public void Stop()
        {
            timer.Stop();
        }
    }
}