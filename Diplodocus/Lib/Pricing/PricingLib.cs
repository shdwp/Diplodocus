using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diplodocus.Assistants.Storefront;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.GSheets;
using Diplodocus.Universalis;

namespace Diplodocus.Lib.Pricing
{
    public sealed class PricingLib : IDisposable
    {
        public struct CalculatedPrice
        {
            public long   price;
            public string source;
        }

        private readonly GSheetsClient  _gSheetsClient;
        private readonly StorefrontData _storefrontData;

        private IReadOnlyList<GSheetsClient.Row> _mbData;

        public PricingLib(GSheetsClient gSheetsClient, StorefrontData storefrontData)
        {
            _gSheetsClient = gSheetsClient;
            _storefrontData = storefrontData;

            UpdateData();
        }

        public void Dispose()
        {
        }

        public async Task UpdateData()
        {
            foreach (var row in await _gSheetsClient.Fetch("MB Data"))
            {

            }
        }

        public void CalculateCurrentBuyingPrice(MarketBoardData data, int neededAmount, out double averagePrice, out string serverName)
        {
            var totalAveragePrice = 0.0;

            var totalAveragePriceCount = 0;
            var servers = new Dictionary<string, int>();
            foreach (var listing in data.listings)
            {
                servers[listing.worldName] = servers.GetValueOrDefault(listing.worldName) + listing.amount;

                if (totalAveragePriceCount < neededAmount * 1.5)
                {
                    totalAveragePriceCount += listing.amount;
                    totalAveragePrice += listing.price * listing.amount;
                }
            }

            serverName = "";
            averagePrice = totalAveragePrice / totalAveragePriceCount;

            foreach (var kv in servers)
            {
                if (kv.Value > neededAmount * 1.5)
                {
                    serverName = kv.Key;
                    break;
                }
            }
        }

        public double CalculateAverageBuyingPrice(MarketBoardData data)
        {
            return data.averageSoldPrice.Value;
        }

        public void CalculateCurrentSellingPrice(long currentWorldMin, MarketBoardData data, out long price, out string source)
        {
            var maxPrice = (long)(CalculateAverageSellingPrice(data) * 1.5f);
            var minPrice = (long)(_storefrontData.FindMinimumPrice(data.id) ?? 0);

            if (minPrice == 0)
            {
                minPrice = (long)(maxPrice * 0.65f);
            }

            var worldUndercutPrice = currentWorldMin - 1;
            if (worldUndercutPrice > minPrice && worldUndercutPrice < maxPrice)
            {
                price = worldUndercutPrice;
                source = PriceSourceString($"wou", price, minPrice, maxPrice);
                return;
            }

            var dcUndercut = (long)(data.currentMinimumPrice ?? 0.0) - 1;
            if (dcUndercut > minPrice && dcUndercut < maxPrice)
            {
                price = dcUndercut;
                source = PriceSourceString("dcu", price, minPrice, maxPrice);
                return;
            }

            if (worldUndercutPrice < minPrice || dcUndercut < minPrice)
            {
                price = minPrice;
                source = PriceSourceString("min", price, minPrice, maxPrice);
                return;
            }

            price = maxPrice;
            source = PriceSourceString("avg", price, minPrice, maxPrice);
        }

        public double CalculateAverageSellingPrice(MarketBoardData data)
        {
            return data.averageSoldPrice.Value * 0.9f;
        }
        
        public static string FormatPrice(double num)
        {
            if (num >= 100000)
                return FormatPrice(num / 1000) + "K";

            if (num >= 10000)
                return (num / 1000D).ToString("0.#") + "K";

            return num.ToString("#,0");
        }

        private static string PriceSourceString(string pref, long price, long min, long max)
        {
            return $"{pref} {FormatPrice(min)} ={FormatPrice(price)}= {FormatPrice(max)}";
        }
    }
}