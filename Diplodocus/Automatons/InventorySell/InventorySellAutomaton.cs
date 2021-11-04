using Diplodocus.Lib.Automaton;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.InventorySell
{
    public sealed class InventorySellAutomaton : BaseAutomaton<InventorySellAutomatonScript, InventorySellAutomatonScript.Settings>
    {
        private int           _minPrice = 1000;
        private int           _count    = 1;

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

                OnItemSkipped = OnItemSkipped,
                OnItemSelling = OnItemSelling,
                OnResult = OnResult,
            };
        }

        private void OnItemSkipped(Item arg1, string arg2)
        {
            _log.Append($"SKIP {arg1.Name} - {arg2}.\n");
        }

        private void OnItemSelling(Item arg1, long arg2)
        {
            _log.Append($"SELLING {arg1.Name} ({arg2}).\n");
        }

        private void OnResult(int count, long totalSum)
        {
            _count = _count - count;
            _log.Append($"Script completed (did {count}), total sum {totalSum}.\n");
        }
    }
}
