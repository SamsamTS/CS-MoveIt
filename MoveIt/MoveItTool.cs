using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Aligning
        }

        public enum AlignModes
        {
            Off,
            Height,
            TerrainHeight,
            Inplace,
            Group,
            Random,
            Slope,
            SlopeNode
        }

        public const string settingsFileName = "MoveItTool";
        public static readonly string saveFolder = Path.Combine(DataLocation.localApplicationData, "MoveItExports");
        public const int UI_Filter_CB_Height = 25;

        public static MoveItTool instance;
        public static SavedBool hideTips = new SavedBool("hideTips", settingsFileName, false, true); 
        public static SavedBool autoCloseAlignTools = new SavedBool("autoCloseAlignTools", settingsFileName, false, true);
        public static SavedBool POOnlySelectedAreVisible = new SavedBool("POOnlySelectedAreVisible", settingsFileName, true, true);
        public static SavedBool POHighlightUnselected = new SavedBool("POHighlightUnselected", settingsFileName, true, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);
        public static SavedBool rmbCancelsCloning = new SavedBool("rmbCancelsCloning", settingsFileName, false, true);
        //public static SavedBool decalsAsSurfaces = new SavedBool("decalsAsSurfaces", settingsFileName, false, true);
        //public static SavedBool brushesAsSurfaces = new SavedBool("brushesAsSurfaces", settingsFileName, false, true);
        //public static SavedBool extraAsSurfaces = new SavedBool("extraAsSurfaces", settingsFileName, false, true);
        public static SavedBool fastMove = new SavedBool("fastMove", settingsFileName, false, true);
        public static SavedBool altSelectNodeBuildings = new SavedBool("altSelectNodeBuildings", settingsFileName, false, true);
        public static SavedBool altSelectSegmentNodes = new SavedBool("altSelectSegmentNodes", settingsFileName, true, true);
        public static SavedBool followTerrainModeEnabled = new SavedBool("followTerrainModeEnabled", settingsFileName, true, true);
        public static SavedBool showDebugPanel = new SavedBool("showDebugPanel", settingsFileName, false, true);
        public static SavedBool HidePO = new SavedBool("HidePO", settingsFileName, true, true);

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
        internal static DebugPanel debugPanel;

        public int segmentUpdateCountdown = -1;
        public HashSet<ushort> segmentsToUpdate = new HashSet<ushort>();

        public int aeraUpdateCountdown = -1;
        public HashSet<Bounds> aerasToUpdate = new HashSet<Bounds>();

        internal static Color m_hoverColor = new Color32(0, 181, 255, 255);
        internal static Color m_selectedColor = new Color32(95, 166, 0, 244);
        internal static Color m_moveColor = new Color32(125, 196, 30, 244);
        internal static Color m_removeColor = new Color32(255, 160, 47, 191);
        internal static Color m_despawnColor = new Color32(255, 160, 47, 191);
        internal static Color m_alignColor = new Color32(255, 255, 255, 244);
        internal static Color m_POhoverColor = new Color32(240, 140, 255, 240);
        internal static Color m_POselectedColor = new Color32(230, 130, 245, 140);
        internal static Color m_POdisabledColor = new Color32(150, 100, 160, 80);

        public static Shader shaderBlend = Shader.Find("Custom/Props/Decal/Blend");
        public static Shader shaderSolid = Shader.Find("Custom/Props/Decal/Solid");

        //internal static bool HidePO = true;
        internal static PO_Manager PO;

        private const float XFACTOR = 0.263671875f;
        private const float YFACTOR = 0.015625f;
        private const float ZFACTOR = 0.263671875f;

        private ToolStates m_toolState;
        public ToolStates ToolState
        {
            get => m_toolState;
            set
            {
                m_toolState = value;
                if (debugPanel != null)
                {
                    debugPanel.Update();
                }
            }
        }
        private AlignModes m_alignMode;
        public AlignModes AlignMode
        { 
            get => m_alignMode;
            set
            {
                m_alignMode = value;
                debugPanel.Update();
            }
        }
        private ushort m_alignToolPhase;
        public ushort AlignToolPhase
        {
            get => m_alignToolPhase;
            set
            {
                m_alignToolPhase = value;
                debugPanel.Update();
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

        public HashSet<Instance> selection
        {
            get { return Action.selection; }
        }

        private UIMoveItButton m_button;
        private UIComponent m_pauseMenu;

        private Quad3 m_selection;
        public Instance m_hoverInstance;
        private Instance m_lastInstance;
        private HashSet<Instance> m_marqueeInstances;

        private Vector3 m_startPosition;
        private Vector3 m_mouseStartPosition;

        private float m_mouseStartX;
        private float m_startAngle;

        private NetSegment m_segmentGuide;

        private bool m_prevRenderZones;
        private ToolBase m_prevTool;

        private static InfoManager.InfoMode m_prevInfoMode;

        private long m_keyTime;
        private long m_rightClickTime;
        private long m_leftClickTime;

        public ToolAction m_nextAction = ToolAction.None;

        protected override void Awake()
        {
            ActionQueue.instance = new ActionQueue();

            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;

            followTerrain = followTerrainModeEnabled;

            PO = new PO_Manager();
        }

        protected override void OnEnable()
        {
            if (UIToolOptionPanel.instance == null)
            {
                UIComponent TSBar = UIView.GetAView().FindUIComponent<UIComponent>("TSBar");
                TSBar.AddUIComponent<UIToolOptionPanel>();
            }
            else
            {
                UIToolOptionPanel.instance.isVisible = true;
            }

            if (!hideTips && UITipsWindow.instance != null)
            {
                UITipsWindow.instance.isVisible = true;
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

            if (!HidePO && PO.Active)
            {
                PO.ToolEnabled();
                ActionQueue.instance.Push(new TransformAction());
            }

            //string msg = $"Selected:{Action.selection.Count} (before PO refresh:{oldSelectionCount})\n";
            //foreach (Instance i in Action.selection)
            //{
            //    msg += $"{i.GetType()} {InstanceIDDebug(i)}\n";
            //    if (i is MoveableProc mpo)
            //    {
            //        msg += $"    {mpo.m_procObj.DebugQuaternion()}\n";
            //    }
            //}
            //msg += "Queue: " + ActionQueue.instance.DebugQueue();
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
                    UpdateAreas();
                }

                ToolState = ToolStates.Default;

                if (UITipsWindow.instance != null)
                {
                    UITipsWindow.instance.isVisible = false;
                }

                if (UIToolOptionPanel.instance != null)
                {
                    UIToolOptionPanel.instance.isVisible = false;
                }

                TerrainManager.instance.RenderZones = m_prevRenderZones;
                InfoManager.instance.SetCurrentMode(m_prevInfoMode, InfoManager.instance.CurrentSubMode);

                if (m_toolController.NextTool == null && m_prevTool != null && m_prevTool != this)
                {
                    m_prevTool.enabled = true;
                }
                m_prevTool = null;

                UIToolOptionPanel.RefreshAlignHeightButton();
                UIToolOptionPanel.RefreshCloneButton();
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (ToolState == ToolStates.Default || ToolState == ToolStates.Aligning)
            {
                // Highlight all PO
                if (!HidePO && PO.Active && POHighlightUnselected)
                {
                    foreach (IPO_Object obj in PO.Objects)
                    {
                        obj.RenderOverlay(cameraInfo, m_POdisabledColor);
                    }
                }

                if (Action.selection.Count > 0)
                {
                    // Highlight Selected Items
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid && instance != m_hoverInstance)
                        {
                            instance.RenderOverlay(cameraInfo, m_selectedColor, m_despawnColor);
                        }
                    }
                    if (ToolState == ToolStates.Aligning && AlignMode == AlignModes.Slope && AlignToolPhase == 2)
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
                    if (ToolState == ToolStates.Aligning)
                    {
                        color = m_alignColor;
                    }
                    else if (Action.selection.Contains(m_hoverInstance))
                    {
                        if(Event.current.shift)
                        {
                            color = m_removeColor;
                        }
                        else
                        {
                            color = m_moveColor;
                        }
                    }
                    m_hoverInstance.RenderOverlay(cameraInfo, color, m_despawnColor);
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

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);

                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, -1f, 1280f, false, true);

                    if (snapping && m_segmentGuide.m_startNode != 0 && m_segmentGuide.m_endNode != 0)
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
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

                //UnityEngine.Debug.Log($"a:{m_selection.a} c:{m_selection.c}");
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

                Matrix4x4 matrix4x = default(Matrix4x4);
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.savedStates)
                {
                    state.instance.RenderCloneOverlay(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, m_hoverColor);
                }
            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;

                Matrix4x4 matrix4x = default(Matrix4x4);
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.savedStates)
                {
                    state.instance.RenderCloneGeometry(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, m_hoverColor);
                }
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;

                foreach (InstanceState state in action.savedStates)
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

                    bool inputHeld = m_keyTime != 0 || m_leftClickTime != 0 || m_rightClickTime != 0;

                    if (segmentUpdateCountdown == 0)
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

                    if (!inputHeld && segmentUpdateCountdown >= 0)
                    {
                        segmentUpdateCountdown--;
                    }

                    if (aeraUpdateCountdown == 0)
                    {
                        UpdateAreas();
                    }

                    if (!inputHeld && aeraUpdateCountdown >= 0)
                    {
                        aeraUpdateCountdown--;
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
            //string msg = "";
            foreach (Bounds bounds in MergeBounds(aerasToUpdate))
            {
                //msg += $"SimStep: {bounds}\n";

                VehicleManager.instance.UpdateParkedVehicles(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
            }
            //Debug.Log(msg);

            aerasToUpdate.Clear();
        }

        public override ToolErrors GetErrors()
        {
            return ToolErrors.None;
        }

        public void ProcessAligning(AlignModes mode)
        {
            if (ToolState == ToolStates.Aligning && AlignMode == mode)
            {
                StopAligning();
            }
            else
            {
                StartAligning(mode);
            }
        }

        public void StartAligning(AlignModes mode)
        {
            if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
            {
                StopCloning();
            }

            if (ToolState != ToolStates.Default) return;

            if (Action.selection.Count > 0)
            {
                ToolState = ToolStates.Aligning;
                AlignMode = mode;
                AlignToolPhase = 1;
            }

            UIAlignTools.UpdateAlignTools();
        }

        public void StopAligning()
        {
            AlignMode = AlignModes.Off;
            AlignToolPhase = 0;
            if (ToolState == ToolStates.Aligning)
            {
                ToolState = ToolStates.Default;
            }
            UIAlignTools.UpdateAlignTools();
        }

        public bool DeactivateAlignTool(bool switchMode = true)
        {
            //Debug.Log($"DEACTIVATE (sM:{switchMode}) (phase was {m_alignToolPhase})");
            if (switchMode)
            {
                AlignMode = AlignModes.Off;
                ToolState = ToolStates.Default;
                AlignToolPhase = 0;
            }

            UIAlignTools.UpdateAlignTools();
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
                        ActionQueue.instance.Push(action);

                        ToolState = ToolStates.Cloning;
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
                    ActionQueue.instance.Undo();
                    ActionQueue.instance.Invalidate();
                    ToolState = ToolStates.Default;

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
                    selectionState.states[i++] = instance.GetState();
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
            lock (ActionQueue.instance)
            {
                StopCloning();
                StopAligning();

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
                        if (state.Info == null)
                        {
                            missingPrefabs.Add(state.prefabName);
                        }
                    }

                    if (missingPrefabs.Count > 0)
                    {
                        DebugUtils.Warning("Missing prefabs: " + string.Join(", ", missingPrefabs.ToArray()));
                        
                        UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Assets missing", "The following assets are missing and will be ignored:\n\n"+ string.Join("\n", missingPrefabs.ToArray()), false);

                    }
                    CloneAction action = new CloneAction(selectionState.states, selectionState.center);

                    if (action.Count > 0)
                    {
                        ActionQueue.instance.Push(action);

                        ToolState = ToolStates.Cloning; // For clone
                        //ActionQueue.instance.Do(); // For paste

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

        internal static Vector3 RaycastMouseLocation(Ray mouseRay)
        {
            RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
            input.m_ignoreTerrain = false;
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

            int c = 0;
            foreach (Bounds b in outerList)
            {
                b.Expand(64f);
            }

            do
            {
                foreach (Bounds outer in outerList)
                {
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

            return innerList;
        }
    }
}
