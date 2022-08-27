using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Diplodocus.NeuUI.InventoryTabs;
using ImGuiNET;
using Ninject;

namespace Diplodocus.NeuUI
{
    public class MainWindow : Window
    {
        private readonly List<BaseInventoryTab> _tabs = new();

        public MainWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
        {
            _tabs.Add(Module.Shared.Get<PlayerInventoryTab>());
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("##at_tabbar"))
            {
                foreach (var tab in _tabs)
                {
                }
                ImGui.EndTabBar();
            }
        }
    }
}