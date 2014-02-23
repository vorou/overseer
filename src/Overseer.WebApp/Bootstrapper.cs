using Nancy;
using Nancy.TinyIoc;

namespace Overseer.WebApp
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register<ITenderRepository>((c, o) => new TenderRepository("overseer"));
        }
    }
}