namespace Overseer
{
    public class Tender
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string TenderId { get; set; }
        public bool Success { get; set; }
        public decimal TotalPrice { get; set; }
    }
}