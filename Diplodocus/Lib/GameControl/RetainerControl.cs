using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        public event Action<bool> OnAtRetainerChanged;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetFilePointer(byte index);
        private GetFilePointer _getFilePtr;
        private DateTime       _lastRetainerCheck;
        private bool           _lastRetainerValue;

        private HashSet<string>                                           _retainerNames             = new();
        private Dictionary<string, IEnumerable<InventoryLib.ItemElement>> _retainerMarketInventories = new();

        public RetainerControl(SigScanner sigScanner, Framework framework, InventoryLib inventoryLib, AtkLib atkLib)
        {
            _framework = framework;
            _inventoryLib = inventoryLib;
            _atkLib = atkLib;

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

        public bool IsAtRetainer()
        {
            return (_getFilePtr != null) && Marshal.ReadInt64(_getFilePtr(7), 0xB0) != 0;
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
                if (item.Value.id == itemId)
                {
                    result += item.Value.amount;
                }
            }

            return result;
        }

        private void UpdateCurrentRetainerInventory()
        {
            var name = CurrentRetainerName();
            if (name == null)
            {
                return;
            }

            PluginLog.Debug($"Updating inventory of \"{name}\"");
            _retainerNames.Add(name);
            _retainerMarketInventories[name] = _inventoryLib.EnumerateInventory(InventoryType.RetainerMarket).ToArray();
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (DateTime.Now - _lastRetainerCheck > TimeSpan.FromSeconds(2.5))
            {
                _lastRetainerCheck = DateTime.Now;

                var atRetainer = IsAtRetainer();
                if (atRetainer)
                {
                    UpdateCurrentRetainerInventory();
                }

                if (atRetainer != _lastRetainerValue)
                {
                    OnAtRetainerChanged?.Invoke(atRetainer);
                    _lastRetainerValue = atRetainer;
                }
            }
        }
    }
}
