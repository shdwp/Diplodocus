using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Diplodocus.Automatons.InventorySell;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameControl;
using Diplodocus.Universalis;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.Undercut
{
    public sealed class UndercutAutomatonScript: BaseAutomatonScript<UndercutAutomatonScript.UndercutSettings>
    {
        public class UndercutSettings : BaseAutomatonScriptSettings
        {
            public bool  useCrossworld;
            public float minimumFraction;

            public Action<Item, long, long, string> OnPriceUpdated;
        }

        private readonly GameGui             _gameGui;
        private readonly UniversalisClient   _universalis;
        private readonly HIDControl          _hidControl;
        private readonly RetainerSellControl _retainerSellControl;
        private readonly RetainerControl     _retainerControl;
        private readonly AtkControl          _atkControl;

        private ExcelSheet<Item> _itemSheet;

        public UndercutAutomatonScript(HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl, DataManager dataManager, GameGui gameGui, RetainerControl retainerControl, UniversalisClient universalis)
        {
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
            _gameGui = gameGui;
            _retainerControl = retainerControl;
            _universalis = universalis;
            _itemSheet = dataManager.GameData.GetExcelSheet<Item>();
        }

        public override void Dispose()
        {
        }

        protected override bool ShouldDelayStart => true;

        protected override bool ValidateStart()
        {
            if (!_atkControl.IsRetainerMarketWindowFocused())
            {
                _settings.OnScriptFailed?.Invoke("market window not focused");
                return false;
            }

            return true;
        }

        public override async Task StartImpl()
        {
            await _hidControl.CursorDown();

            var amount = _retainerControl.CurrentMarketItemCount;
            for (var i = 0; i < amount; i++)
            {
                if (!_run)
                {
                    return;
                }

                await _hidControl.CursorDown();
                var itemHq = _gameGui.HoveredItem > 1000000;
                var itemId = itemHq ? _gameGui.HoveredItem - 1000000 : _gameGui.HoveredItem;
                if (itemId == 0)
                {
                    PluginLog.Error("Listing skipped - item hover 0!");
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
                    continue;
                }

                await _hidControl.CursorConfirm();
                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorUp();

                await _retainerSellControl.WaitForOfferingsThrottle();
                await _hidControl.CursorConfirm();
                var offeringsData = await _retainerSellControl.WaitForCurrentOfferings();
                if (!_atkControl.IsRetainerOfferingsWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorCancel();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("fail");
                    return;
                }

                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();

                var newPrice = (itemHq ? offeringsData.minimumPriceHQ : offeringsData.minimumPriceNQ) - 1;
                var currentPrice = _retainerSellControl.GetAskingPrice();
                var priceSource = "world min";

                if (_settings.useCrossworld)
                {
                    InventorySellAutomatonScript.CalculatePrice(
                        newPrice,
                        (long)data.minimumPrice,
                        (long)data.averagePrice,
                        _settings.minimumFraction,
                        out newPrice,
                        out priceSource
                    );
                }

                if (newPrice < currentPrice)
                {
                    _retainerSellControl.SetAskingPrice(newPrice);
                    _settings.OnPriceUpdated?.Invoke(item, newPrice, (long)data.averagePrice, priceSource);
                }

                await _hidControl.CursorConfirm();
                if (!_atkControl.IsRetainerMarketWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("fail");
                    return;
                }
            }

            _settings.OnScriptCompleted?.Invoke();
        }
    }
}
