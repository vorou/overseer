using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Overseer.Common;

namespace Overseer.WebApp
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            LogConfigurator.LogToConsoleAnd(@"c:\logs\ovrs-webapp.log");
            container.Resolve<IRegionNameService>().Fetch();
        }
    }
}