using System.Collections.Generic;

namespace Overseer
{
    public interface ITenderRepository
    {
        IEnumerable<Tender> GetMostExpensive(int limit = 5);
    }
}