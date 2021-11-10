using Diplodocus.Automatons.InventorySell;
using Diplodocus.Lib.Automaton;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.InventoryInspect
{
    public sealed class InventoryInspectAutomaton : BaseAutomaton<InventoryInspectAutomatonScript, InventorySellAutomatonScript.Settings>
    {
        private int _minPrice = 1000;

        public InventoryInspectAutomaton(InventoryInspectAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Min price:");
            ImGui.SameLine();
            ImGui.InputInt("##minprice", ref _minPrice);
        }

        public override string GetName()
        {
            return "Inventory Inspect";
        }

        public override InventorySellAutomatonScript.Settings GetSettings()
        {
            return new InventorySellAutomatonScript.Settings
            {
                minimumPrice = _minPrice,
                OnResult = OnResult,
                OnItemSelling = OnItemSelling,
            };
        }

        private void OnItemSelling(Item arg1, long price, long averagePrice, string priceSource)
        {
            // _log.Append($"SELLING {arg1.Name} ({InventoryLib.FormatPrice(price)}).\n");
        }

        private void OnResult(int count, long totalSum)
        {
            Log($"Script completed (did {count}), total sum {totalSum}.");
        }
    }
}
