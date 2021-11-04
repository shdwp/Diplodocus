using Diplodocus.GameApi;

namespace Diplodocus.GameControl
{
    public sealed class AtkControl
    {
        private AtkLib _atkLib;

        public AtkControl(AtkLib atkLib)
        {
            _atkLib = atkLib;
        }

        public bool IsInventoryWindowFocused()
        {
            return _atkLib.IsFocused("InventoryExpansion");
        }

        public bool IsRetainerMarketWindowFocused()
        {
            return _atkLib.IsFocused("RetainerSellList");
        }

        public bool IsRetainerAdjustPriceWindowFocused()
        {
            return _atkLib.IsFocused("RetainerSell");
        }

        public bool IsRetainerOfferingsWindowFocused()
        {
            return _atkLib.IsFocused("ItemSearchResult");
        }
    }
}
