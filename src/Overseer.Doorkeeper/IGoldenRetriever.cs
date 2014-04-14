using System;
using System.Collections.Generic;
using Overseer.Common;

namespace Overseer.Doorkeeper
{
    public interface IGoldenRetriever
    {
        IEnumerable<XmlDownloaded> GetNewRaws();
        void MarkImported(Uri src);
    }
}