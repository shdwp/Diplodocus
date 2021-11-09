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

        public List<(Item, bool, int)> Items;

        public StorefrontData(InventoryLib inventoryLib)
        {
            _inventoryLib = inventoryLib;
            Items = new List<(Item, bool, int)>();
            this._client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        }

        public async Task FetchData()
        {
            Items.Clear();

            var sheetName = HttpUtility.UrlEncode(_sheetName);
            var url = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}".Format(_id, sheetName);
            PluginLog.Debug(url);

            var response = await _client.GetStringAsync(url);
            var newItems = new List<(Item, bool, int)>();

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

                if (itemNameString.Any() && itemCountString.Any())
                {
                    var itemType = _inventoryLib.GetItemType(itemNameString);
                    if (itemType == null)
                    {
                        continue;
                    }

                    var itemCount = int.Parse(itemCountString);
                    newItems.Add((itemType, false, itemCount));
                }
            }

            Items = newItems;
        }
    }
}
