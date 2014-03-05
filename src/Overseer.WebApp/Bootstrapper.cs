using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Overseer.WebApp
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            var layout = new PatternLayout("%-5level: %message%newline");
            var file = new FileAppender {AppendToFile = false, File = @"c:\logs\ovrs-front.log", Layout = layout};
            file.ActivateOptions();
            var console = new ConsoleAppender {Layout = layout};
            console.ActivateOptions();
            BasicConfigurator.Configure(file, console);
        }
    }
}