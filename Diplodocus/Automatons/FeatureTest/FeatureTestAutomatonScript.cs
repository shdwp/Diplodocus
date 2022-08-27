using System.Threading.Tasks;
using Diplodocus.Lib.Automaton;

namespace Diplodocus.Automatons.FeatureTest
{
    public class FeatureTestAutomatonScript : BaseAutomatonScript<FeatureTestAutomatonScript.Settings>
    {
        public class Settings : BaseAutomatonScriptSettings
        {
        }
        
        public override void Dispose()
        {
        }
        
        public override Task StartImpl()
        {
            return Task.CompletedTask;
        }
    }
}