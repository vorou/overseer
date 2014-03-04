using System.Collections.Generic;

namespace Overseer.WebApp.Models
{
    public class HomeModel
    {
        public IEnumerable<TenderModel> Tenders { get; set; }
        public string MostRecentTenderDate { get; set; }
    }
}