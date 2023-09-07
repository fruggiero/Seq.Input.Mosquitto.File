using Serilog;

namespace Seq.Input.Mosquitto.File
{
    internal interface ILoggerProvider
    {
        ILogger Log { get; }
    }
}