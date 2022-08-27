using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Dalamud.Logging;
using Dalamud.Utility;
using Diplodocus.Lib.GameApi.Inventory;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Assistants.Storefront
{
    public sealed class StorefrontData
    {
        private readonly HttpClient   _client;
        private readonly InventoryLib _inventoryLib;

        private readonly string _id        = "1-KA1DFzaiJOCdFfTUdNKRaTUUXnoh3bef_erklXjN98";
        private readonly string _sheetName = "MBStorefront";

        public List<(Item, bool, int, int)> Items;

        public StorefrontData(InventoryLib inventoryLib)
        {
            _inventoryLib = inventoryLib;
            Items = new();

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        }

        public int? FindMinimumPrice(ulong itemId)
        {
            foreach (var item in Items)
            {
                if (item.Item1.RowId == itemId)
                {
                    return item.Item4;
                }
            }

            return null;
        }

        public async Task FetchData()
        {
            Items.Clear();

            var sheetName = HttpUtility.UrlEncode(_sheetName);
            var url = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}".Format(_id, sheetName);
            PluginLog.Debug(url);

            var response = await _client.GetStringAsync(url);
            var newItems = new List<(Item, bool, int, int)>();

            foreach (var rowString in response.Split('\n'))
            {
                var rowData = new List<string>();
                foreach (var colString in rowString.Split(','))
                {
                    var dataString = colString.Substring(1, colString.Length - 2);
                    rowData.Add(dataString);
                }

                var itemNameString = rowData[1];
                var itemCountString = rowData[6];
                var itemMinimumPrice = rowData[10];
                
                if (itemNameString.Any() && itemCountString.Any() && itemMinimumPrice.Any())
                {
                    var itemType = _inventoryLib.GetItemType(itemNameString);
                    if (itemType == null)
                    {
                        continue;
                    }

                    var itemCount = int.Parse(itemCountString);

                    if (itemCount > 0)
                    {
                        newItems.Add((itemType, false, itemCount, int.Parse(itemMinimumPrice)));
                    }
                }
            }

            Items = newItems;
        }
    }
}