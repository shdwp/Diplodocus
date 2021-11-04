using Diplodocus.Lib.Automaton;
using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Automatons.Undercut
{
    public sealed class UndercutAutomaton : BaseAutomaton<UndercutAutomatonScript, UndercutAutomatonScript.UndercutSettings>
    {
        public UndercutAutomaton(UndercutAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
        }

        public override string GetName()
        {
            return "Undercut";
        }

        public override UndercutAutomatonScript.UndercutSettings GetSettings()
        {
            return new UndercutAutomatonScript.UndercutSettings
            {
                OnPriceUpdated = OnPriceUpdated,
            };
        }

        private void OnPriceUpdated(Item arg1, long arg2)
        {
            _log.Append($"{arg1.Name} price updated to {arg2}.\n");
        }
    }
}
