using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SigScanner = Dalamud.Game.SigScanner;

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

            public long GetMinimumPrice(bool isHq)
            {
                if (isHq)
                {
                    return minimumPriceHQ != long.MaxValue ? minimumPriceHQ : 0;
                }
                else
                {
                    return minimumPriceNQ != long.MaxValue ? minimumPriceNQ : 0;
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetFilePointer(byte index);

        public int MarketItemCount => _inventoryLib.EnumerateInventory(InventoryType.RetainerMarket).Count();

        public event Action<bool>             OnAtRetainerChanged;
        public event Action<CurrentOfferings> OnCurrentOfferingsReceived;

        // Inject
        private readonly GameGui      _gui;
        private readonly DataManager  _dataManager;
        private readonly GameNetwork  _network;
        private readonly ChatGui      _chat;
        private readonly SigScanner   _sigScanner;
        private readonly Framework    _framework;
        private readonly AtkLib       _lib;
        private readonly InventoryLib _inventoryLib;

        private CurrentOfferings _currentOfferings;
        private DateTime         _lastPriceRequest;
        private TimeSpan         _priceRequestDelay = TimeSpan.FromMilliseconds(2250);
        private DateTime         _lastRetainerCheck;
        private bool             _lastRetainerValue;
        private GetFilePointer   _getFilePtr;

        private TaskCompletionSource<CurrentOfferings> _currentOfferingsTaskSource;

        public RetainerSellControl(GameGui gui, DataManager dataManager, GameNetwork network, ChatGui chat, SigScanner sigScanner, Framework framework, AtkLib lib, InventoryLib inventoryLib)
        {
            _gui = gui;
            _dataManager = dataManager;
            _network = network;
            _chat = chat;
            _sigScanner = sigScanner;
            _framework = framework;
            _lib = lib;
            _inventoryLib = inventoryLib;

            Resolver.Initialize();

            _network.NetworkMessage += OnNetworkEvent;
            _chat.ChatMessage += OnChatMessage;
            _framework.Update += FrameworkOnUpdate;

            try
            {
                var ptr = _sigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
                _getFilePtr = Marshal.GetDelegateForFunctionPointer<GetFilePointer>(ptr);
            }
            catch (Exception e)
            {
                _getFilePtr = null;
                PluginLog.LogError(e.ToString());
            }
        }

        public void Dispose()
        {
            _framework.Update -= FrameworkOnUpdate;
            _network.NetworkMessage -= OnNetworkEvent;
            _chat.ChatMessage -= OnChatMessage;
        }

        public unsafe void SetAskingPrice(long value)
        {
            var comp = GetPriceInput();
            comp->SetValue((int)value);
        }

        public unsafe long GetAskingPrice()
        {
            var comp = GetPriceInput();
            return long.Parse(comp->AtkTextNode->NodeText.ToString());
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

            return await _currentOfferingsTaskSource.Task;
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            var msg = message.TextValue;
            // PluginLog.Verbose(msg);
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (DateTime.Now - _lastRetainerCheck > TimeSpan.FromSeconds(2.5))
            {
                _lastRetainerCheck = DateTime.Now;

                var atRetainer = (_getFilePtr != null) && Marshal.ReadInt64(_getFilePtr(7), 0xB0) != 0;
                if (atRetainer != _lastRetainerValue)
                {
                    OnAtRetainerChanged?.Invoke(atRetainer);
                    _lastRetainerValue = atRetainer;
                }
            }
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
            }

            if (opCode != _dataManager.ServerOpCodes["MarketBoardOfferings"]) return;

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

            OnCurrentOfferingsReceived?.Invoke(_currentOfferings);

            var itemHq = _gui.HoveredItem > 1000000;
            var price = _currentOfferings.GetMinimumPrice(itemHq);
            ImGui.SetClipboardText("" + price);
        }

        private unsafe AtkComponentNumericInput* GetPriceInput()
        {
            var unit = _lib.FindUnitBase("RetainerSell");
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
