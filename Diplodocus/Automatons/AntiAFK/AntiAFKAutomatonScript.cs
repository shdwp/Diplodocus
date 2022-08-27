using System;
using System.Threading.Tasks;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameControl;

namespace Diplodocus.Automatons.AntiAFK
{
    public sealed class AntiAFKAutomatonScript : BaseAutomatonScript<BaseAutomatonScriptSettings>
    {
        private readonly HIDControl _hidControl;

        public AntiAFKAutomatonScript(HIDControl hidControl)
        {
            _hidControl = hidControl;
        }

        public override void Dispose()
        {
        }

        public override async Task StartImpl()
        {
            _settings.OnScriptFailed?.Invoke("ENABLED");
            while (true)
            {
                if (CheckIfStopped())
                {
                    _settings.OnScriptCompleted?.Invoke();
                    return;
                }

                await _hidControl.Dummy();
                await Task.Delay(TimeSpan.FromMinutes(0.1));
            }
        }
    }
}