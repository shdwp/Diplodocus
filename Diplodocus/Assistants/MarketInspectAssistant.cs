using System.Linq;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Diplodocus.Lib.Assistant;
using Diplodocus.Lib.GameApi;
using Diplodocus.Lib.GameApi.Inventory;
using Diplodocus.Lib.Pricing;
using Diplodocus.Universalis;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Assistants
{
    public sealed class MarketInspectAssistant : IAssistant
    {
        private readonly InventoryLib      _inventoryLib;
        private readonly UniversalisClient _universalis;
        private readonly GameGui           _gameGui;
        private readonly ChatGui           _chatGui;
        private readonly KeyState          _keyState;
        private readonly Framework         _framework;

        private ulong _lastItemChecked;
        private bool  _lastItemCheckCrossworld;
        private int   _lastKeyState;

        public MarketInspectAssistant(UniversalisClient universalis, GameGui gameGui, InventoryLib inventoryLib, ChatGui chatGui, DataManager dataManager, KeyState keyState, Framework framework)
        {
            _universalis = universalis;
            _gameGui = gameGui;
            _inventoryLib = inventoryLib;
            _chatGui = chatGui;
            _keyState = keyState;
            _framework = framework;

            _gameGui.HoveredItemChanged += OnHoveredItemChanged;
            _framework.Update += OnUpdate;
        }

        public void Dispose()
        {
            _gameGui.HoveredItemChanged -= OnHoveredItemChanged;
            _framework.Update -= OnUpdate;
        }

        private void OnUpdate(Framework framework)
        {
            var keyState = (_keyState.GetRawValue(VirtualKey.SHIFT) << 4) + _keyState.GetRawValue(VirtualKey.CONTROL);
            if (_lastKeyState != keyState)
            {
                PerformHoveredItemCheck();
                _lastKeyState = keyState;
            }
        }

        private void OnHoveredItemChanged(object? sender, ulong e)
        {
            PerformHoveredItemCheck();
        }

        private void PerformHoveredItemCheck()
        {
            if (_keyState.GetRawValue(VirtualKey.SHIFT) == 0)
            {
                return;
            }

            var crossworld = _keyState.GetRawValue(VirtualKey.CONTROL) > 0;
            var listings = _keyState[VirtualKey.MENU] || crossworld;

            if (_gameGui.HoveredItem == 0)
            {
                return;
            }

            if (_gameGui.HoveredItem == _lastItemChecked && _lastItemCheckCrossworld == crossworld)
            {
                return;
            }

            var type = _inventoryLib.GetItemType(_gameGui.HoveredItem);
            if (type.IsUntradable)
            {
                return;
            }

            _lastItemChecked = _gameGui.HoveredItem;
            _lastItemCheckCrossworld = crossworld;
            var task = crossworld ? _universalis.GetDCData(_gameGui.HoveredItem) : _universalis.GetWorldData(_gameGui.HoveredItem);
            var _ = EchoMarketData(task, crossworld, listings, type);
        }

        private async Task EchoMarketData(Task<MarketBoardData?> dataTask, bool crossworld, bool listings, Item type)
        {
            InventoryLib.ParseItemId(_gameGui.HoveredItem, out var id, out var hq);

            var data = await dataTask;

            var msg = new SeString();
            if (crossworld)
            {
                msg.Append(new IconPayload(BitmapFontIcon.CrossWorld));
            }
            else
            {
                msg.Append(new IconPayload(BitmapFontIcon.PriorityWorld));
            }

            msg.Append(new ItemPayload(id, hq));
            msg.Append(new UIForegroundPayload(GameColors.Green));
            msg.Append(new TextPayload(type.Name));
            if (hq)
            {
                msg.Append(new TextPayload("" + (char)SeIconChar.HighQuality));
            }
            msg.Append(UIForegroundPayload.UIForegroundOff);
            msg.Append(RawPayload.LinkTerminator);

            msg.Append(new TextPayload(" min "));
            msg.Append(new UIGlowPayload(GameColors.Red));
            msg.Append(new UIForegroundPayload(GameColors.Red));
            msg.Append(new TextPayload(PricingLib.FormatPrice(data.currentMinimumPrice.Value) + (char)SeIconChar.Gil));
            msg.Append(UIForegroundPayload.UIForegroundOff);
            msg.Append(UIGlowPayload.UIGlowOff);

            if (crossworld)
            {
                msg.Append(new TextPayload(" (" + data.listings.First().worldName + ")"));
            }

            msg.Append(new TextPayload(", avgh "));
            msg.Append(new UIGlowPayload(GameColors.Orange));
            msg.Append(new TextPayload($"{PricingLib.FormatPrice(data.averageSoldPrice.Value)}{(char)SeIconChar.Gil}"));
            msg.Append(UIGlowPayload.UIGlowOff);

            msg.Append(new TextPayload(", spd "));
            msg.Append(new UIGlowPayload(GameColors.Blue));
            msg.Append(new TextPayload($"{data.averageSoldPerDay:F1}{(char)SeIconChar.Hexagon}"));
            msg.Append(UIGlowPayload.UIGlowOff);

            if (listings)
            {
                foreach (var listing in data.listings.Take(5))
                {
                    msg.Append(new NewLinePayload());
                    msg.Append(new UIForegroundPayload(GameColors.Red));
                    msg.Append(new TextPayload(PricingLib.FormatPrice(listing.price) + (char)SeIconChar.Gil));
                    msg.Append(UIForegroundPayload.UIForegroundOff);
                    var hqStr = listing.hq ? (char)SeIconChar.HighQuality : ' ';
                    msg.Append(new TextPayload($"       {hqStr}{listing.amount}       "));

                    if (crossworld)
                    {
                        if (listing.worldName != "Twintania")
                        {
                            msg.Append(new IconPayload(BitmapFontIcon.CrossWorld));
                        }
                        else
                        {
                            msg.Append(new IconPayload(BitmapFontIcon.PriorityWorld));
                        }

                        msg.Append(new TextPayload($"{listing.worldName}"));
                    }
                }
            }

            _chatGui.Print(msg);
        }
    }
}