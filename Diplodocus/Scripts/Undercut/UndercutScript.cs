using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Diplodocus.GameControl;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Diplodocus.Scripts.Undercut
{
    public sealed class UndercutScript
    {
        public struct UndercutSettings
        {
            public Func<bool>         ShouldContinue;
            public Action<Item, long> OnPriceUpdated;
            public Action             OnScriptCompleted;
            public Action<string>     OnScriptFailed;
        }

        [PluginService] private GameGui     GameGui     { get; set; }
        [PluginService] private DataManager DataManager { get; set; }

        private HIDControl          _hidControl;
        private RetainerSellControl _retainerSellControl;
        private AtkControl          _atkControl;

        private ExcelSheet<Item> _itemSheet;

        public UndercutScript(HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl)
        {
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
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

        public async Task Start(UndercutSettings settings)
        {
            if (!_atkControl.IsRetainerMarketWindowFocused())
            {
                settings.OnScriptFailed?.Invoke("market window not focused");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
            await _hidControl.CursorDown();

            var amount = _retainerSellControl.MarketItemCount;
            for (var i = 0; i < amount; i++)
            {
                if (settings.ShouldContinue() != true)
                {
                    return;
                }

                await _hidControl.CursorDown();
                var itemHq = GameGui.HoveredItem > 1000000;
                var itemId = itemHq ? GameGui.HoveredItem - 1000000 : GameGui.HoveredItem;
                if (itemId == 0)
                {
                    PluginLog.Error("Listing skipped - item hover 0!");
                    continue;
                }

                var item = _itemSheet.GetRow((uint)itemId);
                if (item == null)
                {
                    PluginLog.Debug($"Failed to find item {GameGui.HoveredItem} in item sheet!");
                    continue;
                }

                await _hidControl.CursorConfirm();
                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorUp();

                await _retainerSellControl.WaitForOfferingsThrottle();
                await _hidControl.CursorConfirm();
                var offeringsData = await _retainerSellControl.WaitForCurrentOfferings();
                if (!_atkControl.IsRetainerOfferingsWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorCancel();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();

                var newPrice = (itemHq ? offeringsData.minimumPriceHQ : offeringsData.minimumPriceNQ) - 1;
                var currentPrice = _retainerSellControl.GetAskingPrice();

                if (newPrice != currentPrice)
                {
                    _retainerSellControl.SetAskingPrice(newPrice);
                    settings.OnPriceUpdated?.Invoke(item, newPrice);
                }

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerMarketWindowFocused())
                {
                    settings.OnScriptFailed?.Invoke("fail");
                    return;
                }
            }

            settings.OnScriptCompleted?.Invoke();
        }
    }
}
