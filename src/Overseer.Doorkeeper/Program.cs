using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Topshelf;

namespace Overseer.Doorkeeper
{
    internal class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Program));

        public static void Main()
        {
            ConfigureLogger();
            log.InfoFormat("starting the service");
            HostFactory.Run(x =>
                            {
                                x.Service<Doorkeeper>(s =>
                                                      {
                                                          s.ConstructUsing(name => new Doorkeeper());
                                                          s.WhenStarted(tc => tc.Start());
                                                          s.WhenStopped(tc => tc.Stop());
                                                      });
                                x.RunAsLocalSystem();

                                x.SetDescription("Overseer's import service");
                                x.SetDisplayName("Overseer Doorkeeper");
                                x.SetServiceName("osdoorkeeper");
                            });
        }

        private static void ConfigureLogger()
        {
            var layout = new PatternLayout("%-5level [%thread]: %message%newline");
            var file = new FileAppender {AppendToFile = false, File = @"c:\logs\os-doorkeeper.log", Layout = layout};
            file.ActivateOptions();
            var console = new ConsoleAppender {Layout = layout};
            console.ActivateOptions();
            BasicConfigurator.Configure(file, console);
        }
    }
}