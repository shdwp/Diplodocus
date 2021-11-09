using Diplodocus.Lib.Automaton;
using ImGuiNET;

namespace Diplodocus.Automatons.MacroCrafting
{
    public sealed class MacroCraftingAutomaton: BaseAutomaton<MacroCraftingAutomatonScript, MacroCraftingAutomatonScript.Settings>
    {
        private int _amount = 1;

        public MacroCraftingAutomaton(MacroCraftingAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Amount:");
            ImGui.SameLine();
            ImGui.InputInt("##amount", ref _amount);
        }

        public override string GetName()
        {
            return "Macro Crafting";
        }

        public override MacroCraftingAutomatonScript.Settings GetSettings()
        {
            return new MacroCraftingAutomatonScript.Settings
            {
                amount = _amount,
                OnItemCrafted = OnItemCrafted
            };
        }

        private void OnItemCrafted()
        {
            _log.Append("Item crafted succesfully.\n");
            _amount--;
        }
    }
}
