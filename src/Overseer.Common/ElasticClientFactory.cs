using System;
using System.Configuration;
using log4net;
using Nest;

namespace Overseer.Common
{
    public static class ElasticClientFactory
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (ElasticClientFactory));

        public static ElasticClient Create()
        {
            var esHost = ConfigurationManager.AppSettings["esHost"];
            log.DebugFormat("esHost = {0}", esHost);
            var esIndex = ConfigurationManager.AppSettings["esIndex"];
            log.DebugFormat("esIndex = {0}", esIndex);
            var client = new ElasticClient(new ConnectionSettings(new Uri(esHost)).SetDefaultIndex(esIndex));
            if (!client.IndexExists(esIndex).Exists)
                client.CreateIndex(esIndex, new IndexSettings());
            return client;
        }
    }
}