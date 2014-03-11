using System;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;
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
            Get["/"] = _ => View["index"];
            Get["/tenders"] = _ =>
                              {
                                  var query = (string) Request.Query.q;
                                  var tenders = query != null ? tenderRepo.Find(query) : tenderRepo.GetMostExpensive(30);
                                  var model = new GridModel {Tenders = tenders.Select(Map)};
                                  return Negotiate.WithModel(model).WithAllowedMediaRange(MediaRange.FromString("application/json"));
                              };
        }

        private TenderModel Map(Tender t)
        {
            return new TenderModel
                   {
                       Name = t.Name,
                       Price = t.TotalPrice.ToString("N0", new CultureInfo("ru-RU")),
                       RegionName = regionNameService.GetName(t.Region),
                       Url = new Uri(string.Format("https://zakupki.kontur.ru/notification44?id={0}", t.Id)).ToString()
                   };
        }
    }
}