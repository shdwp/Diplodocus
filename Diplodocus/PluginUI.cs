using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Diplodocus
{
    public class PluginUI : IDisposable
    {
        private struct Tab
        {
            public string name;
            public Action onDraw;
        }

        private Configuration configuration;

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

        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
            _tabs.Clear();
        }

        public void AddTab(string name, Action onDraw)
        {
            _tabs.Add(new Tab
            {
                name = name,
                onDraw = onDraw,
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
                if (ImGui.BeginTabBar("tabbar"))
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
    }
}
