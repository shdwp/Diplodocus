using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Diplodocus.Lib.GameControl
{
    public sealed class RetainerControl : IDisposable
    {
        private readonly Framework    _framework;
        private readonly AtkLib       _atkLib;
        private readonly InventoryLib _inventoryLib;
        private readonly HIDControl   _hidControl;
        private readonly AtkControl   _atkControl;

        public event Action<bool> OnAtRetainerChanged;
        public event Action<bool> OnAtRetainersChanged;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetFilePointer(byte index);
        private GetFilePointer _getFilePtr;
        private DateTime       _lastRetainerCheck;
        private bool           _lastAtRetainerValue;
        private bool           _lastAtRetainersValue;

        private static float _dialogueDelay = 1.6f;

        private HashSet<string>                                           _retainerNames             = new();
        private Dictionary<string, IEnumerable<InventoryLib.ItemElement>> _retainerMarketInventories = new();

        public RetainerControl(SigScanner sigScanner, Framework framework, InventoryLib inventoryLib, AtkLib atkLib, HIDControl hidControl, AtkControl atkControl)
        {
            _framework = framework;
            _inventoryLib = inventoryLib;
            _atkLib = atkLib;
            _hidControl = hidControl;
            _atkControl = atkControl;

            _framework.Update += FrameworkOnUpdate;

            try
            {
                var ptr = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
                _getFilePtr = Marshal.GetDelegateForFunctionPointer<GetFilePointer>(ptr);
            }
            catch (Exception e)
            {
                _getFilePtr = null;
                PluginLog.LogError(e.ToString());
            }
        }

        public void Dispose()
        {
            _framework.Update -= FrameworkOnUpdate;
        }

        public bool IsAtRetainers()
        {
            return _atkLib.IsFocused(AtkControl.Retainers);
        }

        public bool IsAtRetainer()
        {
            return (_getFilePtr != null) && Marshal.ReadInt64(_getFilePtr(7), 0xB0) != 0;
        }

        public async Task OpenRetainerMenu(int retainerIdx)
        {
            foreach (var _ in Enumerable.Range(0, retainerIdx))
            {
                await _hidControl.CursorDown();
            }

            await _hidControl.CursorConfirm();
            await Task.Delay(TimeSpan.FromSeconds(_dialogueDelay));
            await _hidControl.CursorConfirm();
            await _atkControl.WaitForWindow("SelectString");
        }

        public async Task RetainerMenuOpenSellMenu()
        {
            await _hidControl.CursorDown();
            await _hidControl.CursorDown();
            await _hidControl.CursorConfirm();
            await _atkControl.WaitForWindow(AtkControl.RetainerSellList);
        }

        public async Task RetainerMenuCloseSellMenu()
        {
            await _hidControl.CursorCancel();
            await _atkControl.WaitForWindow("SelectString");
        }

        public async Task CloseRetainerMenu()
        {
            await _hidControl.CursorCancel();
            await Task.Delay(TimeSpan.FromSeconds(_dialogueDelay));
            await _hidControl.CursorConfirm();
            await _atkControl.WaitForWindow(AtkControl.Retainers);
        }

        public unsafe string? CurrentRetainerName()
        {
            var unit = _atkLib.FindUnitBase("SelectString");
            if (unit == null)
            {
                return null;
            }

            var text = (AtkTextNode*)unit->UldManager.NodeList[3];
            if (text == null)
            {
                return null;
            }

            return new string(text->NodeText.ToString().Skip("Retainer: ".Length).TakeWhile(c => c != 13).ToArray());
        }

        public int CurrentMarketItemCount => _inventoryLib.EnumerateInventory(InventoryType.RetainerMarket).Count();

        public IEnumerable<KeyValuePair<string, InventoryLib.ItemElement>> EnumerateRetainerMarkets()
        {
            foreach (var kv in _retainerMarketInventories)
            {
                foreach (var item in kv.Value)
                {
                    yield return new KeyValuePair<string, InventoryLib.ItemElement>(kv.Key, item);
                }
            }
        }

        public int CountInRetainerMarkets(ulong itemId)
        {
            var result = 0;
            foreach (var item in EnumerateRetainerMarkets())
            {
                if (item.Value.type.RowId == itemId)
                {
                    result += item.Value.amount;
                }
            }

            return result;
        }

        private void UpdateCurrentRetainerInventory()
        {
            var name = CurrentRetainerName();
            if (name == null || name.Equals(""))
            {
                return;
            }

            _retainerNames.Add(name);
            _retainerMarketInventories[name] = _inventoryLib.EnumerateInventory(InventoryType.RetainerMarket).ToArray();
            PluginLog.Debug($"Updating inventory of \"{name}\", total {_retainerMarketInventories[name].Count()}");
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (DateTime.Now - _lastRetainerCheck > TimeSpan.FromSeconds(0.5))
            {
                _lastRetainerCheck = DateTime.Now;

                var atRetainer = IsAtRetainer();
                if (atRetainer)
                {
                    UpdateCurrentRetainerInventory();
                }

                if (atRetainer != _lastAtRetainerValue)
                {
                    OnAtRetainerChanged?.Invoke(atRetainer);
                    _lastAtRetainerValue = atRetainer;
                }

                var atRetainers = IsAtRetainers();
                if (atRetainers != _lastAtRetainersValue)
                {
                    OnAtRetainersChanged?.Invoke(atRetainers);
                    _lastAtRetainersValue = atRetainer;
                }
            }
        }
    }
}