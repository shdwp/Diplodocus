using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Diplodocus.Lib.Pricing;
using ImGuiNET;

namespace Diplodocus.Assistants.Storefront
{
    public sealed class StorefrontAssistant : IAssistant
    {
        private readonly RetainerControl       _retainerControl;
        private readonly CraftingListAssistant _craftingListAssistant;
        private readonly CraftingLib           _craftingLib;

        public bool Open = false;

        public StorefrontData Data;

        public StorefrontAssistant(RetainerControl retainerControl, StorefrontData data, CraftingListAssistant craftingListAssistant, CraftingLib craftingLib)
        {
            _retainerControl = retainerControl;
            Data = data;
            _craftingListAssistant = craftingListAssistant;
            _craftingLib = craftingLib;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (!Open)
                return;

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Storefront List", ref Open))
            {
                if (ImGui.Button("Fetch"))
                {
                    Data.FetchData();
                }

                ImGui.SameLine();
                if (ImGui.Button("Add all"))
                {
                    foreach (var item in Data.Items)
                    {
                        var itemId = InventoryLib.EncodeItemId(item.Item1, item.Item2);
                        var retainerCount = _retainerControl.CountInRetainerMarkets(itemId);
                        var left = item.Item3 - retainerCount;
                        
                        if (left > 0 && !_craftingListAssistant.CheckIfOnCraftingList(item.Item1, item.Item3))
                        {
                            PluginLog.Debug($"Adding {item.Item1.Name} to crafting list");
                            _craftingListAssistant.AddCraft(item.Item1, _craftingLib.GetIngredients(item.Item1.RowId), left);
                        }
                    }
                }

                ImGui.Columns(5);
                ImGui.SetColumnWidth(1, 100);
                ImGui.SetColumnWidth(2, 100);
                ImGui.SetColumnWidth(3, 200);
                ImGui.Text("Item");
                ImGui.NextColumn();
                ImGui.Text("Required");
                ImGui.NextColumn();
                ImGui.Text("Current");
                ImGui.NextColumn();
                ImGui.Text("MinPrice");
                ImGui.NextColumn();
                ImGui.NextColumn();

                var totalSlots = 0;
                var totalStorefront = 0;
                var totalSelling = 0;
                foreach (var item in Data.Items)
                {
                    var itemId = InventoryLib.EncodeItemId(item.Item1, item.Item2);
                    var retainerCount = _retainerControl.CountInRetainerMarkets(itemId);
                    var left = item.Item3 - retainerCount;
                    var onCraftingList = _craftingListAssistant.CheckIfOnCraftingList(item.Item1, left);

                    ImGui.TextColored(left > 0 && !onCraftingList ? ImGuiColors.DalamudRed : ImGuiColors.HealerGreen, item.Item1.Name);
                    ImGui.NextColumn();
                    ImGui.Text(item.Item3.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(retainerCount.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(PricingLib.FormatPrice(item.Item4));
                    ImGui.NextColumn();

                    if (onCraftingList)
                    {
                        ImGui.Text("On list");
                    }
                    else
                    {
                        if (left > 0 && ImGui.Button("Add to list##sa_craft_" + itemId))
                        {
                            PluginLog.Debug($"Adding {item.Item1.Name} to crafting list");
                            _craftingListAssistant.AddCraft(item.Item1, _craftingLib.GetIngredients(item.Item1.RowId), left);
                        }
                    }
                    ImGui.NextColumn();

                    totalSlots += item.Item3;
                    totalStorefront += retainerCount;
                }

                foreach (var kv in _retainerControl.EnumerateRetainerMarkets())
                {
                    totalSelling += kv.Value.amount;
                }

                ImGui.NextColumn();
                ImGui.Text(totalSlots.ToString());
                ImGui.NextColumn();
                ImGui.Text(totalStorefront.ToString());
                ImGui.NextColumn();
                ImGui.Text(totalSelling.ToString());

                ImGui.End();
            }
        }
    }
}