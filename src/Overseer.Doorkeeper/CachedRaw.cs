using Nest;

namespace Overseer.Doorkeeper
{
    public class CachedRaw
    {
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Zip { get; set; }
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Entry { get; set; }
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Content { get; set; }
    }
}