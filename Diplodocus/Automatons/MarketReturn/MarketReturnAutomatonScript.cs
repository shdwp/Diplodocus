using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameControl;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Diplodocus.Automatons.MarketReturn
{
    public sealed class MarketReturnAutomatonScript : BaseAutomatonScript<MarketReturnAutomatonScript.Settings>
    {
        public class Settings : BaseAutomatonScriptSettings
        {
            public Action             OnScriptCompleted;
            public Action<string>     OnScriptFailed;
        }

        private readonly HIDControl          _hidControl;
        private readonly RetainerSellControl _retainerSellControl;
        private readonly RetainerControl     _retainerControl;
        private readonly AtkControl          _atkControl;

        private ExcelSheet<Item> _itemSheet;

        public MarketReturnAutomatonScript(HIDControl hidControl, RetainerSellControl retainerSellControl, DataManager dataManager, AtkControl atkControl, RetainerControl retainerControl)
        {
            _hidControl = hidControl;
            _retainerSellControl = retainerSellControl;
            _atkControl = atkControl;
            _retainerControl = retainerControl;
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
                    _settings.OnScriptFailed?.Invoke("stopped");
                    return;
                }

                await _hidControl.CursorConfirm();
                await _hidControl.CursorUp();
                await _hidControl.CursorConfirm();
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            _settings.OnScriptCompleted?.Invoke();
        }
    }
}
