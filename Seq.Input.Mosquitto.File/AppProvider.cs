using Seq.Apps;

namespace Seq.Input.Mosquitto.File
{
    internal class AppProvider : IAppProvider
    {
        public App App { get; }

        public AppProvider(App app)
        {
            App = app;
        }
    }
}