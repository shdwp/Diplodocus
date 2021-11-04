using System.Threading.Tasks;
using Dalamud.Logging;
using Diplodocus.Automatons.InventorySell;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Universalis;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Diplodocus.Automatons.InventoryInspect
{
    public sealed class InventoryInspectAutomatonScript : BaseAutomatonScript<InventorySellAutomatonScript.Settings>
    {
        private UniversalisClient _universalis;
        private InventoryLib      _inventoryLib;

        public InventoryInspectAutomatonScript(UniversalisClient universalis, InventoryLib inventoryLib)
        {
            _universalis = universalis;
            _inventoryLib = inventoryLib;
        }

        public override void Dispose()
        {
        }

        public override async Task StartImpl()
        {
            var types = new[] { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3 };

            var sellingCount = 0;
            long sellingSum = 0;

            foreach (var item in _inventoryLib.EnumerateInventories(types))
            {
                if (CheckIfStopped())
                {
                    return;
                }

                var data = await _universalis.GetWorldData(item.id);
                if (data == null || data.minimumPrice == 0)
                {
                    _settings.OnItemSkipped?.Invoke(item.type, "no market data");
                    continue;
                }

                if (data.minimumPrice < item.type.PriceLow)
                {
                    _settings.OnItemSkipped?.Invoke(item.type, $"vendor price higher ({data.minimumPrice} vs {item.type.PriceLow})");
                    continue;
                }

                var totalPrice = data.minimumPrice * item.amount;
                PluginLog.Debug($"Price for item {item.type.Name} - {totalPrice} ({data.minimumPrice}, amount {item.amount}");

                if (totalPrice < _settings.minimumPrice)
                {
                    _settings.OnItemSkipped?.Invoke(item.type, $"price too low ({totalPrice})");
                    continue;
                }

                _settings.OnItemSelling?.Invoke(item.type, (long)totalPrice);
                sellingCount++;
                sellingSum += (long)totalPrice;
            }

            _settings.OnResult?.Invoke(sellingCount, sellingSum);
        }

    }
}
