using System;
using System.Threading.Tasks;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameControl;

namespace Diplodocus.Automatons.RestartVentures
{
    public class RestartVenturesAutomatonScript : BaseAutomatonScript<RestartVenturesAutomatonScript.RestartVentureSettings>
    {
        public class RestartVentureSettings : BaseAutomatonScriptSettings
        {
            public int retainerFirst;
            public int retainerLast;
        }

        private readonly HIDControl      _hidControl;
        private readonly RetainerControl _retainerControl;
        private readonly AtkControl      _atkControl;

        public RestartVenturesAutomatonScript(HIDControl hidControl, RetainerControl retainerControl, AtkControl atkControl)
        {
            _hidControl = hidControl;
            _retainerControl = retainerControl;
            _atkControl = atkControl;
        }

        public override void Dispose()
        {
        }

        protected override bool ShouldDelayStart => true;

        protected override bool ValidateStart()
        {
            if (_settings.retainerLast == 1)
            {
                if (!_atkControl.IsRetainerMarketWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("retainer market window not focused");
                    return false;
                }
            }
            else
            {
                if (!_atkControl.IsRetainersWindowFocused())
                {
                    _settings.OnScriptFailed?.Invoke("retainer window not focused");
                    return false;
                }
            }

            return true;
        }


        public override async Task StartImpl()
        {
            await _hidControl.CursorLeft();

            for (var retainerIdx = _settings.retainerFirst; retainerIdx < _settings.retainerLast; retainerIdx++)
            {
                if (!_run)
                {
                    return;
                }

                if (_settings.retainerLast > 1)
                {
                    await _retainerControl.OpenRetainerMenu(retainerIdx);
                }

                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorDown();
                await _hidControl.CursorConfirm();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _hidControl.CursorConfirm();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _hidControl.CursorConfirm();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _hidControl.CursorConfirm();
                await _atkControl.WaitForWindow("SelectString");
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (!_run)
                {
                    return;
                }

                if (_settings.retainerLast > 1)
                {
                    await _retainerControl.CloseRetainerMenu();
                }
            }
        }
    }
}