using System;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Diplodocus.Assistants;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameControl;

namespace Diplodocus.Automatons.RetainerInventory
{
    public class RetainerInventoryAutomatonScript : BaseAutomatonScript<RetainerInventoryAutomatonScript.RetainerInventorySettings>
    {
        public class RetainerInventorySettings : BaseAutomatonScriptSettings
        {
            public int  itemCount;
            public bool fromRetainer;

            public bool itemsShoppingList;
            public bool itemsAll;
        }

        private readonly RetainerControl       _retainerControl;
        private readonly AtkControl            _atkControl;
        private readonly HIDControl            _hidControl;
        private readonly GameGui               _gameGui;
        private readonly CraftingListAssistant _craftingListAssistant;

        public RetainerInventoryAutomatonScript(HIDControl hidControl, RetainerControl retainerControl, GameGui gameGui, CraftingListAssistant craftingListAssistant, AtkControl atkControl)
        {
            _hidControl = hidControl;
            _retainerControl = retainerControl;
            _gameGui = gameGui;
            _craftingListAssistant = craftingListAssistant;
            _atkControl = atkControl;
        }

        protected override bool ShouldDelayStart => true;

        public override void Dispose()
        {
        }

        protected override bool ValidateStart()
        {
            if (!_settings.fromRetainer && !_atkControl.IsInventoryWindowFocused())
            {
                _settings.OnScriptFailed?.Invoke("inventory window not focused");
                return false;
            }

            return true;
        }

        public override async Task StartImpl()
        {
            await _hidControl.CursorRight();

            for (var i = 0; i < _settings.itemCount; i++)
            {
                if (!_run)
                {
                    _settings.OnScriptFailed?.Invoke("stop");
                    return;
                }

                var itemHq = _gameGui.HoveredItem > 1000000;
                var itemId = itemHq ? _gameGui.HoveredItem - 1000000 : _gameGui.HoveredItem;
                if (itemId != 0)
                {

                    if ((_settings.itemsShoppingList && _craftingListAssistant.CheckIfOnShoppingList(itemId)) || _settings.itemsAll)
                    {
                        await _hidControl.CursorConfirm();
                        await _hidControl.CursorConfirm();

                        if (_settings.fromRetainer)
                        {
                            await _hidControl.CursorConfirm();
                        }
                    }
                }

                await _hidControl.CursorRight();
                await Task.Delay(TimeSpan.FromMilliseconds(40));
            }
        }
    }
}