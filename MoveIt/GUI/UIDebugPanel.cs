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
        internal DebugPanel_ModTools ModTools = null;
        private UILabel HoverLarge, HoverSmall, ToolStatus, SelectedLarge, SelectedSmall;
        private InstanceID id, lastId;

        internal DebugPanel()
        {
            _initialise();

            if (isModToolsEnabled())
            {
                ModTools = new DebugPanel_ModTools(Panel);
            }
        }

        internal void Visible(bool show)
        {
            Panel.isVisible = show;
        }

        internal void UpdateVisible()
        {
            if (MoveItTool.showDebugPanel)
            {
                Panel.isVisible = true;
            }
            else
            {
                Panel.isVisible = false;
            }
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
                if (isModToolsEnabled()) ModTools.Id = id;
            }
            else if (id.Prop > 0)
            {
                string type = "P";
                PropInfo info = PropManager.instance.m_props.m_buffer[id.Prop].Info;
                if (info.m_isDecal) type = "D";
                HoverLarge.text = $"{type}:{id.Prop}  {info.name}";
                HoverSmall.text = $"{info.GetType()}\n{info.m_class.name}";
                if (isModToolsEnabled()) ModTools.Id = id;
            }
            else if (id.NetLane > 0)
            {
                IInfo info = MoveItTool.PO.GetProcObj(id.NetLane).Info;
                HoverLarge.text = $"{id.NetLane}: {info.Name}";
                HoverSmall.text = $"\n";
                if (isModToolsEnabled()) ModTools.Id = id;
            }
            else if (id.Tree > 0)
            {
                TreeInfo info = TreeManager.instance.m_trees.m_buffer[id.Tree].Info;
                HoverLarge.text = $"T:{id.Tree}  {info.name}";
                HoverSmall.text = $"{info.GetType()}\n{info.m_class.name}";
                if (isModToolsEnabled()) ModTools.Id = id;
            }
            else if (id.NetNode > 0)
            {
                NetInfo info = NetManager.instance.m_nodes.m_buffer[id.NetNode].Info;
                HoverLarge.text = $"N:{id.NetNode}  {info.name}";
                HoverSmall.text = $"{info.GetType()} ({info.GetAI().GetType()})\n{info.m_class.name}";
                if (isModToolsEnabled()) ModTools.Id = id;
            }
            else if (id.NetSegment > 0)
            {
                NetInfo info = NetManager.instance.m_segments.m_buffer[id.NetSegment].Info;
                HoverLarge.text = $"S:{id.NetSegment}  {info.name}";
                HoverSmall.text = $"{info.GetType()} ({info.GetAI().GetType()})\n{info.m_class.name}";
                if (isModToolsEnabled()) ModTools.Id = id;
            }

            lastId = id;
        }

        internal static bool isModToolsEnabled()
        {
            return PluginManager.instance.GetPluginsInfo().Any(mod => (mod.publishedFileID.AsUInt64 == 450877484uL || mod.name.Contains("ModTools")) && mod.isEnabled);
        }

        private void _initialise()
        {
            Panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Panel.name = "MoveIt_DebugPanel";
            Panel.atlas = ResourceLoader.GetAtlas("Ingame");
            Panel.backgroundSprite = "SubcategoriesPanel";
            Panel.size = new Vector2(300, 107);
            Panel.absolutePosition = new Vector3(Panel.GetUIView().GetScreenResolution().x - 370, 3);
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


    internal class DebugPanel_ModTools
    {
        internal InstanceID Id { get; set; }
        internal UIPanel Parent { get; set; }
        internal UIButton btn;
        private readonly object ModTools, SceneExplorer;
        private readonly Type tModTools, tSceneExplorer, tReferenceChain;
        private readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        private readonly object rcBuildings, rcProps, rcTrees, rcNodes, rcSegments;

        internal DebugPanel_ModTools(UIPanel parent)
        {
            try
            {
                Assembly mtAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Length >= 12 && assembly.FullName.Substring(0, 12) == "000_ModTools")
                    {
                        mtAssembly = assembly;
                        break;
                    }
                }
                if (mtAssembly == null)
                {
                    return;
                }

                tModTools = mtAssembly.GetType("ModTools.ModTools");
                tSceneExplorer = mtAssembly.GetType("ModTools.SceneExplorer");
                tReferenceChain = mtAssembly.GetType("ModTools.ReferenceChain");

                ModTools = tModTools.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                SceneExplorer = tModTools.GetField("sceneExplorer", flags).GetValue(ModTools);

                //Debug.Log($"\ntModTools:{tModTools}, tSceneExplorer:{tSceneExplorer}, tReferenceChain:{tReferenceChain}");
                //Debug.Log($"Fields:{tModTools.GetFields().Length}, Props:{tModTools.GetProperties().Length}, Methods:{tModTools.GetMethods().Length}");
                //Debug.Log($"{ModTools} ({ModTools.GetType()})\n{SceneExplorer} ({SceneExplorer.GetType()})");

                rcBuildings = Activator.CreateInstance(tReferenceChain);
                rcBuildings = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(GameObject) }, null).Invoke(rcBuildings, new object[] { BuildingManager.instance.gameObject });
                rcBuildings = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(BuildingManager) }, null).Invoke(rcBuildings, new object[] { BuildingManager.instance });
                rcBuildings = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcBuildings, new object[] { typeof(BuildingManager).GetField("m_buildings") });
                rcBuildings = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcBuildings, new object[] { typeof(Array16<Building>).GetField("m_buffer") });
                //Debug.Log($"rcBuildings:{rcBuildings}");

                rcProps = Activator.CreateInstance(tReferenceChain);
                rcProps = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(GameObject) }, null).Invoke(rcProps, new object[] { PropManager.instance.gameObject });
                rcProps = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(PropManager) }, null).Invoke(rcProps, new object[] { PropManager.instance });
                rcProps = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcProps, new object[] { typeof(PropManager).GetField("m_props") });
                rcProps = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcProps, new object[] { typeof(Array16<PropInstance>).GetField("m_buffer") });
                //Debug.Log($"rcProps:{rcProps}");

                rcTrees = Activator.CreateInstance(tReferenceChain);
                rcTrees = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(GameObject) }, null).Invoke(rcTrees, new object[] { TreeManager.instance.gameObject });
                rcTrees = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(TreeManager) }, null).Invoke(rcTrees, new object[] { TreeManager.instance });
                rcTrees = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcTrees, new object[] { typeof(TreeManager).GetField("m_trees") });
                rcTrees = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcTrees, new object[] { typeof(Array32<TreeInstance>).GetField("m_buffer") });
                //Debug.Log($"rcTrees:{rcTrees}");

                rcNodes = Activator.CreateInstance(tReferenceChain);
                rcNodes = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(GameObject) }, null).Invoke(rcNodes, new object[] { NetManager.instance.gameObject });
                rcNodes = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(NetManager) }, null).Invoke(rcNodes, new object[] { NetManager.instance });
                rcSegments = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcNodes, new object[] { typeof(NetManager).GetField("m_segments") });
                rcSegments = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcSegments, new object[] { typeof(Array16<NetSegment>).GetField("m_buffer") });
                rcNodes = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcNodes, new object[] { typeof(NetManager).GetField("m_nodes") });
                rcNodes = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(rcNodes, new object[] { typeof(Array16<NetNode>).GetField("m_buffer") });
                //Debug.Log($"rcNodes:{rcNodes}\nrcSegments:{rcSegments}");

                //rcPO = MoveItTool.PO.GetReferenceChain(tReferenceChain);
            }
            catch (ReflectionTypeLoadException)
            {
                SceneExplorer = null;
                Debug.Log($"MoveIt failed to integrate ModTools (ReflectionTypeLoadException)");
            }
            catch (NullReferenceException)
            {
                SceneExplorer = null;
                Debug.Log($"MoveIt failed to integrate ModTools (NullReferenceException)");
            }

            if (SceneExplorer == null)
            {
                return;
            }

            Id = InstanceID.Empty;
            Parent = parent;
            btn = parent.AddUIComponent<UIButton>();
            btn.name = "MoveIt_ToModTools";
            btn.text = ">";
            btn.textScale = 0.7f;
            btn.tooltip = "Open in ModTools Scene Explorer";
            btn.size = new Vector2(20, 20);
            btn.textPadding = new RectOffset(1, 0, 5, 1);
            btn.relativePosition = new Vector3(parent.width - 24, 22);
            btn.eventClicked += _toModTools;

            btn.atlas = ResourceLoader.GetAtlas("Ingame");
            btn.normalBgSprite = "OptionBase";
            btn.hoveredBgSprite = "OptionBaseHovered";
            btn.pressedBgSprite = "OptionBasePressed";
            btn.disabledBgSprite = "OptionBaseDisabled";
        }

        private void _toModTools(UIComponent c, UIMouseEventParameter p)
        {
            if (SceneExplorer == null)
            {
                return;
            }

            try
            {
                object rc;
                Type[] t = new Type[] { tReferenceChain };

                if (Id.Building > 0)
                {
                    rc = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(ushort) }, null).Invoke(rcBuildings, new object[] { Id.Building });
                    tSceneExplorer.GetMethod("ExpandFromRefChain", flags, null, t, null).Invoke(SceneExplorer, new object[] { rc });
                }
                else if (Id.Prop > 0)
                {
                    rc = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(ushort) }, null).Invoke(rcProps, new object[] { Id.Prop });
                    tSceneExplorer.GetMethod("ExpandFromRefChain", flags, null, t, null).Invoke(SceneExplorer, new object[] { rc });
                }
                else if (Id.Tree > 0)
                {
                    rc = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(uint) }, null).Invoke(rcTrees, new object[] { Id.Tree });
                    tSceneExplorer.GetMethod("ExpandFromRefChain", flags, null, t, null).Invoke(SceneExplorer, new object[] { rc });
                }
                else if (Id.NetNode > 0)
                {
                    rc = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(ushort) }, null).Invoke(rcNodes, new object[] { Id.NetNode });
                    tSceneExplorer.GetMethod("ExpandFromRefChain", flags, null, t, null).Invoke(SceneExplorer, new object[] { rc });
                }
                else if (Id.NetSegment > 0)
                {
                    rc = tReferenceChain.GetMethod("Add", flags, null, new Type[] { typeof(ushort) }, null).Invoke(rcSegments, new object[] { Id.NetSegment });
                    tSceneExplorer.GetMethod("ExpandFromRefChain", flags, null, t, null).Invoke(SceneExplorer, new object[] { rc });
                }

                tSceneExplorer.GetProperty("visible", BindingFlags.Public | BindingFlags.Instance).SetValue(SceneExplorer, true, null);
            }
            catch (ReflectionTypeLoadException)
            {
                Debug.Log($"MoveIt failed to call ModTools (ReflectionTypeLoadException)");
            }
            catch (NullReferenceException)
            {
                Debug.Log($"MoveIt failed to call ModTools (NullReferenceException)");
            }
        }
    }
}
