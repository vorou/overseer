using System;
using System.Globalization;
using System.Linq;
using Nancy;
using Overseer.Common;
using Overseer.WebApp.Models;

namespace Overseer.WebApp
{
    public class OverseerModule : NancyModule
    {
        private readonly IRegionNameService regionNameService;

        public OverseerModule(ITenderRepository tenderRepo, IRegionNameService regionNameService)
        {
            this.regionNameService = regionNameService;
            Get["/"] = _ =>
                       {
                           var tenders = tenderRepo.GetMostExpensive(10);
                           var model = new HomeModel {Tenders = tenders.Select(Map)};
                           return View["index", model];
                       };
        }

        private TenderModel Map(Tender t)
        {
            return new TenderModel
                   {
                       Name = t.Name,
                       Price = t.TotalPrice.ToString("C", new CultureInfo("ru-RU")),
                       RegionName = regionNameService.GetName(t.Region),
                       Url = new Uri(string.Format("https://zakupki.kontur.ru/notification44?id={0}", t.Id)).ToString()
                   };
        }
    }
}