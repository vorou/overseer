using System;
using System.Collections.Generic;

namespace Overseer.Common
{
    public interface ITenderRepository
    {
        IEnumerable<Tender> GetMostExpensive(int limit = 5);
        void Save(Tender tender);
        DateTime GetMostRecentTenderDate();
    }
}