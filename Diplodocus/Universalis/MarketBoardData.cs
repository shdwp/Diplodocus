namespace Diplodocus.Universalis
{
    public class MarketBoardData
    {
        public struct Listing
        {
            public double price;
            public int    amount;
            public string worldName;
            public bool   hq;
        }

        public long lastCheckTime;
        public long lastUploadTime;

        public double? minimumPrice;
        public double? averageMinimumPrice;
        public double? averageSoldPerDay;

        public double? averagePrice;
        public double? averagePriceHQ;
        public double? averagePriceNQ;

        public Listing[]? listings;
    }
}
