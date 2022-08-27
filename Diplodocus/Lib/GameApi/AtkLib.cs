using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Logging;
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

        public AtkResNode* FindNode(AtkResNode* root, IEnumerable<int> path)
        {
            return InternalFindNodeChild(root, path);
        }

        private AtkResNode* InternalFindNodeChild(AtkResNode* root, IEnumerable<int> path)
        {
            PluginLog.Debug($"Testing {root->NodeID} {(ulong)root->ChildNode}, looking for {path.First()}");
            if (root->ChildNode != null)
            {
                var t = Test(root->ChildNode, path);
                if (t == 1)
                {
                    return root->ChildNode;
                }

                if (t == 2)
                {
                    var x = InternalFindNodeChild(root->ChildNode, path.Skip(1));
                    if (x != null)
                    {
                        return x;
                    }
                }

                var y = InternalFindNodeChild(root->ChildNode, path);
                if (y != null)
                {
                    return y;
                }
            }

            if (root->GetComponent() != null)
            {
                for (var i = 0; i < root->GetComponent()->UldManager.NodeListCount; i++)
                {
                    var n = root->GetComponent()->UldManager.NodeList[i];
                    var t = Test(n, path);
                    if (t == 1)
                    {
                        return n;
                    }

                    if (t == 2)
                    {
                        var x = InternalFindNodeChild(n, path.Skip(1));
                        if (x != null)
                        {
                            return x;
                        }
                    }
                }
            }

            var node = root;
            while ((node = node->NextSiblingNode) != null)
            {
                PluginLog.Debug($"Next {node->NodeID}");
                var t = Test(node, path);
                if (t == 1)
                {
                    return node;
                }

                if (t == 2)
                {
                    return InternalFindNodeChild(node, path.Skip(1));
                }
            }

            node = root;
            while ((node = node->PrevSiblingNode) != null)
            {
                PluginLog.Debug($"Prev {node->NodeID}");
                var t = Test(node, path);
                if (t == 1)
                {
                    return node;
                }

                if (t == 2)
                {
                    return InternalFindNodeChild(node, path.Skip(1));
                }
            }

            return null;
        }

        private int Test(AtkResNode* node, IEnumerable<int> path)
        {
            if (node->NodeID == path.First())
            {
                return path.Count() == 1 ? 1 : 2;
            }
            else
            {
                return 0;
            }
        }
    }
}