using System;
using System.Configuration;
using log4net;
using Topshelf;

namespace Overseer.Doorkeeper
{
    internal class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Program));

        public static void Main()
        {
            LogConfigurator.LogToConsoleAnd(@"c:\logs\ovrs-doorkeeper.log");
            log.InfoFormat("starting the service");
            HostFactory.Run(x =>
                            {
                                x.Service<Doorkeeper>(s =>
                                                      {
                                                          s.ConstructUsing(name => new Doorkeeper(new Uri(ConfigurationManager.AppSettings["ftp"])));
                                                          s.WhenStarted(tc => tc.Start());
                                                          s.WhenStopped(tc => tc.Stop());
                                                      });
                                x.RunAsLocalSystem();

                                x.SetDescription("Overseer's import service");
                                x.SetDisplayName("Overseer Doorkeeper");
                                x.SetServiceName("doorkeeper");
                            });
        }
    }
}