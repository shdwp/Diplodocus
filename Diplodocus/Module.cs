using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using Diplodocus.Assistants;
using Diplodocus.Automatons.InventoryInspect;
using Diplodocus.Automatons.InventorySell;
using Diplodocus.Automatons.MacroCrafting;
using Diplodocus.Automatons.MarketReturn;
using Diplodocus.Automatons.Undercut;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Ninject;
using Ninject.Modules;

namespace Diplodocus
{
    public sealed class Module : NinjectModule
    {
        public static IReadOnlyKernel Shared;

        private object _scope = new();

        [PluginService] public DalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public BuddyList              BuddyList       { get; private set; }
        [PluginService] public ChatGui                ChatGui         { get; private set; }
        [PluginService] public ChatHandlers           ChatHandlers    { get; private set; }
        [PluginService] public ClientState            ClientState     { get; private set; }
        [PluginService] public CommandManager         CommandManager  { get; private set; }
        [PluginService] public Condition              Condition       { get; private set; }
        [PluginService] public DataManager            DataManager     { get; private set; }
        [PluginService] public FateTable              FateTable       { get; private set; }
        [PluginService] public FlyTextGui             FlyTextGui      { get; private set; }
        [PluginService] public Framework              Framework       { get; private set; }
        [PluginService] public GameGui                GameGui         { get; private set; }
        [PluginService] public GameNetwork            GameNetwork     { get; private set; }
        [PluginService] public JobGauges              JobGauges       { get; private set; }
        [PluginService] public KeyState               KeyState        { get; private set; }
        [PluginService] public LibcFunction           LibcFunction    { get; private set; }
        [PluginService] public ObjectTable            ObjectTable     { get; private set; }
        [PluginService] public PartyFinderGui         PartyFinderGui  { get; private set; }
        [PluginService] public PartyList              PartyList       { get; private set; }
        [PluginService] public SigScanner             SigScanner      { get; private set; }
        [PluginService] public TargetManager          TargetManager   { get; private set; }
        [PluginService] public ToastGui               ToastGui        { get; private set; }

        public Module(DalamudPluginInterface pi)
        {
            pi.Inject(this);
        }

        public override void Load()
        {
            BindDalamudService(PluginInterface);
            BindDalamudService(BuddyList);
            BindDalamudService(ChatGui);
            BindDalamudService(ChatHandlers);
            BindDalamudService(ClientState);
            BindDalamudService(CommandManager);
            BindDalamudService(Condition);
            BindDalamudService(DataManager);
            BindDalamudService(FateTable);
            BindDalamudService(FlyTextGui);
            BindDalamudService(Framework);
            BindDalamudService(GameGui);
            BindDalamudService(GameNetwork);
            BindDalamudService(JobGauges);
            BindDalamudService(KeyState);
            BindDalamudService(LibcFunction);
            BindDalamudService(ObjectTable);
            BindDalamudService(PartyFinderGui);
            BindDalamudService(PartyList);
            BindDalamudService(SigScanner);
            BindDalamudService(TargetManager);
            BindDalamudService(ToastGui);

            var config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Bind<Configuration>().ToConstant(config).InScope(_ => _scope);

            BindSingleton<App>();
            BindSingleton<PluginUI>();

            BindSingleton<InventoryLib>();
            BindSingleton<AtkLib>();

            BindSingleton<AtkControl>();
            BindSingleton<CraftingUIControl>();
            BindSingleton<HIDControl>();
            BindSingleton<RetainerSellControl>();

            BindAutomaton<MacroCraftingAutomaton, MacroCraftingAutomatonScript>();
            BindAutomaton<UndercutAutomaton, UndercutAutomatonScript>();
            BindAutomaton<InventoryInspectAutomaton, InventoryInspectAutomatonScript>();
            BindAutomaton<InventorySellAutomaton, InventorySellAutomatonScript>();
            BindAutomaton<MarketReturnAutomaton, MarketReturnAutomatonScript>();

            BindAssistant<MarketCheckAssistant>();
        }

        private void BindDalamudService<T>(T instance)
        {
            Bind<T>().ToConstant(instance).InTransientScope();
        }

        private void BindSingleton<T>()
        {
            Bind<T>().To<T>().InScope(_ => _scope);
        }

        private void BindAutomaton<TComponent, TScript>()
        {
            BindSingleton<TComponent>();
            BindSingleton<TScript>();
        }

        private void BindAssistant<T>()
        {
            BindSingleton<T>();
        }
    }
}
