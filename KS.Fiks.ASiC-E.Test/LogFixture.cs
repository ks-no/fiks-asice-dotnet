using System;
using Common.Logging;
using Common.Logging.Simple;

namespace KS.Fiks.ASiC_E.Test
{
    public class LogFixture : IDisposable
    {
        private readonly ILoggerFactoryAdapter Adapter;
        public LogFixture()
        {
            this.Adapter = new DebugLoggerFactoryAdapter
            {
                Level = LogLevel.All, ShowLevel = true, ShowLogName = true, ShowDateTime = true
            };
            LogManager.Adapter = this.Adapter;
        }

        public ILog GetLog<T>()
        {
            return Adapter.GetLogger(typeof(T));
        }
        
        public void Dispose()
        {
            LogManager.Reset();
        }
    }
}