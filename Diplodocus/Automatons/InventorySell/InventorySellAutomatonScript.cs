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
            public bool  useCrossworld;

            public Action<int, long>                OnResult;
            public Action<Item, string>             OnItemSkipped;
            public Action<Item, long, long, string> OnItemSelling;
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
                if (data == null || data.minimumPrice == 0)
                {
                    _settings.OnItemSkipped?.Invoke(item, "no market data");
                    continue;
                }

                if (data.minimumPrice < item.PriceLow)
                {
                    _settings.OnItemSkipped?.Invoke(item, $"vendor price higher ({data.minimumPrice} vs {item.PriceLow})");
                    continue;
                }

                var amount = _inventoryLib.Count(_gameGui.HoveredItem, InventoryLib.PlayerInventories);
                if (amount == 0)
                {
                    _settings.OnScriptFailed?.Invoke("Failed to find hovered item in inventory!");
                    return;
                }

                var totalPrice = data.minimumPrice * amount;
                PluginLog.Debug($"Price for item {item.Name} - {totalPrice} ({data.minimumPrice}, amount {amount})");

                if (totalPrice < _settings.minimumPrice)
                {
                    _settings.OnItemSkipped?.Invoke(item, $"price too low ({totalPrice})");
                    continue;
                }

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                await _hidControl.CursorUp();

                await _retainerSellControl.WaitForOfferingsThrottle();
                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerOfferingsWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }
                var offeringsData = await _retainerSellControl.WaitForCurrentOfferings();

                await _hidControl.CursorCancel();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();

                var minPrice = offeringsData.GetMinimumPrice(itemHq);
                if (minPrice == 0)
                {
                    _settings.OnItemSkipped?.Invoke(item, "no applicable offerings");
                    await _hidControl.CursorCancel();
                    continue;
                }

                var sellingPrice = minPrice - 1;
                var priceSource = "world undercut";
                if (_settings.useCrossworld)
                {
                    _pricingLib.CalculatePrice(
                        minPrice,
                        (long)data.minimumPrice,
                        (long)data.averagePrice,
                        _settings.minimumFraction,
                        out sellingPrice,
                        out priceSource
                    );
                }

                _settings.OnItemSelling?.Invoke(item, sellingPrice, (long)data.averagePrice, priceSource);
                _retainerSellControl.SetAskingPrice(sellingPrice);

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsInventoryWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                totalSum += sellingPrice * amount;

                if (_retainerControl.CurrentMarketItemCount >= 20)
                {
                    break;
                }
            }

            _settings.OnResult?.Invoke(i, totalSum);
        }
    }
}
