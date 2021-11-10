using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GameControl;
using Diplodocus.Universalis;

namespace Diplodocus.Automatons.CraftingLogResearch
{
    public sealed class CraftingLogResearchAutomatonScript : BaseAutomatonScript<CraftingLogResearchAutomatonScript.ResearchSettings>
    {
        private readonly AtkControl        _atkControl;
        private readonly UniversalisClient _universalis;
        private readonly InventoryLib      _inventoryLib;
        private readonly HIDControl        _hidControl;
        private readonly GameGui           _gameGui;
        private readonly CraftingLib       _craftingLib;

        protected override bool ShouldDelayStart => true;

        public class ResearchSettings : BaseAutomatonScriptSettings
        {
            public int            sections;
            public Action<string> OnResult;
        }

        public CraftingLogResearchAutomatonScript(AtkControl atkControl, HIDControl hidControl, GameGui gameGui, CraftingLib craftingLib, UniversalisClient universalis, InventoryLib inventoryLib)
        {
            _atkControl = atkControl;
            _hidControl = hidControl;
            _gameGui = gameGui;
            _craftingLib = craftingLib;
            _universalis = universalis;
            _inventoryLib = inventoryLib;
        }

        public override void Dispose()
        {
        }

        protected override bool ValidateStart()
        {
            return _atkControl.IsWindowFocused(AtkControl.CraftingLog);
        }

        public override async Task StartImpl()
        {
            await _hidControl.CursorConfirm();
            var result = new StringBuilder();

            foreach (var sectionId in Enumerable.Range(0, _settings.sections))
            {
                if (CheckIfStopped())
                {
                    return;
                }

                await _hidControl.CursorConfirm();
                while (_gameGui.HoveredItem != 0)
                {
                    if (CheckIfStopped())
                    {
                        return;
                    }

                    await PerformInspection(sectionId, _gameGui.HoveredItem);
                    await _hidControl.CursorDown();
                }

                await _hidControl.CursorCancel();
                await _hidControl.CursorDown();
            }

            _settings.OnResult?.Invoke(result.ToString());
        }

        private async Task PerformInspection(int section, ulong itemId)
        {
            var itemType = _inventoryLib.GetItemType(itemId);

            var resultMarketData = await _universalis.GetDCData(itemId);
            if (resultMarketData == null)
            {
                PluginLog.Warning($"MB data null for " + itemId);
                return;
            }

            var ingredientsData = await _craftingLib.GetIngredientsCost(itemId);
            if (ingredientsData == null)
            {
                PluginLog.Warning($"MB ingredients data null for " + itemId);
                return;
            }

            var totalHQCost = ingredientsData.Sum(d => d.priceTotalHQ);
            var totalNQCost = ingredientsData.Sum(d => d.priceTotalNQ);
            var avgProfit = resultMarketData.averagePrice - totalNQCost;

            _settings.OnResult(
                $"{section}\t" +
                $"{itemType.Name}\t" +
                $"{resultMarketData.averageSoldPerDay}\t" +
                $"{totalNQCost}\t" +
                $"{resultMarketData.averagePrice}\t" +
                $"{avgProfit}\t" +
                $"{resultMarketData.averagePriceNQ}\t" +
                $"{resultMarketData.averagePriceHQ}\t" +
                "\n"
            );
        }
    }
}
