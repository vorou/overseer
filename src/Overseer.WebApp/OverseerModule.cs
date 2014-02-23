using System.Globalization;
using System.Linq;
using Nancy;

namespace Overseer.WebApp
{
    public class OverseerModule : NancyModule
    {
        public OverseerModule(ITenderRepository tenderRepo)
        {
            Get["/"] = _ =>
                       {
                           var tenders = tenderRepo.GetMostExpensive();
                           return View["index", tenders.Select(Map)];
                       };
        }

        private static TenderModel Map(Tender t)
        {
            return new TenderModel {Name = t.Name, Price = t.TotalPrice.ToString("C", new CultureInfo("ru-RU"))};
        }
    }
}