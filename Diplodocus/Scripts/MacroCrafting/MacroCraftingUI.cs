using ImGuiNET;

namespace Diplodocus.Scripts.MacroCrafting
{
    public sealed class MacroCraftingUI
    {
        private MacroCraftingScript _script;

        public MacroCraftingUI(MacroCraftingScript script)
        {
            _script = script;
        }

        public void Draw()
        {
            if (ImGui.Button("Start##startcrafting"))
            {
                _script.Start(new MacroCraftingScript.CraftingSettings
                {
                    amount = 1,
                    OnScriptCompleted = OnScriptCompleted,
                    OnScriptFailed = OnScriptFailed
                });
            }
        }

        private void OnScriptFailed(string obj)
        {
        }

        private void OnScriptCompleted()
        {
        }
    }
}
