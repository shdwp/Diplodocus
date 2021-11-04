using System;
using System.Text;
using ImGuiNET;

namespace Diplodocus.ScriptLib
{
    public abstract class BaseScriptUI<T> : IDisposable
    {
        protected readonly StringBuilder _log = new();

        protected T _script;

        protected BaseScriptUI(T script)
        {
            _script = script;
        }

        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {

        }

        public void DrawUI()
        {
            Draw();

            ImGui.Text("Log:");
            ImGui.TextWrapped(_log.ToString());
        }

        public abstract void Draw();

        public abstract string GetName();

        public void Dispose()
        {
            Disable();
        }
    }
}
