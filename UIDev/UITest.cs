﻿using System;
using ImGuiNET;
using ImGuiScene;
using System.Numerics;
using UIDev.Framework;

namespace UIDev
{
    class UITest : IPluginUIMock
    {
        public static void Main(string[] args)
        {
            long price = 17000;
            long averagePrice = 16000;
            var fraction = (float)price / averagePrice;
            fraction *= 100;

            Console.WriteLine($"{(int)fraction}%");
            // UIBootstrap.Inititalize(new UITest());
        }

        private TextureWrap? goatImage;
        private SimpleImGuiScene? scene;

        public void Initialize(SimpleImGuiScene scene)
        {

            this.goatImage = scene.LoadImage("goat.png");

            scene.OnBuildUI += Draw;

            this.Visible = true;

            // saving this only so we can kill the test application by closing the window
            // (instead of just by hitting escape)
            this.scene = scene;
        }

        public void Dispose()
        {
            this.goatImage?.Dispose();
        }

        // You COULD go all out here and make your UI generic and work on interfaces etc, and then
        // mock dependencies and conceivably use exactly the same class in this testbed and the actual plugin
        // That is, however, a bit excessive in general - it could easily be done for this sample, but I
        // don't want to imply that is easy or the best way to go usually, so it's not done here either
        private void Draw()
        {
            DrawMainWindow();
            DrawSettingsWindow();

            if (!Visible)
            {
                this.scene!.ShouldQuit = true;
            }
        }

        #region Nearly a copy/paste of PluginUI
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // this is where you'd have to start mocking objects if you really want to match
        // but for simple UI creation purposes, just hardcoding values works
        private bool fakeConfigBool = true;

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text($"The random config bool is {this.fakeConfigBool}");

                if (ImGui.Button("Show Settings"))
                {
                    SettingsVisible = true;
                }

                ImGui.Spacing();

                ImGui.Text("Have a goat:");
                ImGui.Indent(55);
                ImGui.Image(this.goatImage!.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
                ImGui.Unindent(55);
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
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (ImGui.Checkbox("Random Config Bool", ref this.fakeConfigBool))
                {
                    // nothing to do in a fake ui!
                }
            }
            ImGui.End();
        }
        #endregion
    }
}
