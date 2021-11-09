using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using ImGuiNET;

namespace Diplodocus.Assistants.Storefront
{
    public sealed class StorefrontAssistant : IAssistant
    {
        private RetainerControl       _retainerControl;
        private InventoryLib          _inventoryLib;
        private CraftingListAssistant _craftingListAssistant;
        private CraftingLib           _craftingLib;

        public bool Open = false;

        private StorefrontData _data;

        public StorefrontAssistant(RetainerControl retainerControl, InventoryLib inventoryLib, StorefrontData data, CraftingListAssistant craftingListAssistant, CraftingLib craftingLib)
        {
            _retainerControl = retainerControl;
            _inventoryLib = inventoryLib;
            _data = data;
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
                    _data.FetchData();
                }

                ImGui.SameLine();
                if (ImGui.Button("Add all"))
                {
                    foreach (var item in _data.Items)
                    {
                        var itemId = InventoryLib.EncodeItemId(item.Item1, item.Item2);
                        var retainerCount = _retainerControl.CountInRetainerMarkets(itemId);
                        var left = item.Item3 - retainerCount;

                        if (!_craftingListAssistant.CheckIfOnCraftingList(item.Item1, item.Item3))
                        {
                            PluginLog.Debug($"Adding {item.Item1.Name} to crafting list");
                            _craftingListAssistant.AddCraft(item.Item1, _craftingLib.GetIngredients(item.Item1.RowId), left);
                        }
                    }
                }

                ImGui.Columns(4);
                ImGui.SetColumnWidth(1, 100);
                ImGui.SetColumnWidth(2, 100);
                ImGui.SetColumnWidth(3, 200);
                ImGui.Text("Item");
                ImGui.NextColumn();
                ImGui.Text("Required");
                ImGui.NextColumn();
                ImGui.Text("Current");
                ImGui.NextColumn();
                ImGui.NextColumn();

                var totalSlots = 0;
                var totalStorefront = 0;
                foreach (var item in _data.Items)
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

                ImGui.NextColumn();
                ImGui.Text(totalSlots.ToString());
                ImGui.NextColumn();
                ImGui.Text(totalStorefront.ToString());

                ImGui.End();
            }
        }
    }
}
