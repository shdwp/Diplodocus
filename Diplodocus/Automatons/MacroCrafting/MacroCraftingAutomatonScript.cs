using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameControl;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Diplodocus.Automatons.MacroCrafting
{
    public sealed class MacroCraftingAutomatonScript : BaseAutomatonScript<MacroCraftingAutomatonScript.Settings>
    {
        public class Settings : BaseAutomatonScriptSettings
        {
            public int    amount;
            public Action OnItemCrafted;
        }

        private readonly HIDControl _hidControl;
        private readonly AtkControl _atkControl;
        private readonly ToastGui   _toastGui;

        private ExcelSheet<Recipe>         _recipeSheet;
        private TaskCompletionSource<bool> _synthesisTaskSource;

        public MacroCraftingAutomatonScript(DataManager dataManager, HIDControl hidControl, AtkControl atkControl, ToastGui toastGui)
        {
            _hidControl = hidControl;
            _atkControl = atkControl;
            _toastGui = toastGui;
            _recipeSheet = dataManager.GameData.GetExcelSheet<Recipe>();

            _toastGui.QuestToast += OnQuestToast;
        }

        public override void Dispose()
        {
            _toastGui.QuestToast -= OnQuestToast;
        }

        protected override bool ShouldDelayStart => true;

        protected override bool ValidateStart()
        {
            return _atkControl.IsWindowFocused(AtkControl.CraftingLog);
        }

        public override async Task StartImpl()
        {
            await _hidControl.CursorConfirm();

            for (var i = 0; i < _settings.amount; i++)
            {
                if (CheckIfStopped())
                {
                    return;
                }

                await _hidControl.CursorConfirm();
                await _hidControl.CursorConfirm();
                if (!await _atkControl.WaitForWindow(AtkControl.Synthesis))
                {
                    _settings.OnScriptFailed?.Invoke("crafting didn't start");
                }

                await _hidControl.Keypress((int)VirtualKey.KEY_5);

                var result = await WaitForSynthesisResult();
                PluginLog.Debug("Synthesis result: " + result);

                if (!result)
                {
                    _settings.OnScriptFailed?.Invoke("crafting timed out or failed");
                    return;
                }

                _settings.OnItemCrafted?.Invoke();

                if (!await _atkControl.WaitForWindow(AtkControl.CraftingLog))
                {
                    _settings.OnScriptFailed?.Invoke("crafting didn't finish");
                }
            }

            _settings.OnScriptCompleted?.Invoke();
        }

        private async Task<bool> WaitForSynthesisResult()
        {
            _synthesisTaskSource = new TaskCompletionSource<bool>();
            var task = await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(3) * 30), _synthesisTaskSource.Task);

            if (task is Task<bool> resultTask)
            {
                return resultTask.Result;
            }
            else
            {
                return false;
            }
        }

        private void OnQuestToast(ref SeString message, ref QuestToastOptions options, ref bool ishandled)
        {
            if (_synthesisTaskSource == null || _synthesisTaskSource.Task.IsCompleted)
            {
                return;
            }

            if (message.ToString().StartsWith("Your synthesis fails"))
            {
                _synthesisTaskSource.SetResult(false);
            }
            else if (message.ToString().StartsWith("You synthesize"))
            {
                _synthesisTaskSource.SetResult(true);
            }
        }
    }
}
