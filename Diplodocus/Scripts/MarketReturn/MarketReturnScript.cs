using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Diplodocus.GameControl;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Diplodocus.Scripts.MarketReturn
{
    public sealed class MarketReturnScript
    {
        public struct ReturnSettings
        {
            public Func<bool>         ShouldContinue;
            public Action             OnScriptCompleted;
            public Action<string>     OnScriptFailed;
        }

        [PluginService] private GameGui     GameGui     { get; set; }
        [PluginService] private DataManager DataManager { get; set; }

        private HIDControl          _hidControl;
        private RetainerSellControl _retainerSellControl;
        private AtkControl          _atkControl;

        private ExcelSheet<Item> _itemSheet;

        public MarketReturnScript(HIDControl hidControl, RetainerSellControl retainerSellControl, AtkControl atkControl)
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

        public async Task Start(ReturnSettings settings)
        {
            await Task.Delay(TimeSpan.FromSeconds(2.5));

            await _hidControl.CursorDown();

            var amount = _retainerSellControl.MarketItemCount;
            for (var i = 0; i < amount; i++)
            {
                if (settings.ShouldContinue() != true)
                {
                    return;
                }

                await _hidControl.CursorConfirm();
                await _hidControl.CursorUp();
                await _hidControl.CursorConfirm();
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }
    }
}
