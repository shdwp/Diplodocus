using System.Text;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Scripts.InventorySell
{
    public sealed class InventorySellUI
    {
        private int           _minPrice = 1000;
        private int           _count    = 1;
        private StringBuilder _log = new();

        private InventorySellScript    _sellScript;
        private InventoryInspectScript _inventoryInspectScript;
        private bool                   _shouldContinue = true;

        public InventorySellUI(InventorySellScript sellScript, InventoryInspectScript inventoryInspectScript)
        {
            _sellScript = sellScript;
            _inventoryInspectScript = inventoryInspectScript;
        }

        public void Draw()
        {
            ImGui.Text("Min price:");
            ImGui.SameLine();
            ImGui.InputInt("##minprice", ref _minPrice);

            ImGui.Text("Item count:");
            ImGui.SameLine();
            ImGui.InputInt("##count", ref _count);

            if (ImGui.Button("Start##invsellstart"))
            {
                _log.Clear();

                _shouldContinue = true;
                _sellScript.Start(new InventorySellScript.SellSettings
                {
                    minimumPrice = _minPrice,
                    itemCount = _count,
                    ShouldContinue = ShouldContinue,
                    OnItemSkipped = OnItemSkipped,
                    OnItemSelling = OnItemSelling,
                    OnScriptCompleted = OnScriptCompleted,
                    OnScriptFailed = OnScriptFailed
                });
            }


            ImGui.SameLine();
            if (ImGui.Button("Stop##invsellstop"))
            {
                _shouldContinue = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Inspect"))
            {
                _log.Clear();

                _inventoryInspectScript.Start(new InventorySellScript.SellSettings
                {
                    minimumPrice = _minPrice,
                    itemCount = _count,
                    ShouldContinue = ShouldContinue,
                    OnItemSkipped = OnItemSkipped,
                    OnItemSelling = OnItemSelling,
                    OnScriptCompleted = OnScriptCompleted
                });
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

        private void OnItemSkipped(Item arg1, string arg2)
        {
            _log.Append($"SKIP {arg1.Name} - {arg2}.\n");
        }

        private void OnItemSelling(Item arg1, long arg2)
        {
            _log.Append($"SELLING {arg1.Name} ({arg2}).\n");
        }

        private void OnScriptCompleted(int count, long totalSum)
        {
            _count = _count - count;
            _log.Append($"Script completed (did {count}), total sum {totalSum}.\n");
        }
    }
}
