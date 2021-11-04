using System;

namespace Diplodocus.Lib.Automaton
{
    public class BaseAutomatonScriptSettings
    {
        public Action         OnScriptCompleted;
        public Action<string> OnScriptFailed;
    }
}
