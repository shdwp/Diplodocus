using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Diplodocus.Universalis;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Assistants
{
    public sealed class CraftingLogInspectAssistant : IAssistant
    {
        private readonly Framework             _framework;
        private readonly GameGui               _gameGui;
        private readonly KeyState              _keyState;
        private readonly UniversalisClient     _universalis;
        private readonly AtkControl            _atkControl;
        private readonly ChatGui               _chatGui;
        private readonly InventoryLib          _inventoryLib;
        private readonly CraftingLib           _craftingLib;
        private readonly CraftingListAssistant _craftingListAssistant;

        private ulong _lastItemChecked;
        private ulong _lastItemInspected;
        private bool  _shoppingListAddHotkeyState;

        public CraftingLogInspectAssistant(GameGui gameGui, KeyState keyState, CraftingLib craftingLib, InventoryLib inventoryLib, UniversalisClient universalis, ChatGui chatGui, AtkControl atkControl, Framework framework, CraftingListAssistant craftingListAssistant)
        {
            _gameGui = gameGui;
            _keyState = keyState;
            _craftingLib = craftingLib;
            _inventoryLib = inventoryLib;
            _universalis = universalis;
            _chatGui = chatGui;
            _atkControl = atkControl;
            _framework = framework;
            _craftingListAssistant = craftingListAssistant;

            PluginLog.LogDebug($"Shopping list {_craftingListAssistant.GetHashCode()}");

            _gameGui.HoveredItemChanged += OnHoveredItemChanged;
            _framework.Update += OnUpdate;
        }

        public void Dispose()
        {
            _gameGui.HoveredItemChanged -= OnHoveredItemChanged;
            _framework.Update -= OnUpdate;
        }

        private void OnUpdate(Framework framework)
        {
            if (_keyState[VirtualKey.SHIFT])
            {
                return;
            }

            if (_gameGui.HoveredItem == 0)
            {
                return;
            }

            if (_keyState[VirtualKey.MENU] && _keyState[VirtualKey.CONTROL])
            {

            }
            else if (!_atkControl.IsWindowFocused(AtkControl.CraftingLog))
            {
                return;
            }

            var type = _inventoryLib.GetItemType(_gameGui.HoveredItem);
            if (type == null)
            {
                var id = _gameGui.HoveredItem - 500000;
                type = _inventoryLib.GetItemType(id);

                if (type == null)
                {
                    PluginLog.Debug($"Couldn't find item in sheet: base {_gameGui.HoveredItem}, collectible adj {id}");
                    return;
                }
            }

            if (_keyState[VirtualKey.CONTROL] && _lastItemChecked != _gameGui.HoveredItem)
            {
                _lastItemChecked = _gameGui.HoveredItem;
                var _ = PerformHoveredItemMarketInspection(type);
            }

            if (_keyState[VirtualKey.F] && _lastItemInspected != _gameGui.HoveredItem)
            {
                _lastItemInspected = _gameGui.HoveredItem;
                var _ = PerformHoveredItemCraftingInspection(type);
            }

            if (!_keyState[VirtualKey.T] && _shoppingListAddHotkeyState)
            {
                var _ = PerformAddToShoppingList(type);
            }

            _shoppingListAddHotkeyState = _keyState[VirtualKey.T];
        }

        private void OnHoveredItemChanged(object? sender, ulong e)
        {
        }

        private async Task PerformAddToShoppingList(Item item)
        {
            PluginLog.Debug($"Adding {item.Name} to shopping list");
            _craftingListAssistant.AddCraft(item, _craftingLib.GetIngredients(item.RowId));
        }

        private async Task PerformHoveredItemMarketInspection(Item resultType)
        {
            var resultMarketData = await _universalis.GetDCData(resultType.RowId);

            var msg = new SeString();

            if (resultMarketData != null)
            {
                msg.Append(new UIGlowPayload(GameColors.Blue));
                msg.Append(new TextPayload($"{resultMarketData.averageSoldPerDay.Value:F1}{(char)SeIconChar.Experience}"));
                msg.Append(UIGlowPayload.UIGlowOff);
            }

            msg.Append(new ItemPayload(resultType.RowId, false));
            msg.Append(new TextPayload(" " + resultType.Name));
            msg.Append(RawPayload.LinkTerminator);
            msg.Append(new ItemPayload(resultType.RowId, true));
            msg.Append(new TextPayload(" " + (char)SeIconChar.HighQuality));
            msg.Append(RawPayload.LinkTerminator);

            _chatGui.Print(msg);
        }

        private async Task PerformHoveredItemCraftingInspection(Item resultType)
        {
            var resultMarketData = await _universalis.GetDCData(resultType.RowId);
            var ingredientsData = await _craftingLib.GetIngredientsCost(resultType.RowId);

            var totalHQCost = ingredientsData.Sum(d => d.priceTotalHQ);
            var totalNQCost = ingredientsData.Sum(d => d.priceTotalNQ);

            var msg = new SeString();

            if (resultMarketData != null)
            {
                var nqnqProfit = resultMarketData.averagePriceNQ - totalNQCost;
                var hqhqProfit = resultMarketData.averagePriceHQ - totalHQCost;
                var nqhqProfit = resultMarketData.averagePriceHQ - totalNQCost;

                msg.Append(new UIGlowPayload(GameColors.Blue));
                msg.Append(new TextPayload($"{resultMarketData.averageSoldPerDay.Value:F1}{(char)SeIconChar.Experience}"));
                msg.Append(UIGlowPayload.UIGlowOff);

                msg.Append(new UIGlowPayload(nqnqProfit > 0 ? GameColors.Green : GameColors.Red));
                msg.Append(new TextPayload(" " + (char)SeIconChar.Square));
                msg.Append(new TextPayload($" {InventoryLib.FormatPrice(nqnqProfit.Value)}{(char)SeIconChar.Gil}"));
                msg.Append(UIGlowPayload.UIGlowOff);

                msg.Append(new UIGlowPayload(hqhqProfit > 0 ? GameColors.Green : GameColors.Red));
                msg.Append(new TextPayload(" " + (char)SeIconChar.Circle));
                msg.Append(new TextPayload($" {InventoryLib.FormatPrice(hqhqProfit.Value)}{(char)SeIconChar.Gil}"));
                msg.Append(UIGlowPayload.UIGlowOff);

                msg.Append(new UIGlowPayload(nqhqProfit > 0 ? GameColors.Green : GameColors.Red));
                msg.Append(new TextPayload(" " + (char)SeIconChar.Hexagon));
                msg.Append(new TextPayload($" {InventoryLib.FormatPrice(nqhqProfit.Value)}{(char)SeIconChar.Gil}"));
                msg.Append(UIGlowPayload.UIGlowOff);
            }

            msg.Append(new UIForegroundPayload(GameColors.Orange));
            msg.Append(new TextPayload($" {InventoryLib.FormatPrice(totalNQCost)}{(char)SeIconChar.Gil}"));
            msg.Append(UIForegroundPayload.UIForegroundOff);

            msg.Append(new UIForegroundPayload(GameColors.Green));
            msg.Append(new TextPayload($" {InventoryLib.FormatPrice(resultMarketData.averagePriceNQ.Value)}{(char)SeIconChar.Gil}"));
            msg.Append(UIForegroundPayload.UIForegroundOff);

            msg.Append(new ItemPayload(resultType.RowId, false));
            msg.Append(new TextPayload(" " + resultType.Name));
            msg.Append(RawPayload.LinkTerminator);
            msg.Append(new ItemPayload(resultType.RowId, true));
            msg.Append(new TextPayload(" " + (char)SeIconChar.HighQuality));
            msg.Append(RawPayload.LinkTerminator);

            _chatGui.Print(msg);
        }
    }
}
