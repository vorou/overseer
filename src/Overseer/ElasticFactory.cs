using System;
using Nest;

namespace Overseer
{
    public static class ElasticFactory
    {
        public static ElasticClient CreateClient(string cs, string index)
        {
            return new ElasticClient(new ConnectionSettings(new Uri(cs)).SetDefaultIndex(index));
        }
    }
}