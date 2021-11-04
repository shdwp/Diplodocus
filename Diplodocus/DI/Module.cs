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
using Diplodocus.GameApi;
using Diplodocus.GameControl;
using Ninject;
using Ninject.Modules;

namespace Diplodocus.DI
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
        }

        public override void Load()
        {
            Bind<DalamudPluginInterface>().ToConstant(PluginInterface);
            Bind<BuddyList>().ToConstant(BuddyList);
            Bind<ChatGui>().ToConstant(ChatGui);
            Bind<ChatHandlers>().ToConstant(ChatHandlers);
            Bind<ClientState>().ToConstant(ClientState);
            Bind<CommandManager>().ToConstant(CommandManager);
            Bind<Condition>().ToConstant(Condition);
            Bind<DataManager>().ToConstant(DataManager);
            Bind<FateTable>().ToConstant(FateTable);
            Bind<FlyTextGui>().ToConstant(FlyTextGui);
            Bind<Framework>().ToConstant(Framework);
            Bind<GameGui>().ToConstant(GameGui);
            Bind<GameNetwork>().ToConstant(GameNetwork);
            Bind<JobGauges>().ToConstant(JobGauges);
            Bind<KeyState>().ToConstant(KeyState);
            Bind<LibcFunction>().ToConstant(LibcFunction);
            Bind<ObjectTable>().ToConstant(ObjectTable);
            Bind<PartyFinderGui>().ToConstant(PartyFinderGui);
            Bind<PartyList>().ToConstant(PartyList);
            Bind<SigScanner>().ToConstant(SigScanner);
            Bind<TargetManager>().ToConstant(TargetManager);
            Bind<ToastGui>().ToConstant(ToastGui);

            Bind<AtkLib>().To<AtkLib>().InScope(_ => _scope);
            Bind<CraftingUIControl>().To<CraftingUIControl>().InScope(_ => _scope);
        }

        public void Dispose()
        {
            _scope = null;
        }
    }
}
