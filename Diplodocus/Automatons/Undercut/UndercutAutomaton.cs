using Diplodocus.Lib.Automaton;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.Undercut
{
    public sealed class UndercutAutomaton : BaseAutomaton<UndercutAutomatonScript, UndercutAutomatonScript.UndercutSettings>
    {
        private float _minFraction = 0.7f;
        private bool  _dcPrices   = true;

        public UndercutAutomaton(UndercutAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Minimum fraction:");
            ImGui.SameLine();
            ImGui.InputFloat("##is_minfraction", ref _minFraction);

            ImGui.Checkbox("##isa_averageprice", ref _dcPrices);
            ImGui.SameLine();
            ImGui.Text(" Use DC prices");
        }

        public override string GetName()
        {
            return "Undercut";
        }

        public override UndercutAutomatonScript.UndercutSettings GetSettings()
        {
            return new UndercutAutomatonScript.UndercutSettings
            {
                useCrossworld = _dcPrices,
                minimumFraction = _minFraction,
                OnPriceUpdated = OnPriceUpdated,
            };
        }

        private void OnPriceUpdated(Item arg1, long price, long averagePrice, string priceSource)
        {
            var fraction = (float)price / averagePrice;
            Log($"UPDATE [{fraction:F}] {arg1.Name} to {price} ({priceSource}).");
        }
    }
}
