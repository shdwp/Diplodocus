using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Universalis;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Lib.GameApi
{
    public sealed class CraftingLib
    {
        public struct Ingredient
        {
            public ulong  id;
            public Item   type;
            public int    amount;
            public double priceTotalNQ;
            public double priceTotalHQ;
        }

        private readonly InventoryLib      _inventoryLib;
        private readonly UniversalisClient _universalis;

        private ExcelSheet<Recipe> _recipeSheet;
        private ExcelSheet<Item>   _itemSheet;

        public CraftingLib(DataManager dataManager, InventoryLib inventoryLib, UniversalisClient universalis)
        {
            _inventoryLib = inventoryLib;
            _universalis = universalis;
            _recipeSheet = dataManager.GameData.GetExcelSheet<Recipe>();
            _itemSheet = dataManager.GameData.GetExcelSheet<Item>();
        }

        public IEnumerable<(ulong, int)> GetIngredients(ulong resultId)
        {
            var resultType = _inventoryLib.GetItemType(resultId);
            if (resultType == null)
            {
                yield break;
            }

            var recipe = _recipeSheet.First(r => r.ItemResult.Row == resultId);
            foreach (var matRequired in recipe.UnkStruct5)
            {
                if (matRequired.ItemIngredient != 0 && matRequired.ItemIngredient != -1)
                {
                    yield return ((ulong)matRequired.ItemIngredient, matRequired.AmountIngredient);
                }
            }
        }

        public async Task<IReadOnlyList<Ingredient>> GetIngredientsCost(ulong resultId)
        {
            var resultType = _inventoryLib.GetItemType(resultId);
            if (resultType == null)
            {
                return null;
            }

            var recipe = _recipeSheet.First(r => r.ItemResult.Row == resultId);
            var result = new List<Ingredient>();

            foreach (var matRequired in recipe.UnkStruct5)
            {
                if (matRequired.ItemIngredient == 0 || matRequired.ItemIngredient == -1)
                {
                    continue;
                }

                var ingredientType = _itemSheet.GetRow((uint)matRequired.ItemIngredient);
                if (ingredientType == null)
                {
                    PluginLog.Error($"Ingredient type null for {matRequired.ItemIngredient}");
                    continue;
                }

                var marketData = await _universalis.GetDCData(ingredientType.RowId);
                if (marketData == null)
                {
                    return null;
                }

                PluginLog.Debug($"Ingredient {ingredientType.Name} avg {marketData.averagePriceNQ} {marketData.averageMinimumPrice}");
                result.Add(new Ingredient
                {
                    id = ingredientType.RowId,
                    type = ingredientType,
                    amount = matRequired.AmountIngredient,
                    priceTotalHQ = (marketData.averagePriceHQ ?? -1) * matRequired.AmountIngredient,
                    priceTotalNQ = (marketData.averagePriceNQ ?? -1) * matRequired.AmountIngredient,
                });
            }

            return result;
        }
    }
}
