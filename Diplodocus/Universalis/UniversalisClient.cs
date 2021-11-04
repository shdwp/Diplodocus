using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace Diplodocus.Universalis
{
    /// <summary>
    /// Universalis client.
    /// </summary>
    public class UniversalisClient : IMarketboardClient
    {
        private const string Endpoint = "https://universalis.app/api/";
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalisClient"/> class.
        /// </summary>
        /// <param name="plugin">price check plugin.</param>
        public UniversalisClient()
        {
            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(5000),
            };
        }

        /// <summary>
        /// Get market board data.
        /// </summary>
        /// <param name="worldId">world id.</param>
        /// <param name="itemId">item id.</param>
        /// <returns>market board data.</returns>
        public async Task<MarketBoardData?> GetMarketBoard(uint worldId, ulong itemId) {
            return await this.GetMarketBoardData(worldId, itemId);
        }

        /// <summary>
        /// Dispose client.
        /// </summary>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private async Task<MarketBoardData?> GetMarketBoardData(uint worldId, ulong itemId)
        {
            var hq = false;
            if (itemId >= 1000000)
            {
                hq = true;
                itemId -= 1000000;
            }


            HttpResponseMessage result;
            try
            {
                result = await this.GetMarketBoardDataAsync(worldId, itemId);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to retrieve data from Universalis for itemId {itemId} / worldId {worldId}.");

                return null;
            }

            PluginLog.Verbose($"universalisResponse={result}");

            if (result.StatusCode != HttpStatusCode.OK)
            {
                PluginLog.Error($"Failed to retrieve data from Universalis for itemId {itemId} / worldId {worldId} with HttpStatusCode {result.StatusCode}.");
                return null;
            }

            var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
            PluginLog.Verbose($"universalisResponseBody={json}");
            if (json == null)
            {
                PluginLog.Error($"Failed to deserialize Universalis response for itemId {itemId} / worldId {worldId}.");
                return null;
            }

            try
            {
                var minPriceHq = json.minPriceHQ;
                var minPriceNq = json.minPriceNQ;

                double minPrice = 0;
                if (hq)
                {
                    minPrice = minPriceHq;
                }
                else
                {
                    if (minPriceNq != 0 && minPriceHq != 0)
                    {
                        minPrice = Math.Min((double)minPriceHq, (double)minPriceNq);
                    }
                    else
                    {
                        minPrice = minPriceNq;
                    }
                }

                var marketBoardData = new MarketBoardData
                {
                    LastCheckTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastUploadTime = json.lastUploadTime?.Value,
                    AveragePriceNQ = json.averagePriceNQ?.Value,
                    AveragePriceHQ = json.averagePriceHQ?.Value,
                    CurrentAveragePriceNQ = json.currentAveragePriceNQ?.Value,
                    CurrentAveragePriceHQ = json.currentAveragePriceHQ?.Value,
                    MinimumPriceNQ = json.minPriceNQ?.Value,
                    MinimumPriceHQ = json.minPriceHQ?.Value,
                    MaximumPriceNQ = json.maxPriceNQ?.Value,
                    MaximumPriceHQ = json.maxPriceHQ?.Value,
                    SaleVelocityNQ = json.nqSaleVelocity?.Value,
                    SaleVelocityHQ = json.hqSaleVelocity?.Value,
                    CurrentMinimumPrice = minPrice,
                };

                PluginLog.Verbose($"marketBoardData={JsonConvert.SerializeObject(marketBoardData)}");
                return marketBoardData;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to parse marketBoard data for itemId {itemId} / worldId {worldId}: {ex}");
                return null;
            }
        }

        private async Task<HttpResponseMessage> GetMarketBoardDataAsync(uint worldId, ulong itemId)
        {
            var request = Endpoint + "/" + "twintania" + "/" + itemId;
            PluginLog.Debug($"universalisRequest={request}");
            return await this.httpClient.GetAsync(new Uri(request));
        }
    }
}
