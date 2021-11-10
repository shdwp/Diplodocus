using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Diplodocus.Assistants;
using Diplodocus.Assistants.Storefront;
using Diplodocus.Automatons.CraftingLogResearch;
using Diplodocus.Automatons.InventoryInspect;
using Diplodocus.Automatons.InventorySell;
using Diplodocus.Automatons.MacroCrafting;
using Diplodocus.Automatons.MarketReturn;
using Diplodocus.Automatons.Undercut;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameControl;
using Ninject;

namespace Diplodocus
{
    public sealed class App : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly CommandManager         _commandManager;
        private readonly AutomatonsWindowUI               _automatonsWindowUi;
        private readonly RetainerControl        _retainerControl;

        private readonly List<IAssistant>      _assistants = new();
        private readonly CraftingListAssistant _craftingListAssistant;
        private readonly StorefrontAssistant   _storefrontAssistant;

        public App(CommandManager commandManager, AutomatonsWindowUI automatonsWindowUi, DalamudPluginInterface pluginInterface, RetainerControl retainerControl, StorefrontAssistant storefrontAssistant, CraftingMacroStopAssistant craftingMacroStopAssistant, CraftingListAssistant craftingListAssistant)
        {
            _commandManager = commandManager;
            _automatonsWindowUi = automatonsWindowUi;
            _pluginInterface = pluginInterface;
            _retainerControl = retainerControl;
            _storefrontAssistant = storefrontAssistant;
            _craftingListAssistant = craftingListAssistant;

            _retainerControl.OnAtRetainerChanged += OnAtRetainerChanged;
            _commandManager.AddHandler("/pd", new CommandInfo(OnCommand));

            _automatonsWindowUi.AddComponent(Module.Shared.Get<MacroCraftingAutomaton>());
            _automatonsWindowUi.AddComponent(Module.Shared.Get<UndercutAutomaton>());
            _automatonsWindowUi.AddComponent(Module.Shared.Get<InventoryInspectAutomaton>());
            _automatonsWindowUi.AddComponent(Module.Shared.Get<InventorySellAutomaton>());
            _automatonsWindowUi.AddComponent(Module.Shared.Get<MarketReturnAutomaton>());
            _automatonsWindowUi.AddComponent(Module.Shared.Get<CraftingLogResearchAutomaton>());

            _assistants.Add(Module.Shared.Get<MarketInspectAssistant>());
            _assistants.Add(Module.Shared.Get<CraftingLogInspectAssistant>());

            PluginLog.LogDebug($"Shopping list {_craftingListAssistant.GetHashCode()}");
            _assistants.Add(_craftingListAssistant);
            _assistants.Add(_storefrontAssistant);
            _assistants.Add(craftingMacroStopAssistant);

            _pluginInterface.UiBuilder.Draw += _craftingListAssistant.Draw;
            _pluginInterface.UiBuilder.Draw += _storefrontAssistant.Draw;
        }

        public void Dispose()
        {
            _commandManager.RemoveHandler("/pd");

            _pluginInterface.UiBuilder.Draw -= _storefrontAssistant.Draw;
            _pluginInterface.UiBuilder.Draw += _craftingListAssistant.Draw;
            _retainerControl.OnAtRetainerChanged -= OnAtRetainerChanged;
        }

        private void OnAtRetainerChanged(bool atRetainer)
        {
            _automatonsWindowUi.Visible = atRetainer;
        }

        private void OnCommand(string command, string arguments)
        {
            if (!arguments.Trim().Any())
            {
                _automatonsWindowUi.Visible = true;
            }
            else if (arguments.Trim().Equals("l"))
            {
                _craftingListAssistant.open = true;
            }
            else if (arguments.Trim().Equals("s"))
            {
                _storefrontAssistant.Open = true;
            }
        }
    }
}
