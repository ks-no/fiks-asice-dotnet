using System;
using Common.Logging;
using Common.Logging.Simple;

namespace KS.Fiks.ASiC_E.Test
{
    public class LogFixture : IDisposable
    {
        private readonly ILoggerFactoryAdapter adapter;

        public LogFixture()
        {
            this.adapter = new DebugLoggerFactoryAdapter
            {
                Level = LogLevel.All, ShowLevel = true, ShowLogName = true, ShowDateTime = true
            };
            LogManager.Adapter = this.adapter;
        }

        public ILog GetLog<T>()
        {
            return this.adapter.GetLogger(typeof(T));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            LogManager.Reset();
        }
    }
}