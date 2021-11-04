using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.GameApi.Inventory
{
    public sealed unsafe class InventoryLib
    {
        public struct ItemElement
        {
            public ulong id;
            public Item  type;
            public bool  hq;
            public uint  amount;
            public short slot;
        }

        private DataManager _dataManager;

        private InventoryManager* _inventoryManager;
        private ExcelSheet<Item>  _itemSheet;

        public static InventoryType[] PlayerInventories = { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 };

        public InventoryLib(DataManager dataManager)
        {
            _dataManager = dataManager;

            Enable();
        }

        public void Enable()
        {
            Resolver.Initialize();

            _itemSheet = _dataManager.GameData.GetExcelSheet<Item>();
            _inventoryManager = InventoryManager.Instance();
        }

        public void Disable()
        {
        }

        public void Dispose()
        {
            Disable();
        }

        public uint Count(ulong itemId, InventoryType[] inventoryTypes)
        {
            uint result = 0;
            foreach (var item in EnumerateInventories(inventoryTypes))
            {
                if (item.id == itemId)
                {
                    result += item.amount;
                }
            }

            return result;
        }

        public IEnumerable<ItemElement> EnumerateInventories(InventoryType[] inventoryTypes)
        {
            foreach (var inventoryType in inventoryTypes)
            {
                foreach (var item in EnumerateInventory(inventoryType))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<ItemElement> EnumerateInventory(InventoryType inventoryType)
        {
            for (var i = 0; i < GetContainerSize(inventoryType); i++)
            {
                var item = GetItem(inventoryType, i);
                var itemId = (item.Flags & InventoryItem.ItemFlags.HQ) != 0 ? item.ItemID + 1000000 : item.ItemID;
                ParseItemId(itemId, out var id, out var hq);

                if (id == 0)
                {
                    continue;
                }

                var type = _itemSheet.GetRow(id);
                if (type == null)
                {
                    PluginLog.Warning($"Unknown item id not in sheets: {id}");
                    continue;
                }

                yield return new ItemElement
                {
                    id = itemId,
                    type = type,
                    hq = hq,
                    amount = item.Quantity,
                    slot = item.Slot,
                };
            }
        }

        public void ParseItemId(uint id, out uint realId, out bool hq)
        {
            hq = id > 1000000;
            realId = hq ? id - 1000000 : id;
        }

        public uint GetContainerSize(InventoryType t)
        {
            var container = _inventoryManager->GetInventoryContainer(t);
            return container->Size;
        }

        public InventoryItem GetItem(InventoryType t, int idx)
        {
            var container = _inventoryManager->GetInventoryContainer(t);
            return container->Items[idx];
        }
    }
}
