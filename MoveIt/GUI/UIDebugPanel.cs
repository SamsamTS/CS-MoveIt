using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MoveIt
{
    internal class DebugPanel
    {
        internal UIPanel Panel;
        private UILabel HoverLarge, HoverSmall, ToolStatus, SelectedLarge, SelectedSmall;
        private InstanceID id, lastId;

        internal DebugPanel()
        {
            _initialise();
        }

        internal void Visible(bool show)
        {
            Panel.isVisible = show;
        }

        internal void Update(InstanceID instanceId)
        {
            id = instanceId;
            Update();
        }

        internal void Update()
        {
            if (!MoveItTool.showDebugPanel)
            {
                return;
            }

            ToolStatus.text = $"{MoveItTool.instance.ToolState} (align:{MoveItTool.instance.AlignMode}.{MoveItTool.instance.AlignToolPhase})";

            SelectedLarge.text = $"Objects Selected: {Action.selection.Count}";
            ushort[] types = new ushort[8];
            foreach (Instance instance in Action.selection)
            {
                if (instance is MoveableBuilding)
                {
                    types[0]++;
                }
                else if (instance is MoveableProp)
                {
                    PropInfo info = PropManager.instance.m_props.m_buffer[instance.id.Prop].Info;
                    if (info.m_isDecal)
                    {
                        types[2]++;
                    }
                    else if (Filters.IsSurface(info))
                    {
                        types[3]++;
                    }
                    else
                    {
                        types[1]++;
                    }
                }
                else if (instance is MoveableTree)
                {
                    types[4]++;
                }
                else if (instance is MoveableProc)
                {
                    types[5]++;
                }
                else if (instance is MoveableNode)
                {
                    types[6]++;
                }
                else if (instance is MoveableSegment)
                {
                    types[7]++;
                }
                else
                {
                    throw new Exception($"Instance is invalid type (<{instance.GetType()}>)");
                }
            }
            SelectedSmall.text = $"B:{types[0]}, P:{types[1]}, D:{types[2]}, S:{types[3]}, T:{types[4]}, PO:{types[5]}, N:{types[6]}, S:{types[7]}\n ";

            // End with updating the hovered item
            if (id == null)
            {
                return;
            }
            if (id == InstanceID.Empty)
            {
                lastId = id;
                HoverLarge.textColor = new Color32(255, 255, 255, 255);
                return;
            }
            if (lastId == id)
            {
                return;
            }

            HoverLarge.textColor = new Color32(127, 217, 255, 255);
            HoverLarge.text = "";
            HoverSmall.text = "";

            if (id.Building > 0)
            {
                BuildingInfo info = BuildingManager.instance.m_buildings.m_buffer[id.Building].Info;
                HoverLarge.text = $"B:{id.Building}  {info.name}";
                HoverSmall.text = $"{info.GetType()} ({info.GetAI().GetType()})\n{info.m_class.name}\n({info.m_class.m_service}.{info.m_class.m_subService})";
            }
            else if (id.Prop > 0)
            {
                string type = "P";
                PropInfo info = PropManager.instance.m_props.m_buffer[id.Prop].Info;
                if (info.m_isDecal) type = "D";
                HoverLarge.text = $"{type}:{id.Prop}  {info.name}";
                HoverSmall.text = $"{info.GetType()}\n{info.m_class.name}";
            }
            else if (id.NetLane > 0)
            {
                IInfo info = MoveItTool.PO.GetProcObj(id.NetLane).Info;
                HoverLarge.text = $"{id.NetLane}: {info.Name}";
                HoverSmall.text = $"\n";
            }
            else if (id.Tree > 0)
            {
                TreeInfo info = TreeManager.instance.m_trees.m_buffer[id.Tree].Info;
                HoverLarge.text = $"T:{id.Tree}  {info.name}";
                HoverSmall.text = $"{info.GetType()}\n{info.m_class.name}";
            }
            else if (id.NetNode > 0)
            {
                NetInfo info = NetManager.instance.m_nodes.m_buffer[id.NetNode].Info;
                HoverLarge.text = $"N:{id.NetNode}  {info.name}";
                HoverSmall.text = $"{info.GetType()} ({info.GetAI().GetType()})\n{info.m_class.name}";
            }
            else if (id.NetSegment > 0)
            {
                NetInfo info = NetManager.instance.m_segments.m_buffer[id.NetSegment].Info;
                HoverLarge.text = $"S:{id.NetSegment}  {info.name}";
                HoverSmall.text = $"{info.GetType()} ({info.GetAI().GetType()})\n{info.m_class.name}";
            }

            lastId = id;
        }

        private void _initialise()
        {
            Panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Panel.name = "MoveIt_DebugPanel";
            Panel.atlas = ResourceLoader.GetAtlas("Ingame");
            Panel.backgroundSprite = "SubcategoriesPanel";
            Panel.size = new Vector2(300, 107);
            Panel.absolutePosition = new Vector3(Panel.GetUIView().GetScreenResolution().x - 412, 3);
            Panel.clipChildren = true;
            Panel.isVisible = MoveItTool.showDebugPanel;

            HoverLarge = Panel.AddUIComponent<UILabel>();
            HoverLarge.textScale = 0.8f;
            HoverLarge.text = "None";
            HoverLarge.relativePosition = new Vector3(6, 7);
            HoverLarge.width = HoverLarge.parent.width - 20;
            HoverLarge.clipChildren = true;
            HoverLarge.useDropShadow = true;
            HoverLarge.dropShadowOffset = new Vector2(2, -2);

            HoverSmall = Panel.AddUIComponent<UILabel>();
            HoverSmall.textScale = 0.65f;
            HoverSmall.text = "No item being hovered\n ";
            HoverSmall.relativePosition = new Vector3(5, 23);
            HoverSmall.width = HoverSmall.parent.width - 20;
            HoverSmall.clipChildren = true;
            HoverSmall.useDropShadow = true;
            HoverSmall.dropShadowOffset = new Vector2(1, -1);

            ToolStatus = Panel.AddUIComponent<UILabel>();
            ToolStatus.textScale = 0.65f;
            ToolStatus.text = "";
            ToolStatus.relativePosition = new Vector3(5, 63);
            ToolStatus.width = HoverSmall.parent.width - 20;
            ToolStatus.clipChildren = true;
            ToolStatus.useDropShadow = true;
            ToolStatus.dropShadowOffset = new Vector2(1, -1);

            SelectedLarge = Panel.AddUIComponent<UILabel>();
            SelectedLarge.textScale = 0.8f;
            SelectedLarge.text = "Objects Selected: 0";
            SelectedLarge.relativePosition = new Vector3(6, 79);
            SelectedLarge.width = SelectedLarge.parent.width - 20;
            SelectedLarge.clipChildren = true;
            SelectedLarge.useDropShadow = true;
            SelectedLarge.dropShadowOffset = new Vector2(2, -2);

            SelectedSmall = Panel.AddUIComponent<UILabel>();
            SelectedSmall.textScale = 0.65f;
            SelectedSmall.text = "B:0, P:0, D:0, S:0, T:0, PO:0, N:0, S:0\n ";
            SelectedSmall.relativePosition = new Vector3(5, 94);
            SelectedSmall.width = SelectedSmall.parent.width - 20;
            SelectedSmall.clipChildren = true;
            SelectedSmall.useDropShadow = true;
            SelectedSmall.dropShadowOffset = new Vector2(1, -1);
        }
    }
}
