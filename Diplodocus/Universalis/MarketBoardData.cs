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

        public ulong id;
        public long  lastCheckTime;
        public long  lastUploadTime;

        public double? currentMinimumPrice;
        public double? averageSoldPerDay;
        public double? averageSoldPrice;

        public float hqPercent;

        public Listing[]? listings;
    }
}