using System;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;

namespace Diplodocus.GameControl
{
    public sealed class CraftingUIControl : IDisposable
    {
        private Framework  _framework;
        private ChatGui    _chatGui;
        private AtkControl _atkControl;

        public CraftingUIControl(Framework framework, ChatGui chatGui, AtkControl atkControl)
        {
            _framework = framework;
            _chatGui = chatGui;
            _atkControl = atkControl;

            _chatGui.ChatMessage += ChatGuiOnChatMessage;
        }

        public void Enable()
        {
            PluginLog.Debug("Enabling crafting UI");
        }

        public void Disable()
        {
        }

        public void Dispose()
        {
            _chatGui.ChatMessage -= ChatGuiOnChatMessage;
        }

        private void ChatGuiOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            PluginLog.Debug("Chat message hit");
        }
    }
}
