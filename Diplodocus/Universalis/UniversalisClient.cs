using System;
using System.Collections.Generic;
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
    public class UniversalisClient
    {
        private const    string     Endpoint = "https://universalis.app/api/";
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
        public async Task<MarketBoardData?> GetWorldData(ulong itemId)
        {
            return await this.GetMarketBoardData("Twintania", itemId);
        }

        public async Task<MarketBoardData?> GetDCData(ulong itemId)
        {
            return await this.GetMarketBoardData("Light", itemId);
        }

        /// <summary>
        /// Dispose client.
        /// </summary>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private async Task<MarketBoardData?> GetMarketBoardData(string worldId, ulong itemId)
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

                double? minPrice = null;
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

                double averagePrice = 0;
                double averageSoldPerDay = 0;
                var lastSell = DateTime.Now;

                // PluginLog.Debug("====================");

                var listings = new List<MarketBoardData.Listing>();

                foreach (var listing in json.listings)
                {
                    listings.Add(new MarketBoardData.Listing
                    {
                        worldName = listing.worldName ?? "Twintania",
                        price = listing.pricePerUnit,
                        amount = listing.quantity,
                        hq = listing.hq,
                    });
                }

                foreach (var historyItem in json.recentHistory)
                {
                    DateTime sellingDate = DateTime.UnixEpoch + TimeSpan.FromSeconds((double)historyItem.timestamp);

                    var age = lastSell - sellingDate;

                    /*
                    PluginLog.Debug("Selling date " + sellingDate);
                    PluginLog.Debug("Age on previous " + age + ", days " + age.TotalDays);
                    PluginLog.Debug("Per day + " + (1 / age.TotalDays));
                    */

                    if (age == TimeSpan.Zero)
                    {
                        averageSoldPerDay += 1;
                    }
                    else
                    {
                        averageSoldPerDay += 1 / age.TotalDays;
                    }

                    if (historyItem.hq == hq)
                    {
                        // PluginLog.Debug("Adding to average " + historyItem.pricePerUnit);
                        averagePrice = (averagePrice + (double)historyItem.pricePerUnit) / 2;
                    }
                }

                var marketBoardData = new MarketBoardData
                {
                    lastCheckTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    lastUploadTime = json.lastUploadTime?.Value,

                    minimumPrice = minPrice,
                    averageMinimumPrice = averagePrice,
                    averageSoldPerDay = averageSoldPerDay,
                    averagePrice = hq ? json.averagePriceHQ : json.averagePriceNQ,
                    averagePriceHQ = json.averagePriceHQ,
                    averagePriceNQ = json.averagePriceNQ,

                    listings = listings?.ToArray(),
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

        private async Task<HttpResponseMessage> GetMarketBoardDataAsync(string worldId, ulong itemId)
        {
            var request = Endpoint + "/" + worldId + "/" + itemId;
            PluginLog.Debug($"universalisRequest={request}");
            return await this.httpClient.GetAsync(new Uri(request));
        }
    }
}
