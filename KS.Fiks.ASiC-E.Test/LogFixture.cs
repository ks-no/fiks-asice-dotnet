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

        public LogFixture()
        {
            var nlogConfig = new LoggingConfiguration();
            nlogConfig.AddTarget("console", new ConsoleTarget
            {
                Name = "Console",
                Encoding = Encoding.UTF8,
                Layout = Layout.FromString("${date}|${level:uppercase=true}|${message} ${exception:format=tostring}|${logger}|${all-event-properties}")
            });
            nlogConfig.AddRuleForAllLevels("console");
            LogManager.Configuration = nlogConfig;
            LogManager.ReconfigExistingLoggers();
        }

        public Logger GetLog<T>()
        {
            return LogManager.GetLogger(typeof(T).Name);
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