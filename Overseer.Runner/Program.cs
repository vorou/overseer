using System;
using System.Diagnostics;

namespace Overseer.Runner
{
    public class Program
    {
        private static void Main()
        {
            var sourceIndexer = new SourceIndexer();
            var sourceRepository = new SourceRepository();
            var sw = Stopwatch.StartNew();
            foreach (var source in sourceIndexer.Index(@"W:\ftp"))
            {
                sourceRepository.Save(source);
            }
            Console.Out.WriteLine(sw.Elapsed.TotalMinutes);
        }
    }
}