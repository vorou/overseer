using System.Collections.Generic;

namespace Overseer.Doorkeeper
{
    public interface IFileReader
    {
        IEnumerable<SourceFile> ReadNewFiles();
        void MarkImported(string src);
    }
}