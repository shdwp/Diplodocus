using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Diplodocus.DI;
using Diplodocus.GameApi;
using Diplodocus.GameApi.Inventory;
using Diplodocus.GameControl;
using Diplodocus.Scripts.InventorySell;
using Diplodocus.Scripts.MarketReturn;
using Diplodocus.Scripts.Undercut;
using Diplodocus.Universalis;
using Ninject;

namespace Diplodocus
{
    // ReSharper disable once UnusedType.Global
    public sealed class Plugin : IDalamudPlugin
    {
        public        string Name => "Diplodocus";
        private const string CommandName = "/pd";

        [PluginService] private DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] private CommandManager         CommandManager  { get; set; }

        private readonly Configuration     _config;
        private readonly PluginUI          _ui;
        private readonly DalamudApi        _api;
        private readonly HIDControl        _hidControl;
        private readonly UniversalisClient _universalisClient;

        private readonly AtkLib              _atkLib;
        private readonly RetainerSellControl _retainerSellControl;
        private readonly InventoryLib        _inventoryLib;
        private readonly AtkControl          _atkControl;

        private readonly InventorySellScript    _inventorySellScript;
        private readonly InventorySellUI        _inventorySellUi;
        private readonly UndercutScript         _undercutScript;
        private readonly UndercutUI             _undercutUi;
        private readonly InventoryInspectScript _inventoryInspectScript;
        private readonly MarketReturnScript     _marketReturnScript;
        private readonly MarketReturnUI         _marketReturnUi;

        public Plugin()
        {
            _config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            _config.Initialize(PluginInterface);

            var settings = new NinjectSettings
            {
                LoadExtensions = false,
            };

            Module.Shared = new StandardKernel(settings, new Module());

            _ui = new PluginUI(_config);
            _api = new DalamudApi(this, PluginInterface);

            Game.Initialize();

            var lib = Module.Shared.Get<CraftingUIControl>();
            PluginLog.Debug("UI from nin: " + lib);

            _universalisClient = new UniversalisClient();

            _hidControl = new HIDControl();
            PluginInterface.Inject(_hidControl);

            _atkLib = new AtkLib();
            PluginInterface.Inject(_atkLib);

            _inventoryLib = new InventoryLib();
            PluginInterface.Inject(_inventoryLib);

            _atkControl = new AtkControl(_atkLib);
            PluginInterface.Inject(_atkControl);

            _retainerSellControl = new RetainerSellControl(_atkLib, _inventoryLib);
            PluginInterface.Inject(_retainerSellControl);

            _inventorySellScript = new InventorySellScript(_universalisClient, _hidControl, _retainerSellControl, _atkControl, _inventoryLib);
            PluginInterface.Inject(_inventorySellScript);
            _inventoryInspectScript = new InventoryInspectScript(_universalisClient, _inventoryLib);
            PluginInterface.Inject(_inventoryInspectScript);
            _inventorySellUi = new InventorySellUI(_inventorySellScript, _inventoryInspectScript);
            PluginInterface.Inject(_inventorySellScript);
            _undercutScript = new UndercutScript(_hidControl, _retainerSellControl, _atkControl);
            PluginInterface.Inject(_undercutScript);
            _undercutUi = new UndercutUI(_undercutScript);
            PluginInterface.Inject(_undercutUi);
            _marketReturnScript = new MarketReturnScript(_hidControl, _retainerSellControl, _atkControl);
            PluginInterface.Inject(_marketReturnScript);
            _marketReturnUi = new MarketReturnUI(_marketReturnScript);
            PluginInterface.Inject(_marketReturnUi);

            _atkLib.Enable();
            _inventoryLib.Enable();
            _retainerSellControl.Enable();
            _undercutScript.Enable();
            _inventorySellScript.Enable();
            _inventoryInspectScript.Enable();
            _marketReturnScript.Enable();

            _ui.AddTab("Inventory Sell", _inventorySellUi.Draw);
            _ui.AddTab("Undercut", _undercutUi.Draw);
            _ui.AddTab("Market Return", _marketReturnUi.Draw);


            _retainerSellControl.OnAtRetainerChanged += RetainerChangedSellControlOnOnAtRetainerChanged;

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand));
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += () => _ui.SettingsVisible = true;
        }

        public void Dispose()
        {
            Module.Shared.Dispose();

            CommandManager.RemoveHandler(CommandName);
            _retainerSellControl.OnAtRetainerChanged -= RetainerChangedSellControlOnOnAtRetainerChanged;

            _marketReturnScript.Dispose();
            _inventoryInspectScript.Dispose();
            _undercutScript.Dispose();
            _inventorySellScript.Dispose();

            _inventoryLib.Dispose();
            _retainerSellControl.Dispose();
            _atkLib.Dispose();

            _ui.Dispose();

            Game.Dispose();
        }

        private void RetainerChangedSellControlOnOnAtRetainerChanged(bool value)
        {
            _ui.Visible = value;
        }

        private void OnCommand(string command, string args)
        {
            if (args == "")
            {
                this._ui.Visible = true;
            }
        }

        private void DrawUI()
        {
            _ui.Draw();
        }
    }
}
