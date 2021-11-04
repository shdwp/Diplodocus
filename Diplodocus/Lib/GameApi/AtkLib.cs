using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Diplodocus.Lib.GameApi
{
    public sealed unsafe class AtkLib : IDisposable
    {
        private AtkStage* _stage;

        public AtkLib()
        {
            _stage = AtkStage.GetSingleton();
        }

        public void Dispose()
        {

        }

        public bool IsFocused(string name)
        {
            var unit = FindUnitBase(name);
            if (unit == null)
            {
                return false;
            }
            else
            {
                return IsFocused(unit);
            }
        }

        public bool IsFocused(AtkUnitBase* unit)
        {
            for (var i = 0; i < unit->UldManager.NodeListCount; i++)
            {
                var childNode = unit->UldManager.NodeList[i];
                if ((int)childNode->Type < 1000)
                {
                    continue;
                }

                var componentNode = (AtkComponentNode*)childNode;
                var info = (AtkUldComponentInfo*)componentNode->Component->UldManager.Objects;
                if (info->ComponentType != ComponentType.Window)
                {
                    continue;
                }

                for (var n = 0; n < componentNode->Component->UldManager.NodeListCount; n++)
                {
                    var child = componentNode->Component->UldManager.NodeList[n];
                    if (child->Type != NodeType.NineGrid)
                    {
                        continue;
                    }

                    var comp = (AtkNineGridNode*)child;
                    var part = comp->PartsList->Parts[0];
                    var texFileNamePtr = part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
                    var texString = Marshal.PtrToStringAnsi(new IntPtr(texFileNamePtr.BufferPtr));
                    if (!texString.EndsWith("BgSelected_Corner_hr1.tex"))
                    {
                        continue;
                    }

                    return child->IsVisible;
                }
            }

            return false;
        }

        public AtkUnitBase* FindUnitBase(string unitName)
        {
            var unitManagers = &_stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
            for (var i = 0; i < 18; i++)
            {
                var manager = &unitManagers[i];
                var unitBaseArray = &(manager->AtkUnitEntries);

                for (var n = 0; n < manager->Count; n++)
                {
                    var unitBase = unitBaseArray[n];
                    var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));

                    if (name == unitName)
                    {
                        return unitBase;
                    }
                }
            }

            return null;
        }
    }
}
