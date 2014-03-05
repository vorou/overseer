using System;
using System.Configuration;
using Nest;

namespace Overseer
{
    public static class ElasticClientFactory
    {
        public static ElasticClient Create()
        {
            return
                new ElasticClient(
                    new ConnectionSettings(new Uri(ConfigurationManager.AppSettings["esHost"])).SetDefaultIndex(ConfigurationManager.AppSettings["esIndex"]));
        }
    }
}