using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using UnifiedUI.Helpers;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        public static List<MoveItIntegrationBase> Integrations { get; } = IntegrationHelper.GetIntegrations();
        public static MoveItIntegrationBase GetIntegrationByID(string ID) => Integrations.Where(item => item.ID == ID).FirstOrDefault();

        public enum ToolAction
        {
            None,
            Do,
            Undo,
            Redo
        }

        public enum ToolStates
        {
            Default,
            MouseDragging,
            RightDraggingClone,
            DrawingSelection,
            Cloning,
            Aligning,
            Picking,
            ToolActive
        }

        public enum MT_Tools
        {
            Off,
            Height,
            Inplace,
            Group,
            Slope,
            Mirror,
            MoveTo
        }

        public const string settingsFileName = "MoveItTool";
        public static readonly string saveFolder = Path.Combine(DataLocation.localApplicationData, "MoveItExports");
        public const int UI_Filter_CB_Height = 25;
        public const int Fastmove_Max = 100;

        public static MoveItTool instance;
        public static SavedBool hideChangesWindow = new SavedBool("hideChanges297", settingsFileName, false, true);
        public static SavedBool autoCloseAlignTools = new SavedBool("autoCloseAlignTools", settingsFileName, false, true);
        public static SavedBool POShowDeleteWarning = new SavedBool("POShowDeleteWarning", settingsFileName, true, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);
        public static SavedBool rmbCancelsCloning = new SavedBool("rmbCancelsCloning", settingsFileName, false, true);
        public static SavedBool advancedPillarControl = new SavedBool("advancedPillarControl", settingsFileName, false, true);
        public static SavedBool fastMove = new SavedBool("fastMove", settingsFileName, false, true);
        public static SavedBool altSelectNodeBuildings = new SavedBool("altSelectNodeBuildings", settingsFileName, false, true);
        public static SavedBool altSelectSegmentNodes = new SavedBool("altSelectSegmentNodes", settingsFileName, true, true);
        public static SavedBool followTerrainModeEnabled = new SavedBool("followTerrainModeEnabled", settingsFileName, true, true);
        public static SavedBool showDebugPanel = new SavedBool("showDebugPanel", settingsFileName, false, true);
        public static SavedBool useUUI = new SavedBool("useUUI", settingsFileName, false, true);

        public static bool filterPicker = false;
        public static bool filterBuildings = true;
        public static bool filterProps = true;
        public static bool filterDecals = true;
        public static bool filterSurfaces = true;
        public static bool filterTrees = true;
        public static bool filterNodes = true;
        public static bool filterSegments = true;
        public static bool filterNetworks = false;
        public static bool filterProcs = true;

        public static bool followTerrain = true;
        public static bool marqueeSelection = false;
        internal static bool dragging = false;
        public static bool treeSnapping = false;

        public static StepOver stepOver;
        internal static DebugPanel m_debugPanel;
        internal static MoveToPanel m_moveToPanel;

        public int segmentUpdateCountdown = -1;
        public HashSet<ushort> segmentsToUpdate = new HashSet<ushort>();

        public int areaUpdateCountdown = -1;
        public HashSet<Bounds> areasToUpdate = new HashSet<Bounds>();
        public HashSet<Bounds> areasToQuickUpdate = new HashSet<Bounds>();

        internal static Color m_hoverColor = new Color32(0, 181, 255, 255);
        internal static Color m_selectedColor = new Color32(95, 166, 0, 244);
        internal static Color m_moveColor = new Color32(125, 196, 30, 244);
        internal static Color m_removeColor = new Color32(255, 160, 47, 191);
        internal static Color m_despawnColor = new Color32(255, 160, 47, 191);
        internal static Color m_alignColor = new Color32(255, 255, 255, 244);
        internal static Color m_POhoverColor = new Color32(240, 140, 255, 230);
        internal static Color m_POselectedColor = new Color32(225, 130, 240, 125);
        internal static Color m_POhoverGroup = new Color32(255, 45, 45, 230);
        internal static Color m_POselectedGroup = new Color32(240, 30, 30, 150);
        internal static bool m_showSelectors = true;

        internal static PO_Manager PO = null;
        internal static NS_Manager NS = null;
        private static int _POProcessing = 0;
        private static float POProcessingStart = 0;
        internal static int POProcessing
        {
            get
            {
                return _POProcessing;
            }
            set
            {
                _POProcessing = value;
                POProcessingStart = Time.time;
                if (m_debugPanel != null)
                {
                    m_debugPanel.UpdatePanel();
                }
            }
        }

        private const float XFACTOR = 0.25f;
        private const float YFACTOR = 0.015625f; // 1/64
        private const float ZFACTOR = 0.25f;

        public static ToolStates ToolState { get; set; } = ToolStates.Default;
        private static MT_Tools m_toolsMode = MT_Tools.Off;
        public static MT_Tools MT_Tool
        {
            get => m_toolsMode;
            set
            {
                m_toolsMode = value;
                if (m_debugPanel != null)
                {
                    m_debugPanel.UpdatePanel();
                }
            }
        }
        private static ushort m_alignToolPhase = 0;
        public static ushort AlignToolPhase
        {
            get => m_alignToolPhase;
            set
            {
                m_alignToolPhase = value;
                if (m_debugPanel != null)
                {
                    m_debugPanel.UpdatePanel();
                }
            }
        }

        private bool m_snapping = false;
        public bool snapping
        {
            get
            {
                if (ToolState == ToolStates.MouseDragging ||
                    ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                {
                    return m_snapping != Event.current.alt;
                }
                return m_snapping;
            }

            set
            {
                m_snapping = value;
            }
        }

        public static bool gridVisible
        {
            get
            {
                return TerrainManager.instance.RenderZones;
            }

            set
            {
                TerrainManager.instance.RenderZones = value;
            }
        }

        public static bool tunnelVisible
        {
            get
            {
                return InfoManager.instance.CurrentMode == InfoManager.InfoMode.Underground;
            }

            set
            {
                if (value)
                {
                    m_prevInfoMode = InfoManager.instance.CurrentMode;
                    InfoManager.instance.SetCurrentMode(InfoManager.InfoMode.Underground, InfoManager.instance.CurrentSubMode);
                }
                else
                {
                    InfoManager.instance.SetCurrentMode(m_prevInfoMode, InfoManager.instance.CurrentSubMode);
                }
            }
        }

        internal UIMoveItButton m_button;
        private UIComponent m_pauseMenu;

        private Quad3 m_selection; // Marquee selection box
        public Instance m_hoverInstance;
        internal Instance m_lastInstance;
        private HashSet<Instance> m_marqueeInstances;

        internal static bool m_isLowSensitivity;
        private Vector3 m_dragStartRelative; // Where the current drag started, relative to selection center
        private Vector3 m_clickPositionAbs; // Where the current drag started, absolute
        private Vector3 m_sensitivityTogglePosAbs; // Where sensitivity was last toggled, absolute

        private float m_mouseStartX;
        private float m_startAngle;
        private float m_sensitivityTogglePosX; // Where sensitivity was last toggled, X-axis absolute
        private float m_sensitivityAngleOffset; // Accumulated angle offset from low sensitivity

        private NetSegment m_segmentGuide;//, m_segmentGuide2;

        private bool m_prevRenderZones;
        private ToolBase m_prevTool;

        private static InfoManager.InfoMode m_prevInfoMode;

        private long m_keyTime;
        private long m_scaleKeyTime;
        private long m_rightClickTime;
        private long m_middleClickTime;
        private long m_leftClickTime;

        internal static Dictionary<ushort, ushort> m_pillarMap; // Building -> First Node

        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
        protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

        internal UIPopupPanel m_whatsNewPanel;
        internal static UIPopupPanel m_lsmWarningPanel;
        private static bool lsmHasNagged = false;

        public ToolAction m_nextAction = ToolAction.None;

        private static System.Random _rand = null;
        internal static System.Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new System.Random();
                return _rand;
            }
        }

        public UIComponent UUIButton;

        protected override void Awake()
        {
            ActionQueue.instance = new ActionQueue();

            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;

            // Unified UI
            EnableUUI();

            followTerrain = followTerrainModeEnabled;
            if (!isTreeAnarchyEnabled())
            {
                treeSnapping = isTreeSnappingEnabled();
            }

            PropLayer.Initialise();
        }

        protected override void OnEnable()
        {
            //string msg = $"Mods:";
            //foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            //{
            //    msg += $"\n{pluginInfo.name} ({pluginInfo.isEnabled}, {pluginInfo.userModInstance.GetType().Name}):\n  ";
            //    foreach (Assembly assembly in pluginInfo.GetAssemblies())
            //    {
            //        msg += $"{assembly.GetName().Name.ToLower()}, ";
            //    }
            //}
            //Log.Debug(msg);

            try
            {
                if (PO == null)
                {
                    PO = new PO_Manager();
                }
            }
            catch (Exception e)
            {
                Log.Error($"PO Failed:\n{e}");
            }

            try
            {
                if (NS == null)
                {
                    NS = new NS_Manager();
                }
            }
            catch (Exception e)
            {
                Log.Error($"NetworkSkins Failed:\n{e}");
            }

            if (UIToolOptionPanel.instance == null)
            {
                UIComponent TSBar = UIView.GetAView().FindUIComponent<UIComponent>("TSBar");
                TSBar.AddUIComponent<UIToolOptionPanel>();
            }
            else
            {
                UIToolOptionPanel.instance.isVisible = true;
            }

            //if (!hideChangesWindow)
            //{
            //    m_whatsNewPanel = UIChangesWindow.Open(typeof(UIChangesWindow));
            //}

            NagOldLSM();

            m_pauseMenu = UIView.library.Get("PauseMenu");

            m_prevInfoMode = InfoManager.instance.CurrentMode;
            InfoManager.SubInfoMode subInfoMode = InfoManager.instance.CurrentSubMode;

            m_prevRenderZones = TerrainManager.instance.RenderZones;
            m_prevTool = m_toolController.CurrentTool == this ? ToolsModifierControl.GetTool<DefaultTool>() : m_toolController.CurrentTool;

            m_toolController.CurrentTool = this;

            InfoManager.instance.SetCurrentMode(m_prevInfoMode, subInfoMode);

            if (UIToolOptionPanel.instance != null && UIToolOptionPanel.instance.grid != null)
            {
                gridVisible = UIToolOptionPanel.instance.grid.activeStateIndex == 1;
                tunnelVisible = UIToolOptionPanel.instance.underground.activeStateIndex == 1;
            }

            if (PO.Active)
            {
                PO.ToolEnabled();
                if (POProcessing > 0 && Time.time > POProcessingStart + 300)
                { // If it's been more than 5 mins since PO last started copying, give up and reset
                    Log.Info($"Timing out PO Processing");
                    POProcessing = 0;
                }
                ActionQueue.instance.Push(new TransformAction());
            }

            UIMoreTools.UpdateMoreTools();
            UpdatePillarMap();
        }

        protected override void OnDisable()
        {
            lock (ActionQueue.instance)
            {
                if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                {
                    // Cancel cloning
                    ActionQueue.instance.Undo();
                    ActionQueue.instance.Invalidate();
                }

                if (ToolState == ToolStates.MouseDragging)
                {
                    ((TransformAction)ActionQueue.instance.current).FinaliseDrag();
                }

                UpdateAreas();
                UpdateSegments();
                SetToolState();

                if (m_whatsNewPanel != null)
                {
                    m_whatsNewPanel.isVisible = false;
                    m_whatsNewPanel = null;
                }

                if (m_lsmWarningPanel != null)
                {
                    m_lsmWarningPanel.isVisible = false;
                    m_lsmWarningPanel = null;
                }

                if (UIToolOptionPanel.instance != null)
                {
                    UIToolOptionPanel.instance.isVisible = false;
                }

                if (m_moveToPanel != null)
                {
                    m_moveToPanel.Visible(false);
                }

                InfoManager.instance.SetCurrentMode(m_prevInfoMode, InfoManager.instance.CurrentSubMode);

                if (m_toolController.NextTool == null && m_prevTool != null && m_prevTool != this)
                {
                    TerrainManager.instance.RenderZones = m_prevRenderZones;
                    m_prevTool.enabled = true;
                }
                m_prevTool = null;

                UIMoreTools.UpdateMoreTools();
                UIToolOptionPanel.RefreshCloneButton();
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!enabled)
            {
                return;
            }

            if (ToolState == ToolStates.Default || ToolState == ToolStates.Aligning || ToolState == ToolStates.Picking || ToolState == ToolStates.ToolActive)
            {
                // Reset all PO
                //if (PO.Active && POHighlightUnselected)
                //{
                //    foreach (PO_Object obj in PO.Objects)
                //    {
                //        obj.Selected = false;
                //    }
                //}

                // Debug overlays
                foreach (DebugOverlay d in DebugBoxes)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, d.color, d.quad, 0, 1000, false, false);
                }
                foreach (Vector3 v in DebugPoints)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, new Color32(255, 255, 255, 63), v, 8, 0, 1000, false, false);
                }

                ActionQueue.instance.current?.Overlays(cameraInfo, GetSelectorColor(m_alignColor), GetSelectorColor(m_despawnColor));

                if (Action.HasSelection())
                {
                    // Highlight Selected Items
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid && instance != m_hoverInstance)
                        {
                            if (instance is MoveableProc mpo)
                            {
                                if (m_hoverInstance == null || (m_hoverInstance.isValid && (mpo.id != m_hoverInstance.id)))
                                {
                                    mpo.RenderOverlay(cameraInfo, GetSelectorColor(m_POselectedColor), GetSelectorColor(m_despawnColor));
                                    mpo.m_procObj.Selected = true;
                                }
                            }
                            else
                            {
                                instance.RenderOverlay(cameraInfo, GetSelectorColor(m_selectedColor), GetSelectorColor(m_despawnColor));
                            }
                        }
                    }
                    if (ToolState == ToolStates.Aligning && MT_Tool == MT_Tools.Slope && AlignToolPhase == 2)
                    {
                        AlignSlopeAction action = ActionQueue.instance.current as AlignSlopeAction;
                        action.PointA.RenderOverlay(cameraInfo, GetSelectorColor(m_alignColor), GetSelectorColor(m_despawnColor));
                    }

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, GetSelectorColor(m_selectedColor), center, 1f, -1f, 1280f, false, true);
                }

                if (m_hoverInstance != null && m_hoverInstance.isValid)
                {
                    Color color = m_hoverColor;
                    if (m_hoverInstance is MoveableProc mpo)
                    {
                        if (!mpo.m_procObj.isGroupRoot() && mpo.m_procObj.Group != null)
                        {
                            InstanceID rootInstance = default;
                            rootInstance.NetLane = mpo.m_procObj.Group.root.Id;
                            m_hoverInstance = new MoveableProc(rootInstance);
                        }
                        color = m_POhoverColor;
                        mpo.m_procObj.Selected = true;
                    }

                    if (ToolState == ToolStates.Aligning || ToolState == ToolStates.Picking)
                    {
                        color = m_alignColor;
                    }
                    else if (Action.selection.Contains(m_hoverInstance))
                    {
                        if (Event.current.shift)
                        {
                            color = m_removeColor;
                        }
                    }

                    m_hoverInstance.RenderOverlay(cameraInfo, GetSelectorColor(color), GetSelectorColor(m_despawnColor));
                }
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                if (Action.HasSelection())
                {
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid && instance != m_hoverInstance)
                        {
                            instance.RenderOverlay(cameraInfo, GetSelectorColor(m_moveColor), GetSelectorColor(m_despawnColor));
                        }
                    }

                    if (!m_isLowSensitivity)
                    {
                        Vector3 center = Action.GetCenter();
                        center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, GetSelectorColor(m_selectedColor), center, 1f, -1f, 1280f, false, true);

                        if (snapping)
                        {
                            if (m_segmentGuide.m_startNode != 0 && m_segmentGuide.m_endNode != 0)
                            {
                                NetManager netManager = NetManager.instance;
                                NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

                                Bezier3 bezier;
                                bezier.a = nodeBuffer[m_segmentGuide.m_startNode].m_position;
                                bezier.d = nodeBuffer[m_segmentGuide.m_endNode].m_position;

                                bool smoothStart = ((nodeBuffer[m_segmentGuide.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);
                                bool smoothEnd = ((nodeBuffer[m_segmentGuide.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);

                                NetSegment.CalculateMiddlePoints(
                                    bezier.a, m_segmentGuide.m_startDirection,
                                    bezier.d, m_segmentGuide.m_endDirection,
                                    smoothStart, smoothEnd, out bezier.b, out bezier.c);

                                RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, GetSelectorColor(m_selectedColor), bezier, 0f, 100000f, -100000f, -1f, 1280f, false, true);

                                //if (m_segmentGuide2.m_startNode != 0 && m_segmentGuide2.m_endNode != 0)
                                //{
                                //    bezier.a = nodeBuffer[m_segmentGuide2.m_startNode].m_position;
                                //    bezier.d = nodeBuffer[m_segmentGuide2.m_endNode].m_position;

                                //    smoothStart = ((nodeBuffer[m_segmentGuide2.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);
                                //    smoothEnd = ((nodeBuffer[m_segmentGuide2.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);

                                //    NetSegment.CalculateMiddlePoints(
                                //        bezier.a, m_segmentGuide2.m_startDirection,
                                //        bezier.d, m_segmentGuide2.m_endDirection,
                                //        smoothStart, smoothEnd, out bezier.b, out bezier.c);

                                //    RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, GetSelectorColor(m_selectedColor * 0.75f), bezier, 0f, 100000f, -100000f, -1f, 1280f, false, true);
                                //}
                            }
                        }
                    }
                }
            }
            else if (ToolState == ToolStates.DrawingSelection)
            {
                bool removing = Event.current.alt;
                bool adding = Event.current.shift;

                if ((removing || adding) && Action.HasSelection())
                {
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid)
                        {
                            if (adding || (removing && !m_marqueeInstances.Contains(instance)))
                            {
                                instance.RenderOverlay(cameraInfo, GetSelectorColor(m_selectedColor), GetSelectorColor(m_despawnColor));
                            }
                        }
                    }

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);

                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, GetSelectorColor(m_selectedColor), center, 1f, -1f, 1280f, false, true);
                }

                Color color = m_hoverColor;
                if (removing)
                {
                    color = m_removeColor;
                }

                if (m_selection.a != m_selection.c)
                {
                    RenderManager.instance.OverlayEffect.DrawQuad(cameraInfo, GetSelectorColor(color), m_selection, -1f, 1280f, false, true);
                }

                if (m_marqueeInstances != null)
                {
                    foreach (Instance instance in m_marqueeInstances)
                    {
                        if (instance.isValid)
                        {
                            if (instance is MoveableProc mpo)
                            {
                                if (mpo.m_procObj.Group != null && mpo.m_procObj.Group.root != mpo.m_procObj)
                                    continue;
                            }

                            bool contains = Action.selection.Contains(instance);
                            if ((adding && !contains) || (removing && contains) || (!adding && !removing))
                            {
                                instance.RenderOverlay(cameraInfo, GetSelectorColor(color), GetSelectorColor(m_despawnColor));
                            }
                        }
                    }
                }
            }
            else if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                CloneActionBase action = ActionQueue.instance.current as CloneActionBase;

                Matrix4x4 matrix4x = default;
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.m_states)
                {
                    Color color = m_hoverColor;
                    if (state is ProcState)
                    {
                        color = m_POhoverColor;
                    }

                    state.instance.RenderCloneOverlay(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, GetSelectorColor(color));
                }
            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                //Log.Debug($"AAA01 {ToolState}");
                CloneActionBase action = ActionQueue.instance.current as CloneActionBase;
                //Log.Debug($"AAA02 {action}");

                Matrix4x4 matrix4x = default;
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.m_states)
                {
                    try
                    {
                        //Log.Debug($"AAA03 {state}");
                        //Log.Debug($"AAA04 {state.instance}");
                        state.instance.RenderCloneGeometry(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, GetSelectorColor(m_hoverColor));
                    }
                    catch (Exception e)
                    {
                        Log.Debug(e.Message);
                    }
                }
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;

                foreach (InstanceState state in action.m_states)
                {
                    state.instance?.RenderGeometry(cameraInfo, GetSelectorColor(m_hoverColor));
                }
            }
        }

        public override void SimulationStep()
        {
            lock (ActionQueue.instance)
            {
                try
                {
                    switch (m_nextAction)
                    {
                        case ToolAction.Undo:
                            {
                                ActionQueue.instance.Undo();
                                break;
                            }
                        case ToolAction.Redo:
                            {
                                ActionQueue.instance.Redo();
                                break;
                            }
                        case ToolAction.Do:
                            {
                                ActionQueue.instance.Do();

                                if (ActionQueue.instance.current is CloneAction)
                                {
                                    StartCloning();
                                }
                                break;
                            }
                    }

                    bool inputHeld = m_scaleKeyTime != 0 || m_keyTime != 0 || m_leftClickTime != 0 || m_rightClickTime != 0;

                    if (segmentUpdateCountdown == 0)
                    {
                        UpdateSegments();
                    }

                    if (!inputHeld && segmentUpdateCountdown >= 0)
                    {
                        segmentUpdateCountdown--;
                    }

                    if (areaUpdateCountdown == 0)
                    {
                        UpdateAreas();
                    }

                    if (!inputHeld && areaUpdateCountdown >= 0)
                    {
                        areaUpdateCountdown--;
                    }
                }
                catch (Exception e)
                {
                    DebugUtils.Log("SimulationStep failed");
                    DebugUtils.LogException(e);
                }

                m_nextAction = ToolAction.None;
            }
        }

        public void UpdateAreas()
        {
            //foreach (Bounds b in areasToUpdate)
            //{
            //    AddDebugBox(b, new Color32(255, 31, 31, 31));
            //}
            HashSet<Bounds> merged = areasToUpdate;
            merged.UnionWith(areasToQuickUpdate);
            merged = MergeBounds(merged);
            bool full = areasToUpdate.Count() != 0;
            //Log.Debug($"AAA UpdateAreas:\nFull:{areasToUpdate.Count}\nMerged:{merged}\nFast:{areasToQuickUpdate.Count}");
            //foreach (Bounds b in merged)
            //{
            //    b.Expand(4f);
            //    AddDebugBox(b, new Color32(31, 31, 255, 31));
            //}

            foreach (Bounds bounds in merged)
            {
                try
                {
                    if (full)
                    {
                        Bounds small = bounds;
                        small.Expand(-16f);
                        SimulationManager.instance.AddAction(() => { Singleton<VehicleManager>.instance.UpdateParkedVehicles(small.min.x, small.min.z, small.max.x, small.max.z); });
                    }
                    bounds.Expand(64f);
                    SimulationManager.instance.AddAction(() => { TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false); });
                    UpdateRender(bounds);

                    if (full)
                    {
                        bounds.Expand(512f);
                        Singleton<ElectricityManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                        Singleton<WaterManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Log.Error($"Failed to update bounds {bounds}");
                }
            }

            areasToUpdate.Clear();
            areasToQuickUpdate.Clear();
        }

        public void UpdateSegments()
        {
            foreach (ushort segment in segmentsToUpdate)
            {
                NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
                if (segmentBuffer[segment].m_flags != NetSegment.Flags.None)
                {
                    ReleaseSegmentBlock(segment, ref segmentBuffer[segment].m_blockStartLeft);
                    ReleaseSegmentBlock(segment, ref segmentBuffer[segment].m_blockStartRight);
                    ReleaseSegmentBlock(segment, ref segmentBuffer[segment].m_blockEndLeft);
                    ReleaseSegmentBlock(segment, ref segmentBuffer[segment].m_blockEndRight);
                }

                segmentBuffer[segment].Info.m_netAI.CreateSegment(segment, ref segmentBuffer[segment]);
            }
            segmentsToUpdate.Clear();
        }

        public override ToolErrors GetErrors()
        {
            return ToolErrors.None;
        }

        internal static Vector3 RaycastMouseLocation()
        {
            return RaycastMouseLocation(Camera.main.ScreenPointToRay(Input.mousePosition));
        }

        internal static Vector3 RaycastMouseLocation(Ray mouseRay)
        {
            RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane)
            {
                m_ignoreTerrain = false
            };
            RayCast(input, out RaycastOutput output);

            return output.m_hitPos;
        }

        private static void UpdateRender(Bounds bounds)
        {
            int num1 = Mathf.Clamp((int)(bounds.min.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(bounds.min.z / 64f + 135f), 0, 269);
            int x0 = num1 * 45 / 270 - 1;
            int z0 = num2 * 45 / 270 - 1;

            num1 = Mathf.Clamp((int)(bounds.max.x / 64f + 135f), 0, 269);
            num2 = Mathf.Clamp((int)(bounds.max.z / 64f + 135f), 0, 269);
            int x1 = num1 * 45 / 270 + 1;
            int z1 = num2 * 45 / 270 + 1;

            RenderManager renderManager = Singleton<RenderManager>.instance;
            RenderGroup[] renderGroups = renderManager.m_groups;

            for (int i = z0; i < z1; i++)
            {
                for (int j = x0; j < x1; j++)
                {
                    int n = Mathf.Clamp(i * 45 + j, 0, renderGroups.Length - 1);

                    if (n < 0)
                    {
                        continue;
                    }
                    else if (n >= renderGroups.Length)
                    {
                        break;
                    }

                    if (renderGroups[n] != null)
                    {
                        renderGroups[n].SetAllLayersDirty();
                        renderManager.m_updatedGroups1[n >> 6] |= 1uL << n;
                        renderManager.m_groupsUpdated1 = true;
                    }
                }
            }
        }
    }
}
