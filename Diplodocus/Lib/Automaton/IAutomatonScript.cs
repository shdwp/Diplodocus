using System.Threading.Tasks;

namespace Diplodocus.Lib.Automaton
{
    public interface IAutomatonScript<TSettings> where TSettings: BaseAutomatonScriptSettings
    {
        Task Start(TSettings config);
        void Stop();
    }
}
