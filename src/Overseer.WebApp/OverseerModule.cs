using Nancy;

namespace Overseer.WebApp
{
    public class OverseerModule : NancyModule
    {
        public OverseerModule(ITenderRepository tenderRepo)
        {
            Get["/"] = _ => View["index", tenderRepo.GetMostExpensive()];
        }
    }
}