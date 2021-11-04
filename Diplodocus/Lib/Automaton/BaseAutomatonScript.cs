using System;
using System.Threading.Tasks;

namespace Diplodocus.Lib.Automaton
{
    public abstract class BaseAutomatonScript<TSettings> : IAutomatonScript<TSettings>, IDisposable where TSettings: BaseAutomatonScriptSettings
    {
        protected bool      _run;
        protected TSettings _settings;

        protected virtual bool ShouldDelayStart => false;

        public virtual async Task Start(TSettings settings)
        {
            _run = true;
            _settings = settings;

            if (!ValidateStart())
            {
                return;
            }

            if (ShouldDelayStart)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5));
            }

            if (!ValidateStart())
            {
                return;
            }

            if (_run)
            {
                await StartImpl();
            }
        }

        public void Stop()
        {
            _run = false;
        }

        public abstract void Dispose();
        public abstract Task StartImpl();

        protected virtual bool ValidateStart()
        {
            return true;
        }

        protected bool CheckIfStopped()
        {
            if (!_run)
            {
                _settings.OnScriptFailed?.Invoke("stop");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
