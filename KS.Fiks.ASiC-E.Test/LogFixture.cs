using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace KS.Fiks.ASiC_E.Test
{
    public class LogFixture : IDisposable
    {
        private const string LogLayout =
            "${date}|${level:uppercase=true}|${message} ${exception:format=tostring}|${logger}|${all-event-properties}";

        public LogFixture()
        {
            var nlogConfig = new LoggingConfiguration();
            var target = new ConsoleTarget
            {
                Name = "Console",
                Encoding = Encoding.UTF8,
                Layout = Layout.FromString(LogLayout)
            };
            nlogConfig.AddTarget(
                "console",
                target);
            nlogConfig.AddRuleForAllLevels("console");
            LogManager.Configuration = nlogConfig;
            LogManager.ReconfigExistingLoggers();
        }

        public static Logger GetLog<T>()
        {
            return LogManager.GetLogger(typeof(T).FullName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            LogManager.Shutdown();
        }
    }
}