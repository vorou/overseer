namespace Overseer.Runner
{
    public class Program
    {
        private static void Main()
        {
            var sourceIndexer = new TenderReader(new FileReader(@"W:\ftp"));
            var sourceRepository = new SourceRepository("overseer");
            sourceRepository.Clear();
            foreach (var source in sourceIndexer.Read())
                sourceRepository.Save(source);
        }
    }
}