using Nancy;
using Nancy.Responses.Negotiation;

namespace Overseer.Service
{
    public class OverseerModule : NancyModule
    {
        public OverseerModule(ITenderRepository tenderRepo)
        {
            Get["/"] = _ => Negotiate.WithModel(tenderRepo.GetMostExpensive()).WithAllowedMediaRange(MediaRange.FromString("application/json"));
        }
    }
}