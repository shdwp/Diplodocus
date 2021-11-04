using System.Threading.Tasks;

namespace Diplodocus.Universalis
{
    public interface IMarketboardClient
    {

        /// <summary>
        /// Get market board data.
        /// </summary>
        /// <param name="worldId">world id.</param>
        /// <param name="itemId">item id.</param>
        /// <returns>market board data.</returns>
        Task<MarketBoardData?> GetMarketBoard(uint worldId, ulong itemId);
    }
}
