using System;
using System.Configuration;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace Overseer.Importer
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Program));

        private static void Main()
        {
            ConfigureLogger();
            var retriever = new TenderRetriever(new FileReader(new Uri(ConfigurationManager.AppSettings["ftp"])));
            var indexName = "overseer";
            log.InfoFormat("using index {0}", indexName);
            var sourceRepository = new TenderRepository(indexName);
            log.Info("removing existing data");
            sourceRepository.Clear();
            log.Info("importing");
            foreach (var source in retriever.GetNew())
            {
                log.InfoFormat("importing tender with id={0}", source.Id);
                sourceRepository.Save(source);
            }
        }

        private static void ConfigureLogger()
        {
            var layout = new PatternLayout("%-5level [%thread]: %message%newline");
            var file = new FileAppender {AppendToFile = false, File = @"c:\logs\overseer-import.log", Layout = layout};
            file.ActivateOptions();
            var console = new ConsoleAppender {Layout = layout};
            console.ActivateOptions();
            BasicConfigurator.Configure(file, console);
        }
    }
}