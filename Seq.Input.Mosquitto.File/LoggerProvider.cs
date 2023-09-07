using Serilog;

namespace Seq.Input.Mosquitto.File
{
    internal class LoggerProvider : ILoggerProvider
    {
        public ILogger Log { get; }
        public LoggerProvider(ILogger log)
        {
            Log = log;
        }
    }
}