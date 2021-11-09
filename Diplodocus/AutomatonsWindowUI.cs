using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin;
using Diplodocus.Lib.Automaton;
using ImGuiNET;

namespace Diplodocus
{
    public class AutomatonsWindowUI : IDisposable
    {
        private struct Tab
        {
            public string name;
            public Action onDraw;
        }

        private readonly Configuration          _configuration;
        private readonly DalamudPluginInterface _pluginInterface;

        private bool _visible = false;
        public bool Visible
        {
            get { return this._visible; }
            set { this._visible = value; }
        }

        private bool _settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this._settingsVisible; }
            set { this._settingsVisible = value; }
        }

        private List<Tab> _tabs = new();

        public AutomatonsWindowUI(Configuration configuration, DalamudPluginInterface pluginInterface)
        {
            _configuration = configuration;
            _pluginInterface = pluginInterface;

            _pluginInterface.UiBuilder.Draw += Draw;
            _pluginInterface.UiBuilder.OpenConfigUi += OpenConfiguration;
        }

        public void Dispose()
        {
            _tabs.Clear();

            _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfiguration;
            _pluginInterface.UiBuilder.Draw -= Draw;
        }

        public void AddComponent<T>(T ui) where T: IAutomaton
        {
            _tabs.Add(new Tab
            {
                name = ui.GetName(),
                onDraw = ui.DrawUI,
            });
        }

        public void Draw()
        {
            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window", ref this._visible))
            {
                if (ImGui.BeginTabBar("##at_tabbar"))
                {
                    foreach (var tab in _tabs)
                    {
                        if (ImGui.BeginTabItem(tab.name))
                        {
                            tab.onDraw.Invoke();
                            ImGui.EndTabItem();
                        }
                    }
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this._settingsVisible,
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
            }
            ImGui.End();
        }

        private void OpenConfiguration()
        {
            _settingsVisible = true;
        }
    }
}
