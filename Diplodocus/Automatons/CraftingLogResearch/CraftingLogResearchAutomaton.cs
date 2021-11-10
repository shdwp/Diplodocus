using System;
using System.Numerics;
using Diplodocus.Lib.Automaton;
using ImGuiNET;

namespace Diplodocus.Automatons.CraftingLogResearch
{
    public sealed class CraftingLogResearchAutomaton : BaseAutomaton<CraftingLogResearchAutomatonScript, CraftingLogResearchAutomatonScript.ResearchSettings>
    {
        private int    _sections = 12;
        private string _output   = "";

        public CraftingLogResearchAutomaton(CraftingLogResearchAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Sections: ");
            ImGui.SameLine();
            ImGui.InputInt("##clr_sections_n", ref _sections);

            ImGui.Text("Data: ");
            ImGui.InputTextMultiline("##clr_output", ref _output, UInt32.MaxValue, new Vector2(0, 100));
        }

        public override string GetName()
        {
            return "CraftingResearch";
        }

        public override CraftingLogResearchAutomatonScript.ResearchSettings GetSettings()
        {
            _output = "";
            return new CraftingLogResearchAutomatonScript.ResearchSettings
            {
                sections = _sections,
                OnResult = (s) => _output = s + _output,
            };
        }
    }
}
