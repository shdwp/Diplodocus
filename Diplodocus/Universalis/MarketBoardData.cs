namespace Diplodocus.Universalis
{
    public class MarketBoardData
    {
        public struct Listing
        {
            public double price;
            public int    amount;
            public string worldName;
        }

        public long lastCheckTime;
        public long lastUploadTime;

        public double? minimumPrice;
        public double? averagePrice;
        public double? averageSoldPerDay;

        public Listing[]? listings;
    }
}
