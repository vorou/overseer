using System;
using System.Configuration;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace Overseer.Importer
{
    public class Program
    {
        private static void Main()
        {
            ConfigureLogger();
            var tenderReader = new TenderReader(new FileReader(new Uri(ConfigurationManager.AppSettings["ftp"])));
            var sourceRepository = new TenderRepository("overseer");
            Console.Out.WriteLine("Clearing existing stuff...");
            sourceRepository.Clear();
            Console.Out.WriteLine("Importing...");
            foreach (var source in tenderReader.Read())
            {
                Console.Out.WriteLine("Importing the object: {0}", source.Id);
                sourceRepository.Save(source);
            }
        }

        private static void ConfigureLogger()
        {
            var file = new FileAppender
                       {
                           AppendToFile = false,
                           File = @"c:\logs\overseer-import.log",
                           Layout = new PatternLayout("%-5level [%thread]: %message%newline")
                       };
            file.ActivateOptions();
            BasicConfigurator.Configure(file);
        }
    }
}