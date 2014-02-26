using System;
using System.Configuration;

namespace Overseer.Importer
{
    public class Program
    {
        private static void Main()
        {
            var sourceIndexer = new TenderReader(new FileReader(new Uri(ConfigurationManager.AppSettings["ftp"])));
            var sourceRepository = new TenderRepository("overseer");
            sourceRepository.Clear();
            foreach (var source in sourceIndexer.Read())
                sourceRepository.Save(source);
        }
    }
}