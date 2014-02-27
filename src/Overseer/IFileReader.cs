using System.Collections.Generic;

namespace Overseer
{
    public interface IFileReader
    {
        IEnumerable<SourceFile> ReadFiles();
        void MarkImported(string src);
    }
}