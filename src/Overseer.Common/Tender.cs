using System;
using Nest;

namespace Overseer.Common
{
    public class Tender
    {
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Id { get; set; }
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Type { get; set; }
        public decimal TotalPrice { get; set; }
        public string Name { get; set; }
        public DateTime PublishDate { get; set; }
        [ElasticProperty(Index = FieldIndexOption.not_analyzed)]
        public string Region { get; set; }
        [ElasticProperty(Index = FieldIndexOption.not_analyzed, Type = FieldType.string_type)]
        public Uri Source { get; set; }
    }
}