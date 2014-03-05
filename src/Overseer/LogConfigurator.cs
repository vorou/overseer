using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace Overseer
{
    public class LogConfigurator
    {
        public static void LogToConsoleAnd(string logPath)
        {
            var layout = new PatternLayout("%-5level [%d{HH:mm:ss}] %message%newline");
            var file = new RollingFileAppender {AppendToFile = false, File = logPath, Layout = layout};
            file.ActivateOptions();
            var console = new ConsoleAppender {Layout = layout};
            console.ActivateOptions();
            BasicConfigurator.Configure(file, console);
        }
    }
}