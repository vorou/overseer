using System;
using System.Collections.Generic;

namespace Overseer.Doorkeeper
{
    public interface IGoldenRetriever
    {
        IEnumerable<Raw> GetNewRaws();
        void MarkImported(Uri src);
    }
}