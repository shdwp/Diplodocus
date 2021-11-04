using System.Text;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Scripts.Undercut
{
    public sealed class UndercutUI
    {
        private StringBuilder _log = new();
        private bool          _shouldContinue;

        private UndercutScript _script;

        public UndercutUI(UndercutScript script)
        {
            _script = script;
        }

        public void Draw()
        {
            if (ImGui.Button("Start##undercutstart"))
            {
                _shouldContinue = true;
                _log.Clear();

                _script.Start(new UndercutScript.UndercutSettings
                {
                    ShouldContinue = ShouldContinue,
                    OnPriceUpdated = OnPriceUpdated,
                    OnScriptCompleted = OnScriptCompleted,
                    OnScriptFailed = OnScriptFailed,
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Stop##undercutstop"))
            {
                _shouldContinue = false;
            }

            ImGui.Text("Log:");
            ImGui.TextWrapped(_log.ToString());
        }

        private bool ShouldContinue()
        {
            return _shouldContinue;
        }

        private void OnPriceUpdated(Item arg1, long arg2)
        {
            _log.Append($"{arg1.Name} price updated to {arg2}.\n");
        }

        private void OnScriptFailed(string obj)
        {
            _log.Append("Script failed - " + obj);
        }

        private void OnScriptCompleted()
        {
            _log.Append("Script finished.\n");
        }
    }
}
