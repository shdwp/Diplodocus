using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameControl;

namespace Diplodocus.Assistants
{
    public sealed class CraftingMacroStopAssistant : IAssistant
    {
        private readonly ToastGui   _toastGui;
        private readonly HIDControl _hidControl;

        public CraftingMacroStopAssistant(ToastGui toastGui, HIDControl hidControl)
        {
            _toastGui = toastGui;
            _hidControl = hidControl;

            _toastGui.QuestToast += OnQuestToast;
        }

        public void Dispose()
        {
            _toastGui.QuestToast -= OnQuestToast;
        }

        private void OnQuestToast(ref SeString message, ref QuestToastOptions options, ref bool ishandled)
        {
            if (message.ToString().StartsWith("Your synthesis fails"))
            {
                _hidControl.Keypress((int)VirtualKey.F);
            }
            else if (message.ToString().StartsWith("You synthesize"))
            {
                _hidControl.Keypress((int)VirtualKey.F);
            }
        }
    }
}
