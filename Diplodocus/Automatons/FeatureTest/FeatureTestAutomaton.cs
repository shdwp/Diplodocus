using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Plugin;
using Diplodocus.Lib.Automaton;
using Diplodocus.Lib.GameApi;
using ImGuiNET;

namespace Diplodocus.Automatons.FeatureTest
{
    public unsafe class FeatureTestAutomaton : BaseAutomaton<FeatureTestAutomatonScript, FeatureTestAutomatonScript.Settings>
    {
        private DalamudPluginInterface _pluginInterface;
        private AtkLib                 _atkLib;

        private IntPtr  _texture = IntPtr.Zero;
        private Vector2 _textureSize;

        public FeatureTestAutomaton(FeatureTestAutomatonScript script, DalamudPluginInterface pluginInterface, AtkLib atkLib) : base(script)
        {
            _pluginInterface = pluginInterface;
            _atkLib = atkLib;
        }

        public override void Draw()
        {
            if (ImGui.Button("Screenshot"))
            {
                var unitBase = _atkLib.FindUnitBase("MiragePrismMiragePlate");
                Log($"x {unitBase->X} y {unitBase->Y} scale {unitBase->Scale}");

                var previewNode = _atkLib.FindNode(unitBase->RootNode, new[] { 32, 101, 109, 4 });

                using var bitmap = new Bitmap(previewNode->Width, previewNode->Height);
                var channels = 4;
                using var g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

                var byteArray = new byte[bitmap.Width * bitmap.Height * channels];
                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        var offset = x * channels + (y * bitmap.Width * channels);
                        var pixel = bitmap.GetPixel(x, y);
                        byteArray[offset + 0] = pixel.R;
                        byteArray[offset + 1] = pixel.G;
                        byteArray[offset + 2] = pixel.B;
                        byteArray[offset + 3] = pixel.A;
                    }
                }

                // _texture = _pluginInterface.UiBuilder.LoadImageRaw(byteArray, bitmap.Width, bitmap.Height, channels);
            }

            if (_texture != IntPtr.Zero)
            {
                ImGui.Image(_texture, _textureSize);
            }
        }

        public override string GetName()
        {
            return "Feature Test";
        }

        public override FeatureTestAutomatonScript.Settings GetSettings()
        {
            return new FeatureTestAutomatonScript.Settings();
        }
    }
}