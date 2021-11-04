using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Diplodocus.GameApi.Inventory;
using Diplodocus.Universalis;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Diplodocus.Scripts.InventorySell
{
    public sealed class InventoryInspectScript
    {
        [PluginService] private GameGui     GameGui     { get; set; }
        [PluginService] private DataManager DataManager { get; set; }

        private UniversalisClient _universalis;
        private InventoryLib      _inventoryLib;

        public InventoryInspectScript(UniversalisClient universalis, InventoryLib inventoryLib)
        {
            _universalis = universalis;
            _inventoryLib = inventoryLib;
        }

        public void Enable()
        {
        }

        public void Disable()
        {

        }

        public void Dispose()
        {
            Disable();
        }

        public async Task Start(InventorySellScript.SellSettings settings)
        {
            var types = new[] { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3 };

            var sellingCount = 0;
            long sellingSum = 0;

            foreach (var item in _inventoryLib.EnumerateInventories(types))
            {
                var data = await _universalis.GetMarketBoard(0, item.id);
                if (data == null || data.CurrentMinimumPrice == 0)
                {
                    settings.OnItemSkipped?.Invoke(item.type, "no market data");
                    continue;
                }

                if (data.CurrentMinimumPrice < settings.minimumPrice)
                {
                    settings.OnItemSkipped?.Invoke(item.type, $"price too low ({data.CurrentMinimumPrice})");
                    continue;
                }

                if (data.CurrentMinimumPrice < item.type.PriceLow)
                {
                    settings.OnItemSkipped?.Invoke(item.type, $"vendor price higher ({data.CurrentMinimumPrice} vs {item.type.PriceLow})");
                    continue;
                }

                settings.OnItemSelling?.Invoke(item.type, (long)data.CurrentMinimumPrice);
                sellingCount++;
                sellingSum += item.amount * (long)data.CurrentMinimumPrice;
            }

            settings.OnScriptCompleted?.Invoke(sellingCount, sellingSum);
        }
    }
}
