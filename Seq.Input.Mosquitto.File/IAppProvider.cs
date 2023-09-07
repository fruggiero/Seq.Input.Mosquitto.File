using Seq.Apps;

namespace Seq.Input.Mosquitto.File
{
    internal interface IAppProvider
    {
        App App { get; }
    }
}