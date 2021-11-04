using System;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Diplodocus.Lib.GameControl
{
    public sealed class CraftingUIControl : IDisposable
    {
        private readonly Framework  _framework;
        private readonly ChatGui    _chatGui;
        private readonly AtkControl _atkControl;

        public CraftingUIControl(Framework framework, ChatGui chatGui, AtkControl atkControl)
        {
            _framework = framework;
            _chatGui = chatGui;
            _atkControl = atkControl;

            _chatGui.ChatMessage += ChatGuiOnChatMessage;
        }

        public void Dispose()
        {
            _chatGui.ChatMessage -= ChatGuiOnChatMessage;
        }

        private void ChatGuiOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
        }
    }
}
