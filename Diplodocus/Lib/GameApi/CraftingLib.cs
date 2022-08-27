using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.Pricing;
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
        private readonly PricingLib        _pricingLib;

        private ExcelSheet<Recipe> _recipeSheet;
        private ExcelSheet<Item>   _itemSheet;

        public CraftingLib(DataManager dataManager, InventoryLib inventoryLib, UniversalisClient universalis, PricingLib pricingLib)
        {
            _inventoryLib = inventoryLib;
            _universalis = universalis;
            _pricingLib = pricingLib;
            _recipeSheet = dataManager.GameData.GetExcelSheet<Recipe>();
            _itemSheet = dataManager.GameData.GetExcelSheet<Item>();
        }

        public int? GetRequiredLevel(ulong resultId)
        {
            var resultType = _inventoryLib.GetItemType(resultId);
            if (resultType == null)
            {
                return null;
            }

            var recipe = FindRecipe(resultId);
            return recipe.RecipeLevelTable.Value.ClassJobLevel;
        }

        public uint? GetClassJobCategory(ulong resultId)
        {
            var resultType = _inventoryLib.GetItemType(resultId);
            if (resultType == null)
            {
                return null;
            }

            return resultType.ClassJobCategory.Row;
        }

        public IEnumerable<(ulong, int)> GetIngredients(ulong resultId)
        {
            var resultType = _inventoryLib.GetItemType(resultId);
            if (resultType == null)
            {
                yield break;
            }

            var recipe = FindRecipe(resultId);
            foreach (var matRequired in recipe.UnkData5)
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

            var recipe = FindRecipe(resultId);
            var result = new List<Ingredient>();

            foreach (var matRequired in recipe.UnkData5)
            {
                if (matRequired.ItemIngredient == 0 || matRequired.ItemIngredient == -1)
                {
                    continue;
                }
                
                var ingredientType = _itemSheet.GetRow((uint)matRequired.ItemIngredient);
                if (ingredientType == null)
                {
                    PluginLog.Error($"Ingredient type null for {matRequired.ItemIngredient}");
                    return null;
                }

                if (ingredientType.IsUntradable)
                {
                    return null;
                }

                var marketData = await _universalis.GetDCData(ingredientType.RowId);

                PluginLog.Debug($"Ingredient {ingredientType.Name} avg {marketData.averageSoldPrice} {marketData.currentMinimumPrice}");
                result.Add(new Ingredient
                {
                    id = ingredientType.RowId,
                    type = ingredientType,
                    amount = matRequired.AmountIngredient,
                    priceTotalNQ = _pricingLib.CalculateAverageBuyingPrice(marketData) * matRequired.AmountIngredient,
                });
            }

            return result;
        }

        public Recipe? FindRecipe(ulong itemId)
        {
            var recipes = _recipeSheet.Where(r => r.ItemResult.Row == itemId);
            foreach (var recipe in recipes)
            {
                if (recipe.CraftType.Row == 5)
                {
                    return recipe;
                }
            }

            PluginLog.Error($"Failed to find recipe for {itemId} with craft type 5");
            return null;
        }
    }
}