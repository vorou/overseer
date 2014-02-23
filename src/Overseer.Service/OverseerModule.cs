using Nancy;

namespace Overseer.Service
{
    public class OverseerModule : NancyModule
    {
        public OverseerModule(ITenderRepository tenderRepo)
        {
            Get["/"] = _ => View["index", tenderRepo.GetMostExpensive()];
        }
    }
}