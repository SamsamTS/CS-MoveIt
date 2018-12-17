using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ColossalFramework.IO;
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

        public enum ToolState
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
            Inplace,
            Group,
            Random,
            Slope
        }

        public const string settingsFileName = "MoveItTool";
        public static readonly string saveFolder = Path.Combine(DataLocation.localApplicationData, "MoveItExports");
        public const int UI_Filter_CB_Height = 25;

        public static MoveItTool instance;
        public static SavedBool hideTips = new SavedBool("hideTips", settingsFileName, false, true); 
        public static SavedBool autoCloseAlignTools = new SavedBool("autoCloseAlignTools", settingsFileName, false, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);
        public static SavedBool rmbCancelsCloning = new SavedBool("rmbCancelsCloning", settingsFileName, false, true);
        public static SavedBool decalsAsSurfaces = new SavedBool("decalsAsSurfaces", settingsFileName, false, true);
        public static SavedBool brushesAsSurfaces = new SavedBool("brushesAsSurfaces", settingsFileName, false, true);
        public static SavedBool extraAsSurfaces = new SavedBool("extraAsSurfaces", settingsFileName, false, true);

        public static bool filterBuildings = true;
        public static bool filterProps = true;
        public static bool filterDecals = true;
        public static bool filterSurfaces = true;
        public static bool filterTrees = true;
        public static bool filterNodes = true;
        public static bool filterSegments = true;
        public static bool filterNetworks = false;

        public static bool followTerrain = true;

        public static bool marqueeSelection = false;
        
        public static StepOver stepOver;

        public int segmentUpdateCountdown = -1;
        public HashSet<ushort> segmentsToUpdate = new HashSet<ushort>();

        public int aeraUpdateCountdown = -1;
        public HashSet<Bounds> aerasToUpdate = new HashSet<Bounds>();

        private static Color m_hoverColor = new Color32(0, 181, 255, 255);
        private static Color m_selectedColor = new Color32(95, 166, 0, 244);
        private static Color m_moveColor = new Color32(125, 196, 30, 244);
        private static Color m_removeColor = new Color32(255, 160, 47, 191);
        private static Color m_despawnColor = new Color32(255, 160, 47, 191);
        private static Color m_alignColor = new Color32(255, 255, 255, 244);

        public static Shader shaderBlend = Shader.Find("Custom/Props/Decal/Blend");
        public static Shader shaderSolid = Shader.Find("Custom/Props/Decal/Solid");

        private const float XFACTOR = 0.263671875f;
        private const float YFACTOR = 0.015625f;
        private const float ZFACTOR = 0.263671875f;

        public ToolState toolState;
        public AlignModes alignMode;

        private bool m_snapping = false;
        public bool snapping
        {
            get
            {
                if (toolState == ToolState.MouseDragging ||
                    toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
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

            m_toolController = GameObject.FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;
        }

        protected override void OnToolGUI(Event e)
        {
            if (UIView.HasModalInput() || UIView.HasInputFocus()) return;

            lock (ActionQueue.instance)
            {
                if (toolState == ToolState.Default)
                {
                    if (OptionsKeymapping.undo.IsPressed(e))
                    {
                        m_nextAction = ToolAction.Undo;
                    }
                    else if (OptionsKeymapping.redo.IsPressed(e))
                    {
                        m_nextAction = ToolAction.Redo;
                    }
                }

                if (OptionsKeymapping.copy.IsPressed(e))
                {
                    if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
                    {
                        StopCloning();
                    }
                    else
                    {
                        StartCloning();
                    }
                }
                else if (OptionsKeymapping.alignHeights.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Height);
                }
                else if (OptionsKeymapping.alignInplace.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Inplace);
                }
                else if (OptionsKeymapping.alignGroup.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Group);
                }
                else if (OptionsKeymapping.alignRandom.IsPressed(e))
                {
                    alignMode = AlignModes.Random;

                    if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
                    {
                        StopCloning();
                    }

                    AlignRandomAction action = new AlignRandomAction();
                    action.followTerrain = followTerrain;
                    ActionQueue.instance.Push(action);
                    ActionQueue.instance.Do();
                    DeactivateAlignTool();
                }

                if (toolState == ToolState.Cloning)
                {
                    if (ProcessMoveKeys(e, out Vector3 direction, out float angle))
                    {
                        CloneAction action = ActionQueue.instance.current as CloneAction;

                        action.moveDelta.y += direction.y * YFACTOR;
                        action.angleDelta += angle;
                    }
                }
                else if (toolState == ToolState.Default && Action.selection.Count > 0)
                {
                    // TODO: if no selection select hovered instance
                    // Or not. Nobody asked for getting it back

                    if (ProcessMoveKeys(e, out Vector3 direction, out float angle))
                    {
                        if (!(ActionQueue.instance.current is TransformAction action))
                        {
                            action = new TransformAction();
                            ActionQueue.instance.Push(action);
                        }

                        if (direction != Vector3.zero)
                        {
                            direction.x = direction.x * XFACTOR;
                            direction.y = direction.y * YFACTOR;
                            direction.z = direction.z * ZFACTOR;

                            if (!useCardinalMoves)
                            {
                                Matrix4x4 matrix4x = default(Matrix4x4);
                                matrix4x.SetTRS(Vector3.zero, Quaternion.AngleAxis(Camera.main.transform.localEulerAngles.y, Vector3.up), Vector3.one);

                                direction = matrix4x.MultiplyVector(direction);
                            }
                        }

                        action.moveDelta += direction;
                        action.angleDelta += angle;
                        action.followTerrain = followTerrain;

                        m_nextAction = ToolAction.Do;
                    }
                }
            }
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
        }

        protected override void OnDisable()
        {
            lock (ActionQueue.instance)
            {
                if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
                {
                    // Cancel cloning
                    ActionQueue.instance.Undo();
                    ActionQueue.instance.Invalidate();
                }

                toolState = ToolState.Default;

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
            if (toolState == ToolState.Default || toolState == ToolState.Aligning)
            {
                if (Action.selection.Count > 0)
                {
                    foreach (Instance instance in Action.selection)
                    {
                        if (instance.isValid && instance != m_hoverInstance)
                        {
                            instance.RenderOverlay(cameraInfo, m_selectedColor, m_despawnColor);
                        }
                    }

                    Vector3 center = Action.GetCenter();
                    center.y = TerrainManager.instance.SampleRawHeightSmooth(center);

                    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, -1f, 1280f, false, true);
                }

                if (m_hoverInstance != null && m_hoverInstance.isValid)
                {
                    Color color = m_hoverColor;
                    if (toolState == ToolState.Aligning)
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
            else if (toolState == ToolState.MouseDragging)
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
            else if (toolState == ToolState.DrawingSelection)
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
            else if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
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
            if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;

                Matrix4x4 matrix4x = default(Matrix4x4);
                matrix4x.SetTRS(action.center + action.moveDelta, Quaternion.AngleAxis(action.angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                foreach (InstanceState state in action.savedStates)
                {
                    state.instance.RenderCloneGeometry(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, m_hoverColor);
                }
            }
        }

        public bool DeactivateAlignTool(bool switchMode = true)
        {
            if (switchMode)
            {
                alignMode = AlignModes.Off;
                toolState = ToolState.Default;
            }

            UIAlignTools.UpdateAlignTools();
            Action.UpdateArea(Action.GetTotalBounds(false));
            return false;
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
                        foreach (Bounds bounds in aerasToUpdate)
                        {
                            bounds.Expand(64f);
                            VehicleManager.instance.UpdateParkedVehicles(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                        }

                        aerasToUpdate.Clear();
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

        public override ToolErrors GetErrors()
        {
            return ToolErrors.None;
        }

        public void ProcessAligning(AlignModes mode)
        {
            if (toolState == ToolState.Aligning && alignMode == mode)
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
            if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
            {
                StopCloning();
            }

            if (toolState != ToolState.Default) return;

            if (Action.selection.Count > 0)
            {
                toolState = ToolState.Aligning;
                alignMode = mode;
            }

            UIAlignTools.UpdateAlignTools();
        }

        public void StopAligning()
        {
            Debug.Log($"tS:{toolState}, aM:{alignMode}");
            alignMode = AlignModes.Off;
            if (toolState == ToolState.Aligning)
            {
                toolState = ToolState.Default;
            }
            UIAlignTools.UpdateAlignTools();
        }

        public void StartCloning()
        {
            lock (ActionQueue.instance)
            {
                if (toolState != ToolState.Default && toolState != ToolState.Aligning) return;

                if (Action.selection.Count > 0)
                {
                    CloneAction action = new CloneAction();

                    if (action.Count > 0)
                    {
                        ActionQueue.instance.Push(action);

                        toolState = ToolState.Cloning;
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
                if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
                {
                    ActionQueue.instance.Undo();
                    ActionQueue.instance.Invalidate();
                    toolState = ToolState.Default;

                    UIToolOptionPanel.RefreshCloneButton();
                }
            }
        }

        public void StartBulldoze()
        {
            if (toolState != ToolState.Default) return;

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
                        if (state.info == null)
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

                        toolState = ToolState.Cloning;
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

        private Vector3 RaycastMouseLocation(Ray mouseRay)
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
    }
}
