using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Logging;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Lib.GameApi.Inventory
{
    public sealed unsafe class InventoryLib : IDisposable
    {
        public struct ItemElement
        {
            public ulong id;
            public Item  type;
            public bool  hq;
            public uint  amount;
            public short slot;
        }

        private readonly InventoryManager* _inventoryManager;
        private readonly ExcelSheet<Item>  _itemSheet;

        public static readonly InventoryType[] PlayerInventories = { InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 };

        public InventoryLib(DataManager dataManager)
        {
            Resolver.Initialize();

            _itemSheet = dataManager.GameData.GetExcelSheet<Item>();
            _inventoryManager = InventoryManager.Instance();
        }

        public void Dispose()
        {
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

        public void ParseItemId(ulong id, out uint realId, out bool hq)
        {
            hq = id > 1000000;
            realId = hq ? (uint)id - 1000000 : (uint)id;
        }

        public Item GetItemType(ulong id)
        {
            ParseItemId(id, out var realId, out var _);
            return _itemSheet.GetRow(realId);
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
