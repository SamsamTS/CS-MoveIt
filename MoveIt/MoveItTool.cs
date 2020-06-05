using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
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
        public static SavedBool hideChangesWindow = new SavedBool("hideChanges280b", settingsFileName, false, true); 
        public static SavedBool autoCloseAlignTools = new SavedBool("autoCloseAlignTools", settingsFileName, true, true);
        public static SavedBool POHighlightUnselected = new SavedBool("POHighlightUnselected", settingsFileName, false, true);
        public static SavedBool POShowDeleteWarning = new SavedBool("POShowDeleteWarning", settingsFileName, true, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);
        public static SavedBool rmbCancelsCloning = new SavedBool("rmbCancelsCloning", settingsFileName, false, true);
        public static SavedBool fastMove = new SavedBool("fastMove", settingsFileName, false, true);
        public static SavedBool hideSelectorsOnLowSensitivity = new SavedBool("hideSelectorsOnLowSensitivity", settingsFileName, true, true);
        public static SavedBool altSelectNodeBuildings = new SavedBool("altSelectNodeBuildings", settingsFileName, false, true);
        public static SavedBool altSelectSegmentNodes = new SavedBool("altSelectSegmentNodes", settingsFileName, true, true);
        public static SavedBool followTerrainModeEnabled = new SavedBool("followTerrainModeEnabled", settingsFileName, true, true);
        public static SavedBool showDebugPanel = new SavedBool("showDebugPanel", settingsFileName, false, true);

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

        public static StepOver stepOver;
        internal static DebugPanel m_debugPanel;
        internal static MoveToPanel m_moveToPanel;

        public int segmentUpdateCountdown = -1;
        public HashSet<ushort> segmentsToUpdate = new HashSet<ushort>();

        public int areaUpdateCountdown = -1;
        public HashSet<Bounds> areasToUpdate = new HashSet<Bounds>();

        internal static Color m_hoverColor = new Color32(0, 181, 255, 255);
        internal static Color m_selectedColor = new Color32(95, 166, 0, 244);
        internal static Color m_moveColor = new Color32(125, 196, 30, 244);
        internal static Color m_removeColor = new Color32(255, 160, 47, 191);
        internal static Color m_despawnColor = new Color32(255, 160, 47, 191);
        internal static Color m_alignColor = new Color32(255, 255, 255, 244);
        internal static Color m_POhoverColor = new Color32(240, 140, 255, 230);
        internal static Color m_POselectedColor = new Color32(225, 130, 240, 125);
        internal static Color m_POdisabledColor = new Color32(130, 95, 140, 70);

        internal static PO_Manager PO = null;
        internal static NS_Manager NS = null;
        private static int _POProcessing = 0;
        internal static int POProcessing
        {
            get
            {
                return _POProcessing;
            }
            set
            {
                _POProcessing = value;
                if (m_debugPanel != null)
                {
                    m_debugPanel.UpdatePanel();
                }
            }
        }

        private const float XFACTOR = 0.25f; //63671875f;
        private const float YFACTOR = 0.015625f; // 1/64
        private const float ZFACTOR = 0.25f; //63671875f;

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
                if(value)
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

        private UIMoveItButton m_button;
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

        private NetSegment m_segmentGuide;

        private bool m_prevRenderZones;
        private ToolBase m_prevTool;

        private static InfoManager.InfoMode m_prevInfoMode;

        private long m_keyTime;
        private long m_scaleKeyTime;
        private long m_rightClickTime;
        private long m_middleClickTime;
        private long m_leftClickTime;

        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

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
        
        protected override void Awake()
        {
            ActionQueue.instance = new ActionQueue();

            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;

            followTerrain = followTerrainModeEnabled;
        }

        protected override void OnEnable()
        {
            if (PO == null)
            {
                PO = new PO_Manager();
            }
            if (NS == null)
            {
                NS = new NS_Manager();
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

            if (!hideChangesWindow && UIChangesWindow.instance != null)
            {
                UIChangesWindow.instance.isVisible = true;
            }

            m_pauseMenu = UIView.library.Get("PauseMenu");

            m_prevInfoMode = InfoManager.instance.CurrentMode;
            InfoManager.SubInfoMode subInfoMode = InfoManager.instance.CurrentSubMode;

            m_prevRenderZones = TerrainManager.instance.RenderZones;
            m_prevTool = m_toolController.CurrentTool;
            
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
                ActionQueue.instance.Push(new TransformAction());
            }

            UIMoreTools.UpdateMoreTools();

            //string msg = $"Assemblies:";
            //foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    msg += $"\n{assembly.GetName().Name.ToLower()}";
            //}
            //Debug.Log(msg);

            // msg = "Plugins:";
            //foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            //{
            //    msg += $"\n{pi.publishedFileID.AsUInt64} - {pi.name} ({pi.isEnabled})" +
            //        $"\n - {pi.modPath}";
            //}
            //Debug.Log(msg);
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

                if (UIChangesWindow.instance != null)
                {
                    UIChangesWindow.instance.isVisible = false;
                }

                if (UIToolOptionPanel.instance != null)
                {
                    UIToolOptionPanel.instance.isVisible = false;
                }

                if (m_moveToPanel != null)
                {
                    m_moveToPanel.Visible(false);
                }

                TerrainManager.instance.RenderZones = m_prevRenderZones;
                InfoManager.instance.SetCurrentMode(m_prevInfoMode, InfoManager.instance.CurrentSubMode);

                if (m_toolController.NextTool == null && m_prevTool != null && m_prevTool != this)
                {
                    m_prevTool.enabled = true;
                }
                m_prevTool = null;

                UIMoreTools.UpdateMoreTools();
                UIToolOptionPanel.RefreshCloneButton();
            }
        }

        public static void SetToolState(ToolStates state = ToolStates.Default, MT_Tools tool = MT_Tools.Off, ushort toolPhase = 0)
        {
            ToolStates previousState = ToolState;
            if (ToolState != state)
            {
                if (state != ToolStates.ToolActive && state != ToolStates.Aligning)
                {
                    UIMoreTools.m_activeToolMenu = null;
                }

                if (ToolState == ToolStates.ToolActive)
                {
                    if (MT_Tool == MT_Tools.MoveTo)
                    {
                        m_moveToPanel.Visible(false);
                    }
                }
            }

            ToolState = state;
            m_toolsMode = tool;
            m_alignToolPhase = toolPhase;

            if (state == ToolStates.ToolActive || state == ToolStates.Aligning || previousState == ToolStates.ToolActive || previousState == ToolStates.Aligning)
            {
                UIMoreTools.UpdateMoreTools();
            }
            m_debugPanel?.UpdatePanel();
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
                if (PO.Active && POHighlightUnselected)
                {
                    foreach (PO_Object obj in PO.Objects)
                    {
                        obj.Selected = false;
                    }
                }

                // Debug overlays
                foreach (DebugOverlay d in DebugBoxes)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, d.color, d.quad, 0, 1000, false, false);
                }
                foreach (Vector3 v in DebugPoints)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, new Color32(255, 255, 255, 63), v, 8, 0, 1000, false, false);
                }

                ActionQueue.instance.current?.Overlays(cameraInfo, m_alignColor, m_despawnColor);

                if (Action.selection.Count > 0)
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
                                    mpo.RenderOverlay(cameraInfo, m_POselectedColor, m_despawnColor);
                                    mpo.m_procObj.Selected = true;
                                }
                            }
                            else
                            {
                                instance.RenderOverlay(cameraInfo, m_selectedColor, m_despawnColor);
                            }
                        }
                    }
                    if (ToolState == ToolStates.Aligning && MT_Tool == MT_Tools.Slope && AlignToolPhase == 2)
                    {
                        AlignSlopeAction action = ActionQueue.instance.current as AlignSlopeAction;
                        action.PointA.RenderOverlay(cameraInfo, m_alignColor, m_despawnColor);
                    }

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, -1f, 1280f, false, true);
                }

                if (m_hoverInstance != null && m_hoverInstance.isValid)
                {
                    Color color = m_hoverColor;
                    if (m_hoverInstance is MoveableProc mpo)
                    {
                        color = m_POhoverColor;
                        mpo.m_procObj.Selected = true;
                    }

                    if (ToolState == ToolStates.Aligning || ToolState == ToolStates.Picking)
                    {
                        color = m_alignColor;
                    }
                    else if (Action.selection.Contains(m_hoverInstance))
                    {
                        if(Event.current.shift)
                        {
                            color = m_removeColor;
                        }
                    }

                    m_hoverInstance.RenderOverlay(cameraInfo, color, m_despawnColor);
                }

                // Highlight unselected PO
                if (PO.Active && POHighlightUnselected)
                {
                    foreach (PO_Object obj in PO.Objects)
                    {
                        if (!obj.Selected)
                        {
                            obj.RenderOverlay(cameraInfo, m_POdisabledColor);
                        }
                    }
                }
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                if (Action.selection.Count > 0)
                {
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid && instance != m_hoverInstance)
                        {
                            instance.RenderOverlay(cameraInfo, m_moveColor, m_despawnColor);
                        }
                    }

                    if (!(m_isLowSensitivity && hideSelectorsOnLowSensitivity))
                    {
                        Vector3 center = Action.GetCenter();
                        center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, -1f, 1280f, false, true);
                    }

                    if (snapping && m_segmentGuide.m_startNode != 0 && m_segmentGuide.m_endNode != 0)
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

                        RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, m_selectedColor, bezier, 0f, 100000f, -100000f, -1f, 1280f, false, true);
                    }
                }
            }
            else if (ToolState == ToolStates.DrawingSelection)
            {
                bool removing = Event.current.alt;
                bool adding = Event.current.shift;

                if ((removing || adding) && Action.selection.Count > 0)
                {
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid)
                        {
                            if (adding || (removing && !m_marqueeInstances.Contains(instance)))
                            {
                                instance.RenderOverlay(cameraInfo, m_selectedColor, m_despawnColor);
                            }
                        }
                    }

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);

                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, -1f, 1280f, false, true);
                }

                Color color = m_hoverColor;
                if (removing)
                {
                    color = m_removeColor;
                }

                if (m_selection.a != m_selection.c)
                {
                    RenderManager.instance.OverlayEffect.DrawQuad(cameraInfo, color, m_selection, -1f, 1280f, false, true);
                }

                if (m_marqueeInstances != null)
                {
                    foreach (Instance instance in m_marqueeInstances)
                    {
                        if (instance.isValid)
                        {
                            bool contains = Action.selection.Contains(instance);
                            if ((adding && !contains) || (removing && contains) || (!adding && !removing))
                            {
                                instance.RenderOverlay(cameraInfo, color, m_despawnColor);
                            }
                        }
                    }
                }
            }
            else if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;

                Matrix4x4 matrix4x = default;
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.m_states)
                {
                    Color color = m_hoverColor;
                    if (state is ProcState)
                    {
                        color = m_POhoverColor;
                    }

                    state.instance.RenderCloneOverlay(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, color);
                }
            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;

                Matrix4x4 matrix4x = default;
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.m_states)
                {
                    state.instance.RenderCloneGeometry(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, m_hoverColor);
                }
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;

                foreach (InstanceState state in action.m_states)
                {
                    state.instance.RenderGeometry(cameraInfo, m_hoverColor);
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

        #region Debug Overlays
        internal struct DebugOverlay
        {
            internal Quad3 quad;
            internal Color32 color;

            public DebugOverlay(Quad3 q, Color32 c)
            {
                quad = q;
                color = c;
            }
        }
        private static List<DebugOverlay> DebugBoxes = new List<DebugOverlay>();
        private static List<Vector3> DebugPoints = new List<Vector3>();
        internal static void AddDebugBox(Bounds b, Color32? c = null)
        {
            if (c == null)
            {
                c = new Color32(255, 255, 255, 63);
            }
            Quad3 q = default;
            q.a = new Vector3(b.min.x, b.min.y, b.min.z);
            q.b = new Vector3(b.min.x, b.min.y, b.max.z);
            q.c = new Vector3(b.max.x, b.min.y, b.max.z);
            q.d = new Vector3(b.max.x, b.min.y, b.min.z);
            DebugOverlay d = new DebugOverlay(q, (Color32)c);
            DebugBoxes.Add(d);
            Debug.Log($"\nBounds:{b}");
        }
        internal static void AddDebugPoint(Vector3 v)
        {
            DebugPoints.Add(v);
            Debug.Log($"\nPoint:{v}");
        }
        internal void ClearDebugOverlays()
        {
            DebugBoxes.Clear();
            DebugPoints.Clear();
        }
        internal static Color32 GetRandomDebugColor()
        {
            return new Color32(RandomByte(100, 255), RandomByte(100, 255), RandomByte(100, 255), 63);
        }
        #endregion

        public void UpdateAreas()
        {
            //foreach (Bounds b in areasToUpdate)
            //{
            //    AddDebugBox(b, new Color32(255, 31, 31, 31));
            //}
            HashSet<Bounds> merged = MergeBounds(areasToUpdate);
            //foreach (Bounds b in merged)
            //{
            //    b.Expand(4f);
            //    AddDebugBox(b, new Color32(31, 31, 255, 31));
            //}

            foreach (Bounds bounds in merged)
            {
                try
                {
                    bounds.Expand(64f);
                    Singleton<VehicleManager>.instance.UpdateParkedVehicles(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
                    bounds.Expand(512f);
                    Singleton<ElectricityManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    Singleton<WaterManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Failed to update bounds {bounds}");
                }
            }

            areasToUpdate.Clear();
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

        public void ProcessAligning(MT_Tools mode)
        {
            if (ToolState == ToolStates.Aligning && MT_Tool == mode)
            {
                StopTool();
            }
            else
            {
                StartTool(ToolStates.Aligning, mode);
            }
        }

        public bool StartTool(ToolStates newToolState, MT_Tools mode)
        {
            if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                StopCloning();
            }

            if (ToolState != ToolStates.Default && ToolState != ToolStates.Aligning && ToolState != ToolStates.ToolActive) return false;

            if (Action.selection.Count == 0) return false;

            SetToolState(newToolState, mode, 1);
            UIMoreTools.CheckCloseMenu();
            return true;
        }

        // Called when a tool might not be active
        public void StopTool()
        {
            if (ToolState != ToolStates.Aligning && ToolState != ToolStates.ToolActive) return;

            DeactivateTool();
        }

        public bool DeactivateTool()
        {
            if (MT_Tool == MT_Tools.MoveTo)
            {
                m_moveToPanel.Visible(false);
            }

            SetToolState();
            Action.UpdateArea(Action.GetTotalBounds(false));
            return false;
        }

        public void StartCloning()
        {
            lock (ActionQueue.instance)
            {
                if (ToolState != ToolStates.Default && ToolState != ToolStates.Aligning) return;

                if (Action.selection.Count > 0)
                {
                    CloneAction action = new CloneAction();

                    if (action.Count > 0)
                    {
                        UpdateSensitivityMode();

                        m_sensitivityTogglePosAbs = m_clickPositionAbs = action.center;

                        ActionQueue.instance.Push(action);

                        SetToolState(ToolStates.Cloning);
                        UIToolOptionPanel.RefreshCloneButton();
                        UIToolOptionPanel.RefreshAlignHeightButton();
                    }
                }
            }
        }

        public void StopCloning()
        {
            lock (ActionQueue.instance)
            {
                if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                {
                    ProcessSensitivityMode(false);

                    ActionQueue.instance.Undo();
                    ActionQueue.instance.Invalidate();
                    SetToolState();

                    UIToolOptionPanel.RefreshCloneButton();
                }
            }
        }

        public void StartBulldoze()
        {
            if (ToolState != ToolStates.Default) return;

            if (Action.selection.Count > 0)
            {
                lock (ActionQueue.instance)
                {
                    ActionQueue.instance.Push(new BulldozeAction());
                }
                m_nextAction = ToolAction.Do;
            }
        }

        public void StartReset()
        {
            if (ToolState != ToolStates.Default) return;

            if (Action.selection.Count > 0)
            {
                lock (ActionQueue.instance)
                {
                    ActionQueue.instance.Push(new ResetAction());
                }
                m_nextAction = ToolAction.Do;
            }
        }

        public static bool IsExportSelectionValid()
        {
            return CloneAction.GetCleanSelection(out Vector3 center).Count > 0;
        }

        public bool Export(string filename)
        {
            string path = Path.Combine(saveFolder, filename + ".xml");

            try
            {
                HashSet<Instance> selection = CloneAction.GetCleanSelection(out Vector3 center);

                if (selection.Count == 0) return false;

                Selection selectionState = new Selection();

                selectionState.center = center;
                selectionState.states = new InstanceState[selection.Count];

                int i = 0;
                foreach (Instance instance in selection)
                {
                    selectionState.states[i++] = instance.SaveToState();
                }

                Directory.CreateDirectory(saveFolder);

                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file !!!
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Selection));
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    xmlSerializer.Serialize(stream, selectionState, ns);
                }
            }
            catch(Exception e)
            {
                DebugUtils.Log("Couldn't export selection");
                DebugUtils.LogException(e);

                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Export failed", "The selection couldn't be exported to '" + path + "'\n\n" + e.Message, true);
                return false;
            }

            return true;
        }

        public void Import(string filename)
        {
            ImportImpl(filename, false);
        }

        public void Restore(string filename)
        {
            ImportImpl(filename, true);
        }

        private void ImportImpl(string filename, bool restore)
        {
            lock (ActionQueue.instance)
            {
                StopCloning();
                StopTool();

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Selection));
                Selection selectionState;

                string path = Path.Combine(saveFolder, filename + ".xml");

                try
                {
                    // Trying to Deserialize the file
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        selectionState = xmlSerializer.Deserialize(stream) as Selection;
                    }
                }
                catch (Exception e)
                {
                    // Couldn't Deserialize (XML malformed?)
                    DebugUtils.Log("Couldn't load file");
                    DebugUtils.LogException(e);

                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Import failed", "Couldn't load '" + path + "'\n\n" + e.Message, true);
                    return;
                }

                if (selectionState != null && selectionState.states != null && selectionState.states.Length > 0)
                {
                    HashSet<string> missingPrefabs = new HashSet<string>();

                    foreach (InstanceState state in selectionState.states)
                    {
                        if (state.Info.Prefab == null)
                        {
                            missingPrefabs.Add(state.prefabName);
                        }
                    }

                    if (missingPrefabs.Count > 0)
                    {
                        DebugUtils.Warning("Missing prefabs: " + string.Join(", ", missingPrefabs.ToArray()));

                        UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Assets missing", "The following assets are missing and will be ignored:\n\n" + string.Join("\n", missingPrefabs.ToArray()), false);

                    }

                    CloneAction action = new CloneAction(selectionState.states, selectionState.center);

                    if (action.Count > 0)
                    {
                        ActionQueue.instance.Push(action);

                        if (restore)
                        {
                            ActionQueue.instance.Do(); // For restore to position
                        }
                        else
                        {
                            SetToolState(ToolStates.Cloning); // For clone
                        }

                        UIToolOptionPanel.RefreshCloneButton();
                        UIToolOptionPanel.RefreshAlignHeightButton();
                    }
                }
            }
        }

        public void Delete(string filename)
        {
            try
            {
                string path = Path.Combine(saveFolder, filename + ".xml");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                DebugUtils.Log("Couldn't delete file");
                DebugUtils.LogException(ex);

                return;
            }
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

        protected static void ReleaseSegmentBlock(ushort segment, ref ushort segmentBlock)
        {
            if (segmentBlock != 0)
            {
                ZoneManager.instance.ReleaseBlock(segmentBlock);
                segmentBlock = 0;
            }
        }

        public static string InstanceIDDebug(InstanceID id)
        {
            return $"(B:{id.Building},P:{id.Prop},T:{id.Tree},N:{id.NetNode},S:{id.NetSegment},L:{id.NetLane})";
        }

        public static string InstanceIDDebug(Instance instance)
        {
            if (instance == null) return "(null instance)";
            return $"(B:{instance.id.Building},P:{instance.id.Prop},T:{instance.id.Tree},N:{instance.id.NetNode},S:{instance.id.NetSegment},L:{instance.id.NetLane})";
        }

        internal static HashSet<Bounds> MergeBounds(HashSet<Bounds> outerList)
        {
            HashSet<Bounds> innerList = new HashSet<Bounds>();
            HashSet<Bounds> newList = new HashSet<Bounds>();
            HashSet<Bounds> originalList = outerList;

            int c = 0;

            do
            {
                foreach (Bounds outer in outerList)
                {
                    //Color32 color = GetRandomDebugColor();
                    //AddDebugBox(outer, color);

                    bool merged = false;

                    float outerVolume = outer.size.x * outer.size.y * outer.size.z;
                    foreach (Bounds inner in innerList)
                    {
                        float separateVolume = (inner.size.x * inner.size.y * inner.size.z) + outerVolume;

                        Bounds encapsulated = inner;
                        encapsulated.Encapsulate(outer);
                        float encapsulateVolume = encapsulated.size.x * encapsulated.size.y * encapsulated.size.z;

                        if (!merged && encapsulateVolume < separateVolume)
                        {
                            newList.Add(encapsulated);
                            merged = true;
                        }
                        else
                        {
                            newList.Add(inner);
                        }
                    }
                    if (!merged)
                    {
                        newList.Add(outer);
                    }

                    innerList = new HashSet<Bounds>(newList);
                    newList.Clear();
                }

                if (outerList.Count <= innerList.Count)
                {
                    break;
                }
                outerList = new HashSet<Bounds>(innerList);
                innerList.Clear();

                if (c > 1000)
                {
                    Debug.Log($"Looped bounds-merge a thousand times");
                    break;
                }

                c++;
            }
            while (true);

            //foreach (Bounds b in innerList)
            //{
            //    b.Expand(4f);
            //    AddDebugBox(b, new Color32(255, 0, 0, 200));
            //}
            //Debug.Log($"\nStart:{originalList.Count}\nInner:{innerList.Count}");
            return innerList;
        }

        internal static void CleanGhostNodes()
        {
            if (!MoveItLoader.IsGameLoaded)
            {
                ExceptionPanel notLoaded = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                notLoaded.SetMessage("Not In-Game", "Use this button when in-game to remove ghost nodes (nodes with no segments attached, which were previously created by Move It)", false);
                return;
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            string message;
            int count = 0;

            for (ushort nodeId = 0; nodeId < Singleton<NetManager>.instance.m_nodes.m_buffer.Length; nodeId++)
            {
                NetNode node = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None) continue;
                if ((node.m_flags & NetNode.Flags.Untouchable) != NetNode.Flags.None) continue;
                bool hasSegments = false;

                for (int i = 0; i < 8; i++)
                {
                    if (node.GetSegment(i) > 0)
                    {
                        hasSegments = true;
                        break;
                    }
                }

                if (!hasSegments)
                {
                    count++;
                    Singleton<NetManager>.instance.ReleaseNode(nodeId);
                }
            }
            if (count > 0)
            {
                ActionQueue.instance.Clear();
                message = $"Removed {count} ghost node{(count == 1 ? "" : "s")}!";
            }
            else
            {
                message = "No ghost nodes found, nothing has been changed.";
            }
            panel.SetMessage("Removing Ghost Nodes", message, false);
        }

        internal static byte RandomByte(byte min, byte max)
        {
            return (byte)Rand.Next(min, max);
        }

        internal void ProcessMirror(AlignMirrorAction action)
        {
            StartCoroutine(ProcessMirrorIterate(action));
        }

        internal IEnumerator<object> ProcessMirrorIterate(AlignMirrorAction action)
        {
            const uint MaxAttempts = 1000_000;

            uint c = 0;
            while (c < MaxAttempts && POProcessing > 0)
            {
                c++;
                yield return new WaitForSeconds(0.05f);
            }

            if (c == MaxAttempts)
            {
                throw new Exception($"Failed to mirror PO");
            }

            action.DoProcess();
        }
    }
}
