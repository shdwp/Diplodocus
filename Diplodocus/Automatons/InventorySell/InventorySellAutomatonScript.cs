using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Diplodocus.Lib.Pricing;
using Diplodocus.Universalis;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.InventorySell
{
    public sealed class InventorySellAutomatonScript : BaseAutomatonScript<InventorySellAutomatonScript.Settings>
    {
        public class Settings : BaseAutomatonScriptSettings
        {
            public int   minimumPrice;
            public int   itemCount;
            public float minimumFraction;
            public float maximumFraction;

            public Action<int, long>                OnResult;
            public Action<Item, string>             OnItemSkipped;
            public Action<Item, long, string> OnItemSelling;
        }

        private readonly GameGui             _gameGui;
        private readonly DataManager         _dataManager;
        private readonly UniversalisClient   _universalis;
        private readonly InventoryLib        _inventoryLib;
        private readonly PricingLib          _pricingLib;
        private readonly HIDControl          _hidControl;
        private readonly AtkControl          _atkControl;
        private readonly RetainerSellControl _retainerSellControl;
        private readonly RetainerControl     _retainerControl;

        private ExcelSheet<Item> _itemSheet;

        public InventorySellAutomatonScript(UniversalisClient universalis, HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl, InventoryLib inventoryLib, GameGui gameGui, DataManager dataManager, RetainerControl retainerControl, PricingLib pricingLib)
        {
            _universalis = universalis;
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
            _inventoryLib = inventoryLib;
            _gameGui = gameGui;
            _dataManager = dataManager;
            _retainerControl = retainerControl;
            _pricingLib = pricingLib;

            _itemSheet = _dataManager.GameData.GetExcelSheet<Item>();
        }

        public override void Dispose()
        {
        }

        protected override bool ValidateStart()
        {
            if (!_atkControl.IsInventoryWindowFocused())
            {
                _settings.OnScriptFailed?.Invoke("inventory not focused");
                return false;
            }

            return true;
        }

        protected override bool ShouldDelayStart => true;

        public override async Task StartImpl()
        {
            _retainerSellControl.ResetPricingState();
            
            long totalSum = 0;
            var i = 0;
            for (; i < _settings.itemCount; i++)
            {
                if (!_run)
                {
                    _settings.OnScriptFailed?.Invoke("stop");
                    return;
                }

                await _hidControl.CursorRight();

                var itemHq = _gameGui.HoveredItem > 1000000;
                var itemId = itemHq ? _gameGui.HoveredItem - 1000000 : _gameGui.HoveredItem;
                if (itemId == 0)
                {
                    PluginLog.Debug("Slot skipped - no item!");
                    continue;
                }

                var item = _itemSheet.GetRow((uint)itemId);
                if (item == null)
                {
                    PluginLog.Debug($"Failed to find item {_gameGui.HoveredItem} in item sheet!");
                    continue;
                }

                var data = await _universalis.GetDCData(_gameGui.HoveredItem);
                if (data == null || data.currentMinimumPrice == 0)
                {
                    _settings.OnItemSkipped?.Invoke(item, "no market data");
                    continue;
                }

                if (data.currentMinimumPrice < item.PriceLow)
                {
                    _settings.OnItemSkipped?.Invoke(item, $"vendor price higher ({data.currentMinimumPrice} vs {item.PriceLow})");
                    continue;
                }

                var amount = _inventoryLib.Count(_gameGui.HoveredItem, InventoryLib.PlayerInventories);
                if (amount == 0)
                {
                    _settings.OnScriptFailed?.Invoke("Failed to find hovered item in inventory!");
                    return;
                }

                var totalPrice = data.currentMinimumPrice * amount;
                PluginLog.Debug($"Price for item {item.Name} - {totalPrice} ({data.currentMinimumPrice}, amount {amount})");

                if (totalPrice < _settings.minimumPrice)
                {
                    _settings.OnItemSkipped?.Invoke(item, $"price too low ({totalPrice})");
                    continue;
                }

                await _hidControl.CursorConfirm();
                var (newPrice, newPriceSource) = await _retainerSellControl.CalculateAndSetSellingPrice(item);
                _settings.OnItemSelling(item, newPrice, newPriceSource);
                
                if (!_atkControl.IsInventoryWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                totalSum += newPrice * amount;

                if (_retainerControl.CurrentMarketItemCount >= 20)
                {
                    break;
                }
            }

            _settings.OnResult?.Invoke(i, totalSum);
        }
    }
}