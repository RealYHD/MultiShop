using System;
using SimpleLogger;
using Xunit.Abstractions;

namespace MultiShop.Shop.AliExpressModule
{
    public class XUnitLogger : ILogReceiver
    {
        public LogLevel Level => LogLevel.Debug;

        public string Identifier => GetType().Name;

        private ITestOutputHelper outputHelper;

        public XUnitLogger(ITestOutputHelper output)
        {
            this.outputHelper = output;
        }

        public void Flush()
        {
        }

        public void LogMessage(string message, DateTime time, LogLevel level)
        {
            try
            {
                outputHelper.WriteLine($"[{time.ToShortTimeString()}][{level.ToString()}]: {message}");
            }
            catch (InvalidOperationException) { };
        }
    }
}