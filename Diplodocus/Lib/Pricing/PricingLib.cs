using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Logging;
using Diplodocus.Lib.GSheets;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Lib.Pricing
{
    public sealed class PricingLib : IDisposable
    {
        public struct PricingData
        {
            public Item   item;
            public double averageCost;
            public double averagePrice;
            public double averageSpeed;
        }

        private readonly GSheetsClient _gSheetsClient;

        public float MinProfit = 0.05f;

        private IReadOnlyList<GSheetsClient.Row> _mbData;

        public PricingLib(GSheetsClient gSheetsClient)
        {
            _gSheetsClient = gSheetsClient;

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

        public void CalculatePrice(long worldMin, long dcMin, long dcAvg, float minFraction, out long price, out string priceSource)
        {
            var worldMinFraction = (float)worldMin / dcAvg;
            if (worldMinFraction < minFraction)
            {
                priceSource = $"average price, world too low";
                price = dcAvg;
            }
            else if (dcAvg < worldMin - 1)
            {
                priceSource = $"average price, world too high";
                price = dcAvg;
            }
            else
            {
                priceSource = $"world undercut";
                price = worldMin - 1;
            }

            var dcMinimumFraction = (float)dcMin / dcAvg;
            if (dcMinimumFraction > minFraction && dcMin < price)
            {
                priceSource = $"dc undercut";
                price = dcMin - 1;
            }

            PluginLog.Log($"Checking average - {worldMin - 1} world min against avg DC {dcAvg}, min DC {dcMin}");
            PluginLog.Log($"world min fraction {worldMinFraction}, dc min fraction {dcMinimumFraction}, min fraction {minFraction}");
        }
    }
}
