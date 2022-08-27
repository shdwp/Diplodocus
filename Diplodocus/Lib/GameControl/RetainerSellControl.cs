using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.Pricing;
using Diplodocus.Universalis;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Lib.GameControl
{
    public sealed class RetainerSellControl : IDisposable
    {
        public struct CurrentOfferings
        {
            public int  totalOfferings;
            public int  processedOfferings;
            public long minimumPriceNQ;
            public long minimumPriceHQ;

            public bool IsComplete()
            {
                return minimumPriceHQ != long.MaxValue && minimumPriceNQ != long.MaxValue;
            }

            public long GetMinimumPrice()
            {
                return Math.Min(minimumPriceHQ, minimumPriceNQ);
            }

            public long GetMinimumPrice(bool isHq)
            {
                if (isHq)
                {
                    return minimumPriceHQ != long.MaxValue ? minimumPriceHQ : 0;
                }
                else
                {
                    return Math.Min(minimumPriceHQ, minimumPriceNQ);
                }
            }
        }

        // Inject
        private readonly GameGui           _gui;
        private readonly DataManager       _dataManager;
        private readonly GameNetwork       _network;
        private readonly AtkLib            _atkLib;
        private readonly AtkControl        _atkControl;
        private readonly HIDControl        _hidControl;
        private readonly PricingLib        _pricingLib;
        private readonly UniversalisClient _universalis;
        private readonly InventoryLib      _inventoryLib;

        private CurrentOfferings        _currentOfferings;
        private DateTime                _lastPriceRequest;
        private TimeSpan                _priceRequestDelay = TimeSpan.FromMilliseconds(2250);
        private Dictionary<ulong, long> _priceCache        = new();

        private TaskCompletionSource<CurrentOfferings> _currentOfferingsTaskSource;

        public RetainerSellControl(GameGui gui, DataManager dataManager, GameNetwork network, AtkLib atkLib, InventoryLib inventoryLib, AtkControl atkControl, HIDControl hidControl, PricingLib pricingLib, UniversalisClient universalis)
        {
            _gui = gui;
            _dataManager = dataManager;
            _network = network;
            _atkLib = atkLib;
            _inventoryLib = inventoryLib;
            _atkControl = atkControl;
            _hidControl = hidControl;
            _pricingLib = pricingLib;
            _universalis = universalis;

            Resolver.Initialize();

            _network.NetworkMessage += OnNetworkEvent;
        }

        public void Dispose()
        {
            _network.NetworkMessage -= OnNetworkEvent;
        }

        public unsafe void SetAskingPrice(long value)
        {
            var comp = GetPriceInput();
            comp->SetValue((int)value);
        }

        public unsafe long GetAskingPrice()
        {
            var comp = GetPriceText();
            return long.Parse(comp->NodeText.ToString());
        }

        public void ResetPricingState()
        {
            _priceCache.Clear();
        }

        public async Task<(long, string)> CalculateAndSetSellingPrice(Item item)
        {
            if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
            {
                throw new InvalidOperationException("window not focused");
            }

            var newPrice = (long)999999;
            var newPriceSource = "invalid";
            if (_priceCache.ContainsKey(item.RowId))
            {
                newPrice = _priceCache[item.RowId];
                newPriceSource = "cch";
            }
            else
            {
                await _hidControl.CursorUp();

                await WaitForOfferingsThrottle();
                await _hidControl.CursorConfirm();
                
                var dataTask = _universalis.GetDCData(item.RowId);
                var offeringsData = await WaitForCurrentOfferings();

                if (!_atkControl.IsRetainerOfferingsWindowFocused())
                {
                    throw new InvalidOperationException("window not focused");
                }

                await _hidControl.CursorCancel();
                if (!_atkControl.IsRetainerAdjustPriceWindowFocused())
                {
                    throw new InvalidOperationException("window not focused");
                }

                await _hidControl.CursorDown();

                var data = await dataTask;

                _pricingLib.CalculateCurrentSellingPrice(
                    offeringsData.GetMinimumPrice(),
                    data,
                    out newPrice,
                    out newPriceSource);

                _priceCache[item.RowId] = newPrice;
            }

            await _hidControl.CursorDown();
            await _hidControl.CursorDown();
            SetAskingPrice(newPrice);
            await _hidControl.CursorConfirm();

            return (newPrice, newPriceSource);
        }

        public async Task WaitForOfferingsThrottle()
        {
            var delta = _priceRequestDelay - (DateTime.Now - _lastPriceRequest);
            if (delta > TimeSpan.Zero)
            {
                PluginLog.Debug($"Throttling on offerings ({DateTime.Now - _lastPriceRequest} after last), waiting {delta}");
                await Task.Delay(delta);
            }

            _lastPriceRequest = DateTime.Now;
        }

        public async Task<CurrentOfferings> WaitForCurrentOfferings()
        {
            _currentOfferingsTaskSource = new TaskCompletionSource<CurrentOfferings>();
            await Task.Delay(TimeSpan.FromMilliseconds(350));

            var timeoutTask = Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                return new CurrentOfferings();
            });

            return await await Task.WhenAny(timeoutTask, _currentOfferingsTaskSource.Task);
        }

        private unsafe void OnNetworkEvent(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction != NetworkMessageDirection.ZoneDown) return;
            if (!_dataManager.IsDataReady) return;

            if (opCode == _dataManager.ServerOpCodes["MarketBoardItemRequestStart"])
            {
                using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 16);
                stream.Seek(0xb, SeekOrigin.Begin);

                _currentOfferings = new CurrentOfferings
                {
                    totalOfferings = stream.ReadByte(),
                    minimumPriceHQ = long.MaxValue,
                    minimumPriceNQ = long.MaxValue,
                };

                PluginLog.Debug($"MarketBoardItemRequestStart (total {_currentOfferings.totalOfferings})");
            }

            if (opCode != _dataManager.ServerOpCodes["MarketBoardOfferings"]) return;

            PluginLog.Debug("MarketBoardOfferings");

            var listing = MarketBoardCurrentOfferings.Read(dataPtr);

            foreach (var item in listing.ItemListings)
            {
                if (!item.IsHq)
                {
                    _currentOfferings.minimumPriceNQ = Math.Min(item.PricePerUnit, _currentOfferings.minimumPriceNQ);
                }
                else
                {
                    _currentOfferings.minimumPriceHQ = Math.Min(item.PricePerUnit, _currentOfferings.minimumPriceHQ);
                }

                if (_currentOfferings.IsComplete())
                {
                    FinishOfferingsTaskIfNeeded();
                }

                _currentOfferings.processedOfferings++;
            }

            if (_currentOfferings.processedOfferings >= _currentOfferings.totalOfferings)
            {
                FinishOfferingsTaskIfNeeded();
            }
        }

        private void FinishOfferingsTaskIfNeeded()
        {
            if (_currentOfferingsTaskSource != null && !_currentOfferingsTaskSource.Task.IsCompleted)
            {
                _currentOfferingsTaskSource.SetResult(_currentOfferings);
            }

            // OnCurrentOfferingsReceived?.Invoke(_currentOfferings);

            var itemHq = _gui.HoveredItem > 1000000;
            var price = _currentOfferings.GetMinimumPrice(itemHq);
            ImGui.SetClipboardText("" + (price - 1));
        }

        private unsafe AtkTextNode* GetPriceText()
        {
            var unit = _atkLib.FindUnitBase("RetainerSell");
            if (unit == null)
            {
                PluginLog.Error("Failed to find RetainerSell!");
                return null;
            }

            var num = (AtkComponentNode*)unit->UldManager.NodeList[15];
            var textNode = num->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
            return textNode;
        }

        private unsafe AtkComponentNumericInput* GetPriceInput()
        {
            var unit = _atkLib.FindUnitBase("RetainerSell");
            if (unit == null)
            {
                PluginLog.Error("Failed to find RetainerSell!");
                return null;
            }

            var num = unit->UldManager.NodeList[15];
            return (AtkComponentNumericInput*)num->GetComponent();
        }
    }
}