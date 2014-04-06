using System;
using System.Configuration;
using log4net;
using Nest;

namespace Overseer.Common
{
    public static class ElasticClientFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ElasticClientFactory));
        private static readonly object Locker = new object();

        public static ElasticClient Create()
        {
            var esHost = ConfigurationManager.AppSettings["esHost"];
            Log.DebugFormat("esHost = {0}", esHost);
            var esIndex = ConfigurationManager.AppSettings["esIndex"];
            Log.DebugFormat("esIndex = {0}", esIndex);
            var client = new ElasticClient(new ConnectionSettings(new Uri(esHost)).SetDefaultIndex(esIndex));
            //TODO: double-check
            lock (Locker)
            {
                if (!client.IndexExists(esIndex).Exists)
                    client.CreateIndex(esIndex, new IndexSettings());
            }
            return client;
        }
    }
}