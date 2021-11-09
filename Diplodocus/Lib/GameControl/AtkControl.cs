using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi;

namespace Diplodocus.Lib.GameControl
{
    public sealed class AtkControl : IDisposable
    {
        private readonly AtkLib _atkLib;

        public static string Inventory        = "InventoryExpansion";
        public static string CraftingLog      = "RecipeNote";
        public static string Synthesis        = "Synthesis";
        public static string RetainerSellList = "RetainerSellList";
        public static string RetainerSell     = "RetainerSell";
        public static string MarketBoard = "ItemSearch";

        public AtkControl(AtkLib atkLib)
        {
            _atkLib = atkLib;
        }

        public void Dispose()
        {
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

        public bool IsWindowFocused(string window)
        {
            return _atkLib.IsFocused(window);
        }

        public Task<bool> WaitForWindow(string window)
        {
            return WaitForWindow(window, TimeSpan.FromSeconds(3));
        }

        public Task<bool> WaitForWindow(string window, TimeSpan timeout)
        {
            var token = new CancellationToken();
            var dt = DateTime.Now;

            PluginLog.Debug($"Wait for {window} started...");
            var task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsWindowFocused(window))
                    {
                        PluginLog.Debug($"Wait for {window} success");
                        return true;
                    }

                    if (DateTime.Now - dt > timeout)
                    {
                        PluginLog.Debug($"Wait for {window} timeout");
                        return false;
                    }

                    Thread.Sleep(96);
                }
            }, token);

            return task;
        }
    }
}
