using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using Diplodocus.Lib.GameApi;
using Ninject;

namespace Diplodocus
{
    // ReSharper disable once UnusedType.Global
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Diplodocus";

        private readonly App _app;

        public Plugin(DalamudPluginInterface pluginInterface, GameGui gameGui, SigScanner sigScanner)
        {
            var settings = new NinjectSettings
            {
                LoadExtensions = false,
            };

            var module = new Module(pluginInterface);
            Module.Shared = new StandardKernel(settings, module);

            Game.Initialize(gameGui, sigScanner);

            _app = Module.Shared.Get<App>();
        }

        public void Dispose()
        {
            Module.Shared.Dispose();
            Game.Dispose();
        }
    }
}