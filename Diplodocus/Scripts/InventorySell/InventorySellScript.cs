using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Diplodocus.GameApi;
using Diplodocus.GameApi.Inventory;
using Diplodocus.GameControl;
using Diplodocus.Universalis;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Scripts.InventorySell
{
    public sealed class InventorySellScript
    {
        public struct SellSettings
        {
            public int minimumPrice;
            public int itemCount;

            public Func<bool> ShouldContinue;

            public Action<int, long>    OnScriptCompleted;
            public Action<string>       OnScriptFailed;
            public Action<Item, string> OnItemSkipped;
            public Action<Item, long>   OnItemSelling;
        }

        private UniversalisClient _universalis;
        private InventoryLib      _inventoryLib;

        private HIDControl          _hidControl;
        private AtkControl          _atkControl;
        private RetainerSellControl _retainerSellControl;

        private ExcelSheet<Item> _itemSheet;

        [PluginService] private GameGui     GameGui     { get; set; }
        [PluginService] private DataManager DataManager { get; set; }

        public InventorySellScript(UniversalisClient universalis, HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl, InventoryLib inventoryLib)
        {
            _universalis = universalis;
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
            _inventoryLib = inventoryLib;
        }

        public void Enable()
        {
            _itemSheet = DataManager.GameData.GetExcelSheet<Item>();
        }

        public void Disable()
        {

        }

        public void Dispose()
        {
            Disable();
        }

        public async Task Start(SellSettings settings)
        {
            if (!_atkControl.IsInventoryWindowFocused())
            {
                settings.OnScriptFailed?.Invoke("inventory not focused");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));

            long totalSum = 0;
            var i = 0;
            for (; i < settings.itemCount; i++)
            {
                if (settings.ShouldContinue?.Invoke() != true)
                {
                    return;
                }

                await _hidControl.CursorRight();

                var itemHq = GameGui.HoveredItem > 1000000;
                var itemId = itemHq ? GameGui.HoveredItem - 1000000 : GameGui.HoveredItem;
                if (itemId == 0)
                {
                    PluginLog.Debug("Slot skipped - no item!");
                    continue;
                }

                var item = _itemSheet.GetRow((uint)itemId);
                if (item == null)
                {
                    PluginLog.Debug($"Failed to find item {GameGui.HoveredItem} in item sheet!");
                    continue;
                }

                var data = await _universalis.GetMarketBoard(0, (ulong)GameGui.HoveredItem);
                if (data == null || data.CurrentMinimumPrice == 0)
                {
                    settings.OnItemSkipped?.Invoke(item, "no market data");
                    continue;
                }

                if (data.CurrentMinimumPrice < item.PriceLow)
                {
                    settings.OnItemSkipped?.Invoke(item, $"vendor price higher ({data.CurrentMinimumPrice} vs {item.PriceLow})");
                    continue;
                }

                var amount = _inventoryLib.Count(GameGui.HoveredItem, InventoryLib.PlayerInventories);
                if (amount == 0)
                {
                    settings.OnScriptFailed?.Invoke("Failed to find hovered item in inventory!");
                    return;
                }

                var totalPrice = data.CurrentMinimumPrice * amount;
                PluginLog.Debug($"Price for item {item.Name} - {totalPrice} (hq {data.CurrentAveragePriceHQ}, hq {data.CurrentAveragePriceNQ}), amount {amount}");

                if (totalPrice < settings.minimumPrice)
                {
                    settings.OnItemSkipped?.Invoke(item, $"price too low ({totalPrice})");
                    continue;
                }

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                await _hidControl.CursorUp();

                await _retainerSellControl.WaitForOfferingsThrottle();
                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerOfferingsWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }
                var offeringsData = await _retainerSellControl.WaitForCurrentOfferings();

                await _hidControl.CursorCancel();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();

                var minPrice = offeringsData.GetMinimumPrice(itemHq);
                if (minPrice == 0)
                {
                    settings.OnItemSkipped?.Invoke(item, "no applicable offerings");
                    await _hidControl.CursorCancel();
                    continue;
                }

                var sellingPrice = minPrice - 1;
                settings.OnItemSelling?.Invoke(item, sellingPrice);
                _retainerSellControl.SetAskingPrice(sellingPrice);

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsInventoryWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("Fail");
                    return;
                }

                totalSum += sellingPrice * amount;

                if (_retainerSellControl.MarketItemCount >= 20)
                {
                    break;
                }
            }

            settings.OnScriptCompleted?.Invoke(i, totalSum);
        }
    }
}
