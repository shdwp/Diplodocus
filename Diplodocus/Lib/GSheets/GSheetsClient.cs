using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi.Inventory;

namespace Diplodocus.Lib.GSheets
{
    public sealed partial class GSheetsClient
    {
        private readonly HttpClient   _client;
        private readonly InventoryLib _inventoryLib;

        private readonly string _id = "1-KA1DFzaiJOCdFfTUdNKRaTUUXnoh3bef_erklXjN98";

        public GSheetsClient(InventoryLib inventoryLib)
        {
            _inventoryLib = inventoryLib;

            this._client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        }

        public async Task<IReadOnlyList<Row>> Fetch(string sheetName)
        {
            sheetName = HttpUtility.UrlEncode(sheetName);
            var url = string.Format("https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}", _id, sheetName);

            PluginLog.Debug(url);
            var response = await _client.GetStringAsync(url);

            var result = new List<Row>();

            foreach (var rowString in response.Split('\n'))
            {
                var rowData = new List<string>();
                foreach (var colString in rowString.Split(','))
                {
                    var dataString = colString.Substring(1, colString.Length - 2);
                    rowData.Add(dataString);
                }

                var itemNameString = rowData[1];

                if (itemNameString.Any())
                {
                    var itemType = _inventoryLib.GetItemType(itemNameString);
                    if (itemType == null)
                    {
                        continue;
                    }

                    result.Add(new Row(itemType, rowData.Skip(1).ToArray()));
                }
            }

            return result;
        }
    }
}
