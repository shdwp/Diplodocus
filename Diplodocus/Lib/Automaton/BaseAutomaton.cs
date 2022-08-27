using System;
using System.Text;
using ImGuiNET;

namespace Diplodocus.Lib.Automaton
{
    public abstract class BaseAutomaton<TScript, TSettings> : IDisposable, IAutomaton
        where TSettings : BaseAutomatonScriptSettings
        where TScript : IAutomatonScript<TSettings>
    {
        private readonly StringBuilder _log = new();

        protected TScript script;

        protected BaseAutomaton(TScript script)
        {
            this.script = script;
        }

        public void Dispose()
        {
        }

        public void DrawUI()
        {
            Draw();

            if (ImGui.Button("Start##" + GetName()))
            {
                var settings = GetSettings();
                settings.OnScriptCompleted ??= OnScriptCompleted;
                settings.OnScriptFailed ??= OnScriptFailed;

                _log.Clear();
                script.Start(settings);
            }

            ImGui.SameLine();
            if (ImGui.Button("Stop##" + GetName()))
            {
                script.Stop();
            }

            ImGui.Text("Log:");
            ImGui.TextWrapped(_log.ToString());
        }

        protected void Log(string str)
        {
            var time = DateTime.Now.ToString("hh:mm:ss");
            _log.Insert(0, $"{time} {str}\n");
        }

        private void OnScriptFailed(string obj)
        {
            _log.AppendFormat("Failed: {0}.\n", obj);
        }

        private void OnScriptCompleted()
        {
            _log.Append("Completed.\n");
        }

        public abstract void      Draw();
        public abstract string    GetName();
        public abstract TSettings GetSettings();
    }
}