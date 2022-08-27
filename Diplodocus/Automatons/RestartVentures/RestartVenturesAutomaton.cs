using Diplodocus.Lib.Automaton;
using ImGuiNET;

namespace Diplodocus.Automatons.RestartVentures
{
    public class RestartVenturesAutomaton : BaseAutomaton<RestartVenturesAutomatonScript, RestartVenturesAutomatonScript.RestartVentureSettings>
    {
        private int _retainerFirst = 1;
        private int _retainerLast = 5;

        public RestartVenturesAutomaton(RestartVenturesAutomatonScript script) : base(script)
        {
        }
        
        public override void Draw()
        {
            ImGui.Text("Retainer first:");
            ImGui.SameLine();
            ImGui.InputInt("##is_retainerstart", ref _retainerFirst);
            
            ImGui.Text("Retainer last:");
            ImGui.SameLine();
            ImGui.InputInt("##is_retainercount", ref _retainerLast);
        }
        
        public override string GetName()
        {
            return "RestartVentures";
        }
        
        public override RestartVenturesAutomatonScript.RestartVentureSettings GetSettings()
        {
            return new RestartVenturesAutomatonScript.RestartVentureSettings()
            {
                retainerFirst = _retainerFirst - 1,
                retainerLast = _retainerLast,
            };
        }
    }
}