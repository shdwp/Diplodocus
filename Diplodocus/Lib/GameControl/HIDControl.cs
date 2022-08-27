using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using Diplodocus.Lib.GameApi;

namespace Diplodocus.Lib.GameControl
{
    public sealed class HIDControl : IDisposable
    {
        public void Dispose()
        {
        }

        public async Task CursorUp()
        {
            await Keypress(117);
        }

        public async Task CursorDown()
        {
            await Keypress(118);
        }

        public async Task CursorLeft()
        {
            await Keypress(119);
        }

        public async Task CursorRight()
        {
            await Keypress(120);
        }

        public async Task CursorConfirm()
        {
            await Keypress(69);
        }

        public async Task CursorCancel()
        {
            await Keypress(8);
        }

        public async Task ToggleInventory()
        {
            await Keypress(71);
        }

        public async Task Dummy()
        {
            await Keypress((int)(VirtualKey.F4));
        }

        public async Task Keypress(int code)
        {
            Game.SendKey(code);
            await Task.Delay(TimeSpan.FromMilliseconds(140));
        }
    }
}