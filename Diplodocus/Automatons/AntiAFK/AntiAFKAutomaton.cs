using Diplodocus.Lib.Automaton;

namespace Diplodocus.Automatons.AntiAFK
{
    public sealed class AntiAFKAutomaton : BaseAutomaton<AntiAFKAutomatonScript, BaseAutomatonScriptSettings>
    {
        public AntiAFKAutomaton(AntiAFKAutomatonScript script) : base(script)
        {
        }

        public override void Draw()
        {
        }

        public override string GetName()
        {
            return "AntiAFK";
        }

        public override BaseAutomatonScriptSettings GetSettings()
        {
            return new BaseAutomatonScriptSettings();
        }
    }
}