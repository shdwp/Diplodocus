using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.Pricing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.Undercut
{
    public sealed class UndercutAutomaton : BaseAutomaton<UndercutAutomatonScript, UndercutAutomatonScript.UndercutSettings>
    {
        private float _minFraction   = 0.8f;
        private float _maxFraction   = 1.35f;
        private bool  _dryRun        = false;
        private int   _retainerFirst = 1;
        private int   _retainerLast = 5;

        public UndercutAutomaton(UndercutAutomatonScript script) : base(script)
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

            /*
            ImGui.Text("Minimum fraction:");
            ImGui.SameLine();
            ImGui.InputFloat("##is_minfraction", ref _minFraction);
            
            ImGui.Text("Maximum fraction:");
            ImGui.SameLine();
            ImGui.InputFloat("##is_maxfraction", ref _maxFraction);
            */

            ImGui.Checkbox("##isa_dryrun", ref _dryRun);
            ImGui.SameLine();
            ImGui.Text(" Dry run");
        }

        public override string GetName()
        {
            return "Undercut";
        }

        public override UndercutAutomatonScript.UndercutSettings GetSettings()
        {
            return new UndercutAutomatonScript.UndercutSettings
            {
                retainerFirst = _retainerFirst - 1,
                retainerLast = _retainerLast,
                dryRun = _dryRun,
                OnPriceUpdated = OnPriceUpdated,
            };
        }

        private void OnPriceUpdated(Item arg1, long price, string priceSource)
        {
            Log($"{priceSource}    {arg1.Name}");
        }
    }
}