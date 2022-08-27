using System;
using System.Collections.Generic;
using System.Linq;
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
        private const    string     Endpoint = "https://universalis.app/api";
        private readonly HttpClient httpClient;
        private          DateTime   _lastRequestTime;

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
            return await GetMarketBoardData("Twintania", itemId);
        }

        public async Task<MarketBoardData?> GetDCData(ulong itemId)
        {
            return await GetMarketBoardData("Light", itemId);
        }

        /// <summary>
        /// Dispose client.
        /// </summary>
        public void Dispose()
        {
            httpClient.Dispose();
        }

        private async Task<MarketBoardData?> GetMarketBoardData(string worldId, ulong itemId)
        {
            var hq = false;
            if (itemId >= 1000000)
            {
                hq = true;
                itemId -= 1000000;
            }

            try
            {
                var currentData = await GetMarketBoardDataAsync(worldId, itemId);
                var historyData = await GetMarketBoardHistoryAsync(worldId, itemId);

                if (currentData == null || historyData == null)
                {
                    PluginLog.Error($"Failed to get market data: current {currentData != null}, history {historyData != null}");
                    return null;
                }

                var minPriceHq = currentData.minPriceHQ;
                var minPriceNq = currentData.minPriceNQ;

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

                double averageSoldPrice = 0;
                double averageSoldPerDay = 0;

                // PluginLog.Debug("====================");

                var listings = new List<MarketBoardData.Listing>();

                foreach (var listing in currentData.listings)
                {
                    listings.Add(new MarketBoardData.Listing
                    {
                        worldName = listing.worldName ?? "Twintania",
                        price = listing.pricePerUnit,
                        amount = listing.quantity,
                        hq = listing.hq,
                    });
                }

                var totalSold = 0;
                var totalTimespan = TimeSpan.Zero;
                var allSoldPrices = new List<double>();

                if (historyData.entries != null)
                {
                    foreach (var historyItem in historyData.entries)
                    {
                        DateTime sellingDate = DateTime.UnixEpoch + TimeSpan.FromSeconds((double)historyItem.timestamp);
                        totalTimespan = DateTime.Now - sellingDate;
                        totalSold += (int)historyItem.quantity;
                        allSoldPrices.Add((double)historyItem.pricePerUnit);

                        // PluginLog.Debug($"History item {historyItem.pricePerUnit}");
                        if (totalTimespan.TotalDays > 18)
                        {
                            if (allSoldPrices.Count > 0)
                            {
                                allSoldPrices.Sort();
                                averageSoldPrice = allSoldPrices[allSoldPrices.Count / 2];
                            }

                            averageSoldPerDay = (float)totalSold / totalTimespan.TotalDays;
                            break;
                        }
                    }
                }

                var marketBoardData = new MarketBoardData
                {
                    id = itemId,
                    lastCheckTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    lastUploadTime = currentData.lastUploadTime?.Value,

                    currentMinimumPrice = minPrice,
                    averageSoldPerDay = averageSoldPerDay,
                    averageSoldPrice = averageSoldPrice,

                    listings = listings?.ToArray(),
                    hqPercent = (float)listings.Count() / listings.Count(l => l.hq)
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

        private async Task<dynamic?> GetMarketBoardDataAsync(string worldId, ulong itemId)
        {
            var request = Endpoint + "/" + worldId + "/" + itemId;
            return await MakeRequest(new Uri(request));
        }

        private async Task<dynamic?> GetMarketBoardHistoryAsync(string worldId, ulong itemId)
        {
            var request = Endpoint + $"/v2/history/{worldId}/{itemId}";
            return await MakeRequest(new Uri(request));
        }

        private async Task<dynamic?> MakeRequest(Uri uri)
        {
            var delta = DateTime.Now - _lastRequestTime;
            var limit = TimeSpan.FromSeconds(1f / 10);
            if (delta < limit)
            {
                PluginLog.Debug($"Throttling universalis for {limit - delta}");
                await Task.Delay(limit - delta);
            }

            for (var attempt = 0; attempt < 10; attempt++)
            {
                PluginLog.Debug($"[attempt {attempt}] universalisRequest={uri}");
                try
                {
                    var response = ParseApiResponse(await this.httpClient.GetAsync(uri));
                    _lastRequestTime = DateTime.Now;
                    return response;
                }
                catch (Exception exception)
                {
                    PluginLog.Error($"Universalis request failed: {exception}");
                    _lastRequestTime = DateTime.Now;
                }
            }

            return null;
        }

        private dynamic? ParseApiResponse(HttpResponseMessage result)
        {
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Invalid response code: {result.StatusCode}");
            }

            var body = result.Content.ReadAsStringAsync().Result;
            var json = JsonConvert.DeserializeObject<dynamic>(body);
            // PluginLog.Verbose($"universalisResponseBody={json}");
            
            if (json == null)
            {
                throw new Exception($"Failed to parse Universalis response: {body}");
            }

            return json;
        }
    }
}