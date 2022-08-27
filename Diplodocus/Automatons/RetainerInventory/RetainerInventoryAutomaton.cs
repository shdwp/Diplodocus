using Diplodocus.Lib.Automaton;
using ImGuiNET;

namespace Diplodocus.Automatons.RetainerInventory
{
    public class RetainerInventoryAutomaton : BaseAutomaton<RetainerInventoryAutomatonScript, RetainerInventoryAutomatonScript.RetainerInventorySettings>
    {
        private int  _itemCount    = 70;
        private bool _fromRetainer = true;

        private bool _itemsAll          = false;
        private bool _itemsShoppingList = true;

        public RetainerInventoryAutomaton(RetainerInventoryAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
            ImGui.Text("Item count:");
            ImGui.SameLine();
            ImGui.InputInt("##ria_itemcount", ref _itemCount);
            
            ImGui.Checkbox("##ria_fromretainer", ref _fromRetainer);
            ImGui.SameLine();
            ImGui.Text(" From retainer");
            
            ImGui.Checkbox("##ria_itemsall", ref _itemsAll);
            ImGui.SameLine();
            ImGui.Text(" Move all");

            ImGui.Checkbox("##ria_itemsshoppinglist", ref _itemsShoppingList);
            ImGui.SameLine();
            ImGui.Text(" Move shopping list");
        }

        public override string GetName()
        {
            return "RetInv";
        }

        public override RetainerInventoryAutomatonScript.RetainerInventorySettings GetSettings()
        {
            return new()
            {
                itemCount = _itemCount,
                fromRetainer = _fromRetainer,
                itemsAll = _itemsAll,
                itemsShoppingList = _itemsShoppingList,
            };
        }
    }
}