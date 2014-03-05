using System.Globalization;
using System.Linq;
using Nancy;
using Overseer.WebApp.Models;

namespace Overseer.WebApp
{
    public class OverseerModule : NancyModule
    {
        public OverseerModule(ITenderRepository tenderRepo)
        {
            Get["/"] = _ =>
                       {
                           var tenders = tenderRepo.GetMostExpensive();
                           var model = new HomeModel {Tenders = tenders.Select(Map)};
                           return View["index", model];
                       };
        }

        private static TenderModel Map(Tender t)
        {
            return new TenderModel {Name = t.Name, Price = t.TotalPrice.ToString("C", new CultureInfo("ru-RU"))};
        }
    }
}