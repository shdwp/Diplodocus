using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Diplodocus.Universalis;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace Diplodocus.Assistants
{
    public sealed class CraftingListAssistant : IAssistant
    {
        private class ShoppingListSection
        {
            public string                 name;
            public int                    multiplier = 1;
            public Dictionary<ulong, int> contents   = new();

            public ShoppingListSection(string name)
            {
                this.name = name;
            }
        }

        public bool open;

        private readonly InventoryLib      _inventoryLib;
        private readonly AtkControl        _atkControl;
        private readonly UniversalisClient _universalisClient;

        private Dictionary<Item, ShoppingListSection> _craftingList         = new();
        private Dictionary<ulong, int>                _shoppingList         = new();
        private List<ulong>                           _autocopySkippedItems = new();
        private bool                                  _autocopy             = true;
        private Item                                  _uncategorizedItem;
        private ConcurrentDictionary<ulong, double>   _itemPrices = new();

        public CraftingListAssistant(InventoryLib inventoryLib, AtkControl atkControl, KeyState keyState, Framework framework, UniversalisClient universalisClient)
        {
            _inventoryLib = inventoryLib;
            _atkControl = atkControl;
            _universalisClient = universalisClient;

            _uncategorizedItem = new Item();
            _uncategorizedItem.Name = new SeString("Uncategorized");

            Clear();
        }

        public void Dispose()
        {
        }

        public void Clear()
        {
            _craftingList.Clear();
            _shoppingList.Clear();
        }

        public void AddUncategorized(uint rawId)
        {
            if (_craftingList.TryGetValue(_uncategorizedItem, out var section))
            {
                section = new ShoppingListSection("Uncategorized");
                _craftingList[_uncategorizedItem] = section;
            }

            section.contents[rawId] = section.contents.GetValueOrDefault(rawId) + 1;
            open = true;
        }

        public void AddCraft(Item resultType, IEnumerable<(ulong, int)> ingredients, int multiplier = 1)
        {
            if (!_craftingList.TryGetValue(resultType, out var section))
            {
                section = new ShoppingListSection(resultType.Name);
                _craftingList[resultType] = section;

                section.multiplier = multiplier;
                section.contents = ingredients.ToDictionary(t => t.Item1, t => t.Item2);
            }
            else
            {
                section.multiplier += multiplier;
            }

            RecalculateShoppingList();
            open = true;
        }

        public bool CheckIfOnCraftingList(Item itemType, int amount)
        {
            if (_craftingList.TryGetValue(itemType, out var section))
            {
                return section.multiplier >= amount;
            }

            return false;
        }

        public void Draw()
        {
            if (open)
            {
                ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
                if (ImGui.Begin("Crafting Assistant", ref open))
                {
                    ImGui.Checkbox("##autocopy", ref _autocopy);
                    ImGui.SameLine();
                    ImGui.Text("Autocopy");

                    if (ImGui.BeginTabBar("##cla_tabbar"))
                    {
                        if (ImGui.BeginTabItem("Crafting"))
                        {
                            DrawCraftingList();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Shopping"))
                        {
                            DrawShoppingList();
                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }

                    ImGui.End();
                }
            }
            else
            {
                _autocopySkippedItems.Clear();
            }
        }

        private void DrawCraftingList()
        {
            var craftingLogOpen = _atkControl.IsWindowFocused(AtkControl.CraftingLog);
            var autocopied = false;

            if (ImGui.Button("Clear##cla_clear"))
            {
                Clear();
            }

            ImGui.BeginChild("##cla_cl");
            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 100);
            ImGui.SetColumnWidth(1, 230);
            ImGui.Text("Have");
            ImGui.NextColumn();
            ImGui.Text("To craft");
            ImGui.NextColumn();
            ImGui.Text("Item");

            foreach (var itemSection in _craftingList.ToDictionary(kv => kv.Key, kv => kv.Value))
            {
                var section = itemSection.Value;
                var amountInInventory = _inventoryLib.Count(itemSection.Key.RowId, InventoryLib.PlayerInventories, true);
                var completed = amountInInventory >= section.multiplier;

                ImGui.NextColumn();
                ImGui.Text(amountInInventory.ToString());

                ImGui.NextColumn();
                ImGui.SetNextItemWidth(150);
                if (ImGui.InputInt("##cla_mult_" + section.name, ref section.multiplier))
                {
                    RecalculateShoppingList();
                }

                ImGui.SameLine();
                if (ImGui.Button(" X ##cla_sect_remove_" + section.name))
                {
                    _craftingList.Remove(itemSection.Key);
                    RecalculateShoppingList();
                }

                ImGui.NextColumn();
                ImGui.TextColored(completed ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, section.name);

                ImGui.SameLine();
                if (ImGui.SmallButton("copy##cla_sect_copy_" + section.name))
                {
                    _autocopy = false;
                    ImGui.SetClipboardText(section.name);
                }

                if (_autocopy && !autocopied && !completed && craftingLogOpen)
                {
                    ImGui.SetClipboardText(section.name);
                    autocopied = true;
                }
            }

            ImGui.EndChild();
        }

        private void DrawShoppingList()
        {
            var marketBoardOpen = _atkControl.IsWindowFocused(AtkControl.MarketBoard);
            var autocopied = false;

            if (ImGui.Button("Autocalculate##cla_autocalc"))
            {
                RecalculateShoppingList();
                _autocopySkippedItems.Clear();
            }
            ImGui.SameLine();

            var totalItems = 0;
            double totalCost = 0;
            foreach (var kv in _shoppingList)
            {
                var hasBoughtEverything = CalculateAmounts(kv.Key, kv.Value, out var amountInventory, out var amountLeftToBuy);
                if (!hasBoughtEverything)
                {
                    totalItems += kv.Value;
                    totalCost += _itemPrices.GetValueOrDefault(kv.Key) * kv.Value;
                }
            }

            ImGui.Text($"Total items: {totalItems}. Total cost: {InventoryLib.FormatPrice(totalCost)}.");

            ImGui.BeginChild("##cla_list");
            ImGui.Columns(4);
            ImGui.SetColumnWidth(1, 80);
            ImGui.SetColumnWidth(2, 120);
            ImGui.SetColumnWidth(3, 120);

            var sortedList = _shoppingList.ToList();
            sortedList.Sort((a, b) =>
            {
                return a.Key.CompareTo(b.Key);
            });

            foreach (var kv in sortedList)
            {
                var item = _inventoryLib.GetItemType(kv.Key);
                var hasBoughtEverything = CalculateAmounts(kv.Key, kv.Value, out var amountInventory, out var amountLeftToBuy);
                if (hasBoughtEverything)
                {
                    continue;
                }

                if (ImGui.Checkbox("##checkbox" + kv.Key + kv.Key, ref hasBoughtEverything) && hasBoughtEverything)
                {
                    MarkItemAsResolved(kv.Key);
                }

                var lineColor = ImGuiColors.DalamudGrey;
                if (amountLeftToBuy > 0)
                {
                    if (!_autocopy)
                    {
                        ImGui.SameLine();
                        if (ImGui.ArrowButton("##copy" + kv.Key, ImGuiDir.Right))
                        {
                            ImGui.SetClipboardText(item.Name);
                            _autocopy = false;
                        }
                    }

                    if (_autocopy && !autocopied && !_autocopySkippedItems.Contains(kv.Key))
                    {
                        ImGui.SameLine();
                        if (ImGui.ArrowButton("##skip" + kv.Key, ImGuiDir.Down))
                        {
                            _autocopySkippedItems.Add(kv.Key);
                        }
                        else
                        {
                            if (marketBoardOpen)
                            {
                                ImGui.SetClipboardText(item.Name);
                            }

                            autocopied = true;
                        }

                        lineColor = ImGuiColors.DalamudOrange;
                    }
                    else
                    {
                        lineColor = ImGuiColors.DalamudRed;
                    }
                }

                ImGui.SameLine();
                ImGui.TextColored(lineColor, item.Name);
                ImGui.NextColumn();
                ImGui.TextColored(lineColor, Math.Max(amountLeftToBuy, 0).ToString());

                ImGui.NextColumn();
                if (hasBoughtEverything)
                {
                    ImGui.Text("");
                }
                else if (_itemPrices.TryGetValue(kv.Key, out var price))
                {
                    ImGui.Text(InventoryLib.FormatPrice(price * amountLeftToBuy));
                }
                else
                {
                    ImGui.Text("...");
                }

                ImGui.NextColumn();
                ImGui.Text($"{kv.Value} / {amountInventory}");

                ImGui.NextColumn();
            }

            ImGui.EndChild();
        }

        private bool CalculateAmounts(ulong itemId, int amountNeeded, out int amountInventory, out int amountLeftToBuy)
        {
            amountInventory = _inventoryLib.Count(itemId, InventoryLib.PlayerInventories, true);
            amountLeftToBuy = amountNeeded - amountInventory;
            return amountLeftToBuy <= 0;
        }

        private void RecalculateShoppingList()
        {
            _shoppingList.Clear();

            foreach (var item in _craftingList.Values)
            {
                foreach (var mat in item.contents)
                {
                    _shoppingList[mat.Key] = _shoppingList.GetValueOrDefault(mat.Key) + mat.Value * item.multiplier;
                }
            }

            /*
            foreach (var itemId in _shoppingList.Keys)
            {
                if (!_itemPrices.ContainsKey(itemId))
                {
                    FetchPriceInBackground(itemId);
                }
            }
            */
        }

        private void MarkItemAsResolved(ulong item)
        {
            _shoppingList[item] = 0;
        }

        private async Task FetchPriceInBackground(ulong item)
        {
            var data = await _universalisClient.GetDCData(item);
            if (data != null)
            {
                _itemPrices[item] = data.averagePrice.Value;
            }
        }
    }
}
