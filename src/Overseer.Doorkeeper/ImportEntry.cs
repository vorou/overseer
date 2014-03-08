using Nest;

namespace Overseer.Doorkeeper
{
    public class ImportEntry
    {
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Id { get; set; }
    }
}