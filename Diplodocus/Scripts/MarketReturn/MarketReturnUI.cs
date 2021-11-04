using System.Text;
using Diplodocus.Scripts.Undercut;
using ImGuiNET;

namespace Diplodocus.Scripts.MarketReturn
{
    public sealed class MarketReturnUI
    {
        private StringBuilder _log = new();
        private bool          _shouldContinue;

        private MarketReturnScript _script;

        public MarketReturnUI(MarketReturnScript script)
        {
            _script = script;
        }

        public void Draw()
        {
            if (ImGui.Button("Start##returnstart"))
            {
                _shouldContinue = true;
                _log.Clear();

                _script.Start(new MarketReturnScript.ReturnSettings
                {
                    ShouldContinue = ShouldContinue,
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
