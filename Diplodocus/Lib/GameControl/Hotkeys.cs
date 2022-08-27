using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;

namespace Diplodocus.Lib.GameControl
{
    public class Hotkeys
    {
        private readonly KeyState                     _keyState;
        private readonly Dictionary<VirtualKey, bool> _keysHeld = new();

        public Hotkeys(KeyState keyState)
        {
            _keyState = keyState;
        }

        public bool HotkeyReleased(params VirtualKey[] vks)
        {
            var modifiersHeld = true;
            var key = vks.Last();
            foreach (var mod in vks.Take(vks.Length - 1))
            {
                if (!_keyState[mod])
                {
                    modifiersHeld = false;
                }
            }

            bool keyReleased = _keysHeld.GetValueOrDefault(key) && !_keyState[key];
            _keysHeld[key] = _keyState[key];
            return modifiersHeld && keyReleased;
        }
    }
}