using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Diplodocus.Assistants.Storefront;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameControl;
using Diplodocus.Lib.Pricing;
using Diplodocus.Universalis;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.Undercut
{
    public sealed class UndercutAutomatonScript : BaseAutomatonScript<UndercutAutomatonScript.UndercutSettings>
    {
        public class UndercutSettings : BaseAutomatonScriptSettings
        {
            public bool  dryRun;
            public int   retainerFirst;
            public int   retainerLast;

            public Action<Item, long, string> OnPriceUpdated;
        }

        private readonly GameGui             _gameGui;
        private readonly StorefrontAssistant _storefrontAssistant;
        private readonly HIDControl          _hidControl;
        private readonly RetainerSellControl _retainerSellControl;
        private readonly RetainerControl     _retainerControl;
        private readonly AtkControl          _atkControl;

        private ExcelSheet<Item> _itemSheet;

        public UndercutAutomatonScript(HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl, DataManager dataManager, GameGui gameGui, RetainerControl retainerControl, UniversalisClient universalis, PricingLib pricingLib, StorefrontAssistant storefrontAssistant)
        {
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
            _gameGui = gameGui;
            _retainerControl = retainerControl;
            _storefrontAssistant = storefrontAssistant;
            _itemSheet = dataManager.GameData.GetExcelSheet<Item>();
        }

        public override void Dispose()
        {
        }

        protected override bool ShouldDelayStart => true;

        protected override bool ValidateStart()
        {
            if (_settings.retainerLast == 1)
            {
                if (!_atkControl.IsRetainerMarketWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("retainer market window not focused");
                    return false;
                }
            }
            else
            {
                if (!_atkControl.IsRetainersWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("retainer window not focused");
                    return false;
                }
            }

            return true;
        }

        public override async Task StartImpl()
        {
            _retainerSellControl.ResetPricingState();
            await _hidControl.CursorLeft();
            await _storefrontAssistant.Data.FetchData();

            for (var retainerIdx = _settings.retainerFirst; retainerIdx < _settings.retainerLast; retainerIdx++)
            {
                if (!_run)
                {
                    _settings.OnScriptFailed("Stopped");
                    return;
                }

                if (_settings.dryRun)
                {
                    await _retainerControl.OpenRetainerMenu(retainerIdx);
                    await _retainerControl.CloseRetainerMenu();
                    continue;
                }

                if (_settings.retainerLast > 1)
                {
                    await _retainerControl.OpenRetainerMenu(retainerIdx);
                    await _retainerControl.RetainerMenuOpenSellMenu();
                }

                for (var i = 0; i < _retainerControl.CurrentMarketItemCount; i++)
                {
                    if (!_run)
                    {
                        _settings.OnScriptFailed("Stopped");
                        return;
                    }

                    if (!_atkControl.IsRetainerMarketWindowFocused())
                    {
                        _settings.OnScriptFailed?.Invoke("market window not focused");
                        return;
                    }

                    await _hidControl.CursorLeft();
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

                    await _hidControl.CursorConfirm();
                    await _hidControl.CursorConfirm();
                    
                    var (newPrice, newPriceSource) = await _retainerSellControl.CalculateAndSetSellingPrice(item);
                    _settings.OnPriceUpdated(item, newPrice, newPriceSource);

                    if (!_atkControl.IsRetainerMarketWindowFocused())
                    {
                        _settings.OnScriptFailed?.Invoke("fail");
                        return;
                    }

                    await _hidControl.CursorDown();
                }

                if (!_run)
                {
                    _settings.OnScriptFailed("Stopped");
                    return;
                }

                if (_settings.retainerLast > 1)
                {
                    await _retainerControl.RetainerMenuCloseSellMenu();
                    await _retainerControl.CloseRetainerMenu();
                }
            }

            _settings.OnScriptCompleted?.Invoke();
        }
    }
}