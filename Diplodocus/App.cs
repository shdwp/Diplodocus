using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Diplodocus.Assistants;
using Diplodocus.Automatons.InventoryInspect;
using Diplodocus.Automatons.InventorySell;
using Diplodocus.Automatons.MacroCrafting;
using Diplodocus.Automatons.MarketReturn;
using Diplodocus.Automatons.Undercut;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameControl;
using Ninject;

namespace Diplodocus
{
    public sealed class App : IDisposable
    {
        private readonly CommandManager      _commandManager;
        private readonly PluginUI            _pluginUi;
        private readonly RetainerSellControl _retainerSellControl;

        private readonly List<IAssistant> _assistants = new();

        public App(CommandManager commandManager, PluginUI pluginUi, RetainerSellControl retainerSellControl)
        {
            _commandManager = commandManager;
            _pluginUi = pluginUi;
            _retainerSellControl = retainerSellControl;

            _retainerSellControl.OnAtRetainerChanged += OnAtRetainerChanged;

            _commandManager.AddHandler("/pd", new CommandInfo(OnCommand));

            _pluginUi.AddComponent(Module.Shared.Get<MacroCraftingAutomaton>());
            _pluginUi.AddComponent(Module.Shared.Get<UndercutAutomaton>());
            _pluginUi.AddComponent(Module.Shared.Get<InventoryInspectAutomaton>());
            _pluginUi.AddComponent(Module.Shared.Get<InventorySellAutomaton>());
            _pluginUi.AddComponent(Module.Shared.Get<MarketReturnAutomaton>());

            _assistants.Add(Module.Shared.Get<MarketCheckAssistant>());
        }

        public void Dispose()
        {
            _commandManager.RemoveHandler("/pd");

            _retainerSellControl.OnAtRetainerChanged -= OnAtRetainerChanged;
        }

        private void OnAtRetainerChanged(bool atRetainer)
        {
            _pluginUi.Visible = atRetainer;
        }

        private void OnCommand(string command, string arguments)
        {
            if (!arguments.Trim().Any())
            {
                _pluginUi.Visible = true;
            }
        }
    }
}
