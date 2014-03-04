using System;
using System.Collections.Generic;

namespace Overseer
{
    public interface ITenderRepository
    {
        IEnumerable<Tender> GetMostExpensive(int limit = 5);
        void Save(Tender tender);
        DateTime? GetMostRecentTenderDate();
    }
}