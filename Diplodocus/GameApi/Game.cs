using System;
using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Logging;

namespace Diplodocus.GameApi
{
    public static unsafe class Game
    {
        public delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
        public static ProcessChatBoxDelegate ProcessChatBox;
        public static IntPtr uiModule = IntPtr.Zero;

        private static IntPtr walkingBoolPtr = IntPtr.Zero;
        public static bool IsWalking
        {
            get => walkingBoolPtr != IntPtr.Zero && *(bool*)walkingBoolPtr;
            set
            {
                if (walkingBoolPtr != IntPtr.Zero)
                {
                    *(bool*)walkingBoolPtr = value;
                    *(bool*)(walkingBoolPtr - 0x10B) = value; // Autorun
                }
            }
        }

        private delegate IntPtr GetModuleDelegate(IntPtr basePtr);

        public static IntPtr newGameUIPtr = IntPtr.Zero;
        public delegate IntPtr GetUnknownNGPPtrDelegate();
        public static GetUnknownNGPPtrDelegate GetUnknownNGPPtr;
        public delegate void NewGamePlusActionDelegate(IntPtr a1, IntPtr a2);
        public static NewGamePlusActionDelegate NewGamePlusAction;

        public static IntPtr emoteAgent = IntPtr.Zero;
        public delegate void DoEmoteDelegate(IntPtr agent, uint emoteID, long a3, bool a4, bool a5);
        public static DoEmoteDelegate DoEmote;

        public static IntPtr contentsFinderMenuAgent = IntPtr.Zero;
        public delegate void OpenAbandonDutyDelegate(IntPtr agent);
        public static OpenAbandonDutyDelegate OpenAbandonDuty;

        public static IntPtr itemContextMenuAgent = IntPtr.Zero;
        public delegate void UseItemDelegate(IntPtr itemContextMenuAgent, uint itemID, uint inventoryPage, uint inventorySlot, short a5);
        public static UseItemDelegate UseItem;

        public static IntPtr actionCommandRequestTypePtr = IntPtr.Zero;
        public static byte ActionCommandRequestType
        {
            get => *(byte*)actionCommandRequestTypePtr;
            set
            {
                if (actionCommandRequestTypePtr != IntPtr.Zero)
                    SafeMemory.WriteBytes(actionCommandRequestTypePtr, new[] { value });
            }
        }

        private static int* keyStates;
        private static byte* keyStateIndexArray;
        public static byte GetKeyStateIndex(int key) => key is >= 0 and < 240 ? keyStateIndexArray[key] : (byte)0;
        private static ref int GetKeyState(int key) => ref keyStates[key];

        public static bool SendKey(int key)
        {
            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0) return false;

            GetKeyState(stateIndex) |= 6;
            return true;
        }
        public static bool SendKeyHold(int key)
        {
            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0) return false;

            GetKeyState(stateIndex) |= 1;
            return true;
        }
        public static bool SendKeyRelease(int key)
        {
            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0) return false;

            GetKeyState(stateIndex) &= ~1;
            return true;
        }

        public static IntPtr ActionManager;
        public static ref bool IsQueued => ref *(bool*)(ActionManager + 0x68);
        public static ref uint QueuedActionType => ref *(uint*)(ActionManager + 0x6C);
        public static ref uint QueuedAction => ref *(uint*)(ActionManager + 0x70);
        public static ref long QueuedTarget => ref *(long*)(ActionManager + 0x78);
        public static ref uint QueuedUseType => ref *(uint*)(ActionManager + 0x80);
        public static ref uint QueuedPVPAction => ref *(uint*)(ActionManager + 0x84);

        public static void Initialize()
        {
            try
            {
                uiModule = DalamudApi.GameGui.GetUIModule();
                ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9"));
            }
            catch { PrintError("Failed to load /qexec"); }

            try { walkingBoolPtr = DalamudApi.SigScanner.GetStaticAddressFromSig("88 83 33 05 00 00"); } // also found at g_PlayerMoveController+523
            catch { PrintError("Failed to load /walk"); }

            try
            {
                var GetAgentModule = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(*((IntPtr*)(*(IntPtr*)uiModule) + 34));
                var agentModule = GetAgentModule(uiModule);
                IntPtr GetAgentByInternalID(int id) => *(IntPtr*)(agentModule + 0x20 + id * 0x8); // Client::UI::Agent::AgentModule_GetAgentByInternalID, not going to sig a function this small

                try
                {
                    OpenAbandonDuty = Marshal.GetDelegateForFunctionPointer<OpenAbandonDutyDelegate>(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 90 48 8B CB"));
                    contentsFinderMenuAgent = GetAgentByInternalID(222);
                }
                catch { PrintError("Failed to load /leaveduty"); }

                try
                {
                    UseItem = Marshal.GetDelegateForFunctionPointer<UseItemDelegate>(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 B0 01 BA 13 00 00 00"));
                    itemContextMenuAgent = GetAgentByInternalID(10);
                }
                catch { PrintError("Failed to load /useitem"); }
            }
            catch { PrintError("Failed to get agent module"); }

            try { actionCommandRequestTypePtr = DalamudApi.SigScanner.ScanText("02 00 00 00 45 8B C5 89"); } // Located 1 function deep in Client__UI__Shell__ShellCommandAction_ExecuteCommand
            catch { PrintError("Failed to load /qac"); }

            try
            {
                keyStates = (int*)DalamudApi.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 44 8B 0D"); // 4C 8D 05 ?? ?? ?? ?? 44 8B 1D
                keyStateIndexArray = (byte*)(DalamudApi.SigScanner.Module.BaseAddress + *(int*)(DalamudApi.SigScanner.ScanModule("0F B6 94 33 ?? ?? ?? ?? 84 D2") + 4));
            }
            catch { PrintError("Failed to load /sendkey!"); }

            try { ActionManager = DalamudApi.SigScanner.GetStaticAddressFromSig("41 0F B7 57 04"); } // g_ActionManager
            catch { PrintError("Failed to load ActionManager!"); }
        }

        public static void Dispose()
        {

        }

        private static void PrintError(string str)
        {
            PluginLog.Error(str);
        }
    }
}
