using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi.Inventory;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.InventorySell
{
    public sealed class InventorySellAutomaton : BaseAutomaton<InventorySellAutomatonScript, InventorySellAutomatonScript.Settings>
    {
        private int   _minPrice   = 1000;
        private int   _count      = 1;
        private float _minFraction = 0.7f;
        private bool  _dcPrices   = true;

        public InventorySellAutomaton(InventorySellAutomatonScript sellAutomatonScript) : base(sellAutomatonScript)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Min price:");
            ImGui.SameLine();
            ImGui.InputInt("##minprice", ref _minPrice);

            ImGui.Text("Item count:");
            ImGui.SameLine();
            ImGui.InputInt("##count", ref _count);

            ImGui.Text("Minimum fraction:");
            ImGui.SameLine();
            ImGui.InputFloat("##is_minfraction", ref _minFraction);

            ImGui.Checkbox("##isa_averageprice", ref _dcPrices);
            ImGui.SameLine();
            ImGui.Text(" Use DC prices");
        }

        public override string GetName()
        {
            return "Inventory Sell";
        }

        public override InventorySellAutomatonScript.Settings GetSettings()
        {
            return new InventorySellAutomatonScript.Settings
            {
                minimumPrice = _minPrice,
                itemCount = _count,
                useCrossworld = _dcPrices,
                minimumFraction = _minFraction,

                OnItemSkipped = OnItemSkipped,
                OnItemSelling = OnItemSelling,
                OnResult = OnResult,
            };
        }

        private void OnItemSkipped(Item arg1, string arg2)
        {
            Log($"SKIP {arg1.Name} - {arg2}.\n");
        }

        private void OnItemSelling(Item arg1, long price, long averagePrice, string priceSource)
        {
            var fraction = (float)price / averagePrice;
            Log($"SELLING [{fraction:F}] {arg1.Name} ({InventoryLib.FormatPrice(price)} - {priceSource}).");
        }

        private void OnResult(int count, long totalSum)
        {
            _count = _count - count;
            Log($"Script completed (did {count}), total sum {totalSum}.");
        }
    }
}
