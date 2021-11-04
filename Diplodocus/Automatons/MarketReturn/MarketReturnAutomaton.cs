using Diplodocus.Lib.Automaton;

namespace Diplodocus.Automatons.MarketReturn
{
    public sealed class MarketReturnAutomaton : BaseAutomaton<MarketReturnAutomatonScript, MarketReturnAutomatonScript.Settings>
    {
        public MarketReturnAutomaton(MarketReturnAutomatonScript script) : base(script)
        {
            base.script = script;
        }

        public override void Draw()
        {
        }

        public override string GetName()
        {
            return "Market Return";
        }

        public override MarketReturnAutomatonScript.Settings GetSettings()
        {
            return new MarketReturnAutomatonScript.Settings();
        }
    }
}
