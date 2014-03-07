using System.Collections.Generic;

namespace Overseer.WebApp.Models
{
    public class GridModel
    {
        public IEnumerable<TenderModel> Tenders { get; set; }
    }
}