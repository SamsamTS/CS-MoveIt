using ICities;
using UnityEngine;

using System;
using System.Diagnostics;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;

namespace MoveIt
{
    public class MoveItTool : TransportTool
    {
        private enum ToolAction
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
            AligningHeights
        }

        public const string settingsFileName = "MoveItTool";

        public static MoveItTool instance;
        public static SavedBool hideTips = new SavedBool("hideTips", settingsFileName, false, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);
        public static SavedBool rmbCancelsCloning = new SavedBool("rmbCancelsCloning", settingsFileName, false, true);

        public static bool followTerrain = true;

        public static bool marqueeSelection = false;

        public static bool filterBuildings = true;
        public static bool filterProps = true;
        public static bool filterDecals = true;
        public static bool filterTrees = true;
        public static bool filterNodes = true;
        public static bool filterSegments = true;

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

        private UIMoveItButton m_button;

        private Quad3 m_selection;
        private Instance m_hoverInstance;
        private Instance m_lastInstance;
        private HashSet<Instance> m_marqueeInstances;

        private Vector3 m_startPosition;
        private Vector3 m_mouseStartPosition;

        private float m_mouseStartX;
        private float m_startAngle;

        private NetSegment m_segmentGuide;

        private bool m_prevRenderZones;
        private ToolBase m_prevTool;

        private long m_keyTime;
        private long m_rightClickTime;
        private long m_leftClickTime;

        private ToolAction m_nextAction = ToolAction.None;

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
                    if (toolState == ToolState.AligningHeights)
                    {
                        StopAligningHeights();
                    }
                    else
                    {
                        StartAligningHeights();
                    }
                }

                if (toolState == ToolState.Cloning)
                {
                    Vector3 direction;
                    float angle;

                    if (ProcessMoveKeys(e, out direction, out angle))
                    {
                        CloneAction action = ActionQueue.instance.current as CloneAction;

                        action.moveDelta.y += direction.y * YFACTOR;
                        action.angleDelta += angle;
                    }
                }
                else if (toolState == ToolState.Default && Action.selection.Count > 0)
                {
                    // TODO: if no selection select hovered instance

                    Vector3 direction;
                    float angle;

                    if (ProcessMoveKeys(e, out direction, out angle))
                    {
                        TransformAction action = ActionQueue.instance.current as TransformAction;
                        if (action == null)
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

            if (!MoveItTool.hideTips && UITipsWindow.instance != null)
            {
                UITipsWindow.instance.isVisible = true;
                // TODO: ??? Cause crashes ???
                //UITipsWindow.instance.NextTip();
            }

            InfoManager.InfoMode infoMode = InfoManager.instance.CurrentMode;
            InfoManager.SubInfoMode subInfoMode = InfoManager.instance.CurrentSubMode;

            m_prevRenderZones = TerrainManager.instance.RenderZones;
            m_prevTool = m_toolController.CurrentTool;

            m_toolController.CurrentTool = this;

            InfoManager.instance.SetCurrentMode(infoMode, subInfoMode);
            TerrainManager.instance.RenderZones = true;
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

                if (m_toolController.NextTool != null && m_toolController.NextTool.GetType() == typeof(DefaultTool))
                {
                    DebugUtils.Log("Escape: " + toolState);

                    // Escape pressed
                    if (toolState != ToolState.Default && toolState != ToolState.MouseDragging)
                    {
                        toolState = ToolState.Default;

                        SimulationManager.instance.AddAction(delegate
                        {
                            m_toolController.CurrentTool = instance;
                        });

                        UIToolOptionPanel.RefreshAlignHeightButton();
                        UIToolOptionPanel.RefreshCloneButton();
                        return;
                    }
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
            if (toolState == ToolState.Default || toolState == ToolState.AligningHeights)
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
                    if (toolState == ToolState.AligningHeights)
                    {
                        color = m_alignColor;
                    }
                    else if (Action.selection.Contains(m_hoverInstance) && Event.current.shift)
                    {
                        color = m_removeColor;
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

                RenderManager.instance.OverlayEffect.DrawQuad(cameraInfo, color, m_selection, -1f, 1280f, false, true);

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
                matrix4x.SetTRS(action.center, Quaternion.AngleAxis(action.angleDelta * 57.29578f, Vector3.down), Vector3.one);

                HashSet<InstanceState> newStates = action.CalculateStates(action.moveDelta, action.angleDelta, action.center, followTerrain);

                foreach (InstanceState state in newStates)
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
                matrix4x.SetTRS(action.center, Quaternion.AngleAxis(action.angleDelta * 57.29578f, Vector3.down), Vector3.one);

                HashSet<InstanceState> newStates = action.CalculateStates(action.moveDelta, action.angleDelta, action.center, followTerrain);

                foreach (InstanceState state in newStates)
                {
                    state.instance.RenderCloneGeometry(state, ref matrix4x, action.moveDelta, action.angleDelta, action.center, followTerrain, cameraInfo, m_hoverColor);
                }
            }
        }

        protected override void OnToolUpdate()
        {
            if (m_nextAction != ToolAction.None) return;

            lock (ActionQueue.instance)
            {
                bool isInsideUI = this.m_toolController.IsInsideUI;

                if (m_leftClickTime == 0 && Input.GetMouseButton(0))
                {
                    if (!isInsideUI)
                    {
                        m_leftClickTime = Stopwatch.GetTimestamp();
                        OnLeftMouseDown();
                    }
                }

                if (m_leftClickTime != 0)
                {
                    long elapsed = ElapsedMilliseconds(m_leftClickTime);

                    if (!Input.GetMouseButton(0))
                    {
                        m_leftClickTime = 0;

                        if (elapsed < 200)
                        {
                            OnLeftClick();
                        }
                        else
                        {
                            OnLeftDragStop();
                        }

                        OnLeftMouseUp();
                    }
                    else if (elapsed >= 200)
                    {
                        OnLeftDrag();
                    }
                }

                if (m_rightClickTime == 0 && Input.GetMouseButton(1))
                {
                    if (!isInsideUI)
                    {
                        m_rightClickTime = Stopwatch.GetTimestamp();
                        OnRightMouseDown();
                    }
                }

                if (m_rightClickTime != 0)
                {
                    long elapsed = ElapsedMilliseconds(m_rightClickTime);

                    if (!Input.GetMouseButton(1))
                    {
                        m_rightClickTime = 0;

                        if (elapsed < 200)
                        {
                            OnRightClick();
                        }
                        else
                        {
                            OnRightDragStop();
                        }

                        OnRightMouseUp();
                    }
                    else if (elapsed >= 200)
                    {
                        OnRightDrag();
                    }
                }

                if (!isInsideUI && Cursor.visible)
                {
                    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                    m_hoverInstance = null;
                    m_marqueeInstances = null;
                    m_segmentGuide = default(NetSegment);

                    switch (toolState)
                    {
                        case ToolState.Default:
                        case ToolState.AligningHeights:
                            {
                                RaycastHoverInstance(mouseRay);
                                break;
                            }
                        case ToolState.MouseDragging:
                            {
                                TransformAction action = ActionQueue.instance.current as TransformAction;

                                Vector3 newMove = action.moveDelta;

                                if (m_leftClickTime > 0)
                                {
                                    float y = action.moveDelta.y;
                                    newMove = m_startPosition + RaycastMouseLocation(mouseRay) - m_mouseStartPosition;
                                    newMove.y = y;
                                }

                                float newAngle = action.angleDelta;

                                if (m_rightClickTime > 0)
                                {
                                    newAngle = ushort.MaxValue * 9.58738E-05f * (Input.mousePosition.x - m_mouseStartX) / Screen.width;
                                    if (Event.current.control)
                                    {
                                        float quarterPI = Mathf.PI / 4;
                                        newAngle = quarterPI * Mathf.Round(newAngle / quarterPI);
                                    }
                                    newAngle += m_startAngle;
                                }

                                if (snapping)
                                {
                                    newMove = GetSnapDelta(newMove, action.angleDelta, action.center, out action.autoCurve);

                                    if (action.autoCurve)
                                    {
                                        action.segmentCurve = m_segmentGuide;
                                    }
                                }
                                else
                                {
                                    action.autoCurve = false;
                                }

                                if (action.moveDelta != newMove || action.angleDelta != newAngle)
                                {
                                    action.moveDelta = newMove;
                                    action.angleDelta = newAngle;
                                    action.followTerrain = followTerrain;
                                    m_nextAction = ToolAction.Do;
                                }

                                UIToolOptionPanel.RefreshSnapButton();
                                break;
                            }
                        case ToolState.Cloning:
                            {
                                if (m_rightClickTime != 0) break;

                                CloneAction action = ActionQueue.instance.current as CloneAction;

                                float y = action.moveDelta.y;
                                Vector3 newMove = RaycastMouseLocation(mouseRay) - action.center;
                                newMove.y = y;

                                if (snapping)
                                {
                                    bool autoCurve;
                                    newMove = GetSnapDelta(newMove, action.angleDelta, action.center, out autoCurve);
                                }

                                if (action.moveDelta != newMove)
                                {
                                    action.moveDelta = newMove;
                                }

                                UIToolOptionPanel.RefreshSnapButton();
                                break;
                            }
                        case ToolState.RightDraggingClone:
                            {
                                CloneAction action = ActionQueue.instance.current as CloneAction;

                                float newAngle = ushort.MaxValue * 9.58738E-05f * (Input.mousePosition.x - m_mouseStartX) / Screen.width;
                                if (Event.current.control)
                                {
                                    float quarterPI = Mathf.PI / 4;
                                    newAngle = quarterPI * Mathf.Round(newAngle / quarterPI);
                                }
                                newAngle += m_startAngle;

                                if (action.angleDelta != newAngle)
                                {
                                    action.angleDelta = newAngle;
                                }

                                UIToolOptionPanel.RefreshSnapButton();
                                break;
                            }
                        case ToolState.DrawingSelection:
                            {
                                m_marqueeInstances = GetMarqueeList(mouseRay);
                                break;
                            }
                    }
                }
            }
        }

        protected override void OnToolLateUpdate()
        { }

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
                }
                catch (Exception e)
                {
                    DebugUtils.Log("SimulationStep failed");
                    DebugUtils.LogException(e);
                }

                m_nextAction = ToolAction.None;
            }
        }

        public override ToolBase.ToolErrors GetErrors()
        {
            return ToolErrors.None;
        }

        public void StartAligningHeights()
        {
            if (toolState == ToolState.Cloning || toolState == ToolState.RightDraggingClone)
            {
                StopCloning();
            }

            if (toolState != ToolState.Default) return;

            if (Action.selection.Count > 0)
            {
                toolState = ToolState.AligningHeights;
            }

            UIToolOptionPanel.RefreshAlignHeightButton();
        }

        public void StopAligningHeights()
        {
            if (toolState == ToolState.AligningHeights)
            {
                toolState = ToolState.Default;
                UIToolOptionPanel.RefreshAlignHeightButton();
            }
        }

        public void StartCloning()
        {
            lock (ActionQueue.instance)
            {
                if (toolState != ToolState.Default && toolState != ToolState.AligningHeights) return;

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

        private void OnLeftMouseDown()
        {
            DebugUtils.Log("OnLeftMouseDown: " + toolState);

            if (toolState == ToolState.Default)
            {
                if (marqueeSelection && (m_hoverInstance == null || !Action.selection.Contains(m_hoverInstance)))
                {

                    m_selection = default(Quad3);
                    m_marqueeInstances = null;

                    toolState = ToolState.DrawingSelection;
                }

                m_lastInstance = m_hoverInstance;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (toolState == ToolState.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startPosition = action.moveDelta;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (toolState == ToolState.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                action.followTerrain = followTerrain;

                toolState = ToolState.Default;
                m_nextAction = ToolAction.Do;
            }
        }

        private void OnRightMouseDown()
        {
            DebugUtils.Log("OnRightMouseDown: " + toolState);

            if (toolState == ToolState.Default)
            {
                m_mouseStartX = Input.mousePosition.x;
            }
            else if (toolState == ToolState.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
            else if (toolState == ToolState.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
        }

        private void OnLeftMouseUp()
        {
            DebugUtils.Log("OnLeftMouseUp: " + toolState);

            if (toolState == ToolState.DrawingSelection)
            {
                toolState = ToolState.Default;

                Event e = Event.current;

                if (m_marqueeInstances == null ||
                    m_marqueeInstances.Count == 0 ||
                    (e.alt && !Action.selection.Overlaps(m_marqueeInstances)) ||
                    (e.shift && Action.selection.IsSupersetOf(m_marqueeInstances))
                    ) return;

                SelectAction action = ActionQueue.instance.current as SelectAction;
                if (action == null)
                {
                    action = new SelectAction(e.shift);
                    ActionQueue.instance.Push(action);
                }
                else
                {
                    ActionQueue.instance.Invalidate();
                }

                if (e.alt)
                {
                    Action.selection.ExceptWith(m_marqueeInstances);
                }
                else
                {
                    if (!e.shift)
                    {
                        Action.selection.Clear();
                    }
                    Action.selection.UnionWith(m_marqueeInstances);
                }

                m_marqueeInstances = null;
            }
        }

        private void OnRightMouseUp()
        { }

        private void OnRightClick()
        {
            DebugUtils.Log("OnRightClick: " + toolState);

            if (toolState == ToolState.Default)
            {
                SelectAction action = ActionQueue.instance.current as SelectAction;
                if (action == null)
                {
                    action = new SelectAction();
                    ActionQueue.instance.Push(action);
                }
                else
                {
                    Action.selection.Clear();
                    ActionQueue.instance.Invalidate();
                }
            }
            else if (toolState == ToolState.Cloning)
            {
                if (rmbCancelsCloning.value)
                {
                    StopCloning();
                }
                else
                {
                    // Rotate 45° clockwise
                    CloneAction action = ActionQueue.instance.current as CloneAction;
                    action.angleDelta -= Mathf.PI / 4;
                }
            }
            else if (toolState != ToolState.MouseDragging)
            {
                toolState = ToolState.Default;
            }
        }

        private void OnLeftClick()
        {
            DebugUtils.Log("OnLeftClick: " + toolState);

            if (toolState == ToolState.Default || (toolState == ToolState.DrawingSelection && (m_marqueeInstances == null || m_marqueeInstances.Count == 0)))
            {
                Event e = Event.current;
                if (m_hoverInstance == null) return;

                SelectAction action = ActionQueue.instance.current as SelectAction;
                if (action == null)
                {
                    action = new SelectAction(e.shift);
                    ActionQueue.instance.Push(action);
                }
                else
                {
                    ActionQueue.instance.Invalidate();
                }

                if (e.shift)
                {
                    if (Action.selection.Contains(m_hoverInstance))
                    {
                        Action.selection.Remove(m_hoverInstance);
                    }
                    else
                    {
                        Action.selection.Add(m_hoverInstance);
                    }
                }
                else
                {
                    Action.selection.Clear();
                    Action.selection.Add(m_hoverInstance);
                }
            }
            else if (toolState == ToolState.AligningHeights)
            {
                toolState = ToolState.Default;

                AlignHeightAction action = new AlignHeightAction();
                action.height = m_hoverInstance.position.y;
                ActionQueue.instance.Push(action);

                m_nextAction = ToolAction.Do;

                UIToolOptionPanel.RefreshAlignHeightButton();
            }
        }

        private void OnLeftDrag()
        {
            DebugUtils.Log("OnLeftDrag: " + toolState);

            if (toolState == ToolState.Default)
            {
                if (m_lastInstance == null) return;

                TransformAction action;
                if (Action.selection.Contains(m_lastInstance))
                {
                    action = ActionQueue.instance.current as TransformAction;
                    if (action == null)
                    {
                        action = new TransformAction();
                        ActionQueue.instance.Push(action);
                    }
                }
                else
                {
                    ActionQueue.instance.Push(new SelectAction());
                    Action.selection.Add(m_lastInstance);

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_startPosition = action.moveDelta;

                toolState = ToolState.MouseDragging;
            }
        }

        private void OnRightDrag()
        {
            DebugUtils.Log("OnRightDrag: " + toolState);

            if (toolState == ToolState.Default)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                if (action == null)
                {
                    if (Action.selection.Count == 0) return;

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_startAngle = action.angleDelta;
                toolState = ToolState.MouseDragging;
            }
            else if (toolState == ToolState.Cloning)
            {
                toolState = ToolState.RightDraggingClone;
            }
        }

        private void OnLeftDragStop()
        {
            DebugUtils.Log("OnLeftDragStop: " + toolState);

            if (toolState == ToolState.MouseDragging && m_rightClickTime == 0)
            {
                toolState = ToolState.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
        }

        private void OnRightDragStop()
        {
            DebugUtils.Log("OnRightDragStop: " + toolState);

            if (toolState == ToolState.MouseDragging && m_leftClickTime == 0)
            {
                toolState = ToolState.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
            else if (toolState == ToolState.RightDraggingClone)
            {
                toolState = ToolState.Cloning;
            }
        }

        private void RaycastHoverInstance(Ray mouseRay)
        {
            RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
            RaycastOutput output;

            input.m_netService.m_itemLayers = GetItemLayers();
            input.m_ignoreTerrain = true;

            input.m_ignoreSegmentFlags = NetSegment.Flags.None;
            input.m_ignoreBuildingFlags = Building.Flags.None;
            input.m_ignorePropFlags = PropInstance.Flags.None;
            input.m_ignoreTreeFlags = TreeInstance.Flags.None;

            m_hoverInstance = null;

            if (ToolBase.RayCast(input, out output))
            {
                InstanceID id = default(InstanceID);

                if (output.m_netSegment != 0)
                {
                    NetManager netManager = NetManager.instance;

                    ushort building = NetSegment.FindOwnerBuilding(output.m_netSegment, 363f);

                    if (building != 0)
                    {
                        id.Building = Building.FindParentBuilding(building);
                        if (id.Building == 0) id.Building = building;
                    }
                    else
                    {
                        ushort startNode = netManager.m_segments.m_buffer[output.m_netSegment].m_startNode;
                        ushort endNode = netManager.m_segments.m_buffer[output.m_netSegment].m_endNode;
                        float sqDist = netManager.m_segments.m_buffer[output.m_netSegment].Info.m_halfWidth;
                        sqDist = sqDist * sqDist;

                        Vector2 mousePos = VectorUtils.XZ(output.m_hitPos);
                        Vector2 testPos = VectorUtils.XZ(netManager.m_nodes.m_buffer[startNode].m_position);

                        if ((mousePos - testPos).sqrMagnitude < sqDist)
                        {
                            id.NetNode = startNode;
                            m_hoverInstance = id;
                            return;
                        }

                        testPos = VectorUtils.XZ(netManager.m_nodes.m_buffer[endNode].m_position);

                        if ((mousePos - testPos).sqrMagnitude < sqDist)
                        {
                            id.NetNode = endNode;
                            m_hoverInstance = id;
                            return;
                        }

                        id.NetSegment = output.m_netSegment;
                    }
                }
                else if (output.m_building != 0)
                {
                    id.Building = Building.FindParentBuilding(output.m_building);
                    if (id.Building == 0) id.Building = output.m_building;
                }
                else if (output.m_propInstance != 0)
                {
                    id.Prop = output.m_propInstance;
                }
                else if (output.m_treeInstance != 0u)
                {
                    id.Tree = output.m_treeInstance;
                }

                m_hoverInstance = id;
            }
        }

        private Vector3 RaycastMouseLocation(Ray mouseRay)
        {
            RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
            RaycastOutput output;
            input.m_ignoreTerrain = false;
            ToolBase.RayCast(input, out output);

            return output.m_hitPos;
        }

        private HashSet<Instance> GetMarqueeList(Ray mouseRay)
        {
            HashSet<Instance> list = new HashSet<Instance>();

            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
            PropInstance[] propBuffer = PropManager.instance.m_props.m_buffer;
            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;
            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
            TreeInstance[] treeBuffer = TreeManager.instance.m_trees.m_buffer;

            m_selection.a = m_mouseStartPosition;
            m_selection.c = RaycastMouseLocation(mouseRay);

            if (m_selection.a.x == m_selection.c.x && m_selection.a.z == m_selection.c.z)
            {
                m_selection = default(Quad3);
            }
            else
            {
                float angle = Camera.main.transform.localEulerAngles.y * Mathf.Deg2Rad;
                Vector3 down = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
                Vector3 right = new Vector3(-down.z, 0, down.x);

                Vector3 a = m_selection.c - m_selection.a;
                float dotDown = Vector3.Dot(a, down);
                float dotRight = Vector3.Dot(a, right);

                if ((dotDown > 0 && dotRight > 0) || (dotDown <= 0 && dotRight <= 0))
                {
                    m_selection.b = m_selection.a + dotDown * down;
                    m_selection.d = m_selection.a + dotRight * right;
                }
                else
                {
                    m_selection.b = m_selection.a + dotRight * right;
                    m_selection.d = m_selection.a + dotDown * down;
                }

                Vector3 min = m_selection.Min();
                Vector3 max = m_selection.Max();

                int gridMinX = Mathf.Max((int)((min.x - 16f) / 64f + 135f), 0);
                int gridMinZ = Mathf.Max((int)((min.z - 16f) / 64f + 135f), 0);
                int gridMaxX = Mathf.Min((int)((max.x + 16f) / 64f + 135f), 269);
                int gridMaxZ = Mathf.Min((int)((max.z + 16f) / 64f + 135f), 269);

                InstanceID id = new InstanceID();

                ItemClass.Layer itemLayers = GetItemLayers();

                for (int i = gridMinZ; i <= gridMaxZ; i++)
                {
                    for (int j = gridMinX; j <= gridMaxX; j++)
                    {
                        if (filterBuildings)
                        {
                            ushort building = BuildingManager.instance.m_buildingGrid[i * 270 + j];
                            int count = 0;
                            while (building != 0u)
                            {
                                if (IsBuildingValid(ref buildingBuffer[building], itemLayers) && buildingBuffer[building].m_parentBuilding <= 0 && PointInRectangle(m_selection, buildingBuffer[building].m_position))
                                {
                                    id.Building = building;
                                    list.Add(id);
                                }
                                building = buildingBuffer[building].m_nextGridBuilding;

                                if (++count > 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }

                        if (filterProps || filterDecals)
                        {
                            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
                            int count = 0;
                            while (prop != 0u)
                            {
                                bool isDecal = IsDecal(propBuffer[prop].Info);
                                if ((filterDecals && isDecal) || (filterProps && !isDecal))
                                {
                                    if (PointInRectangle(m_selection, propBuffer[prop].Position))
                                    {
                                        id.Prop = prop;
                                        list.Add(id);
                                    }
                                }

                                prop = propBuffer[prop].m_nextGridProp;

                                if (++count > 65536)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (filterNodes)
                        {
                            ushort node = NetManager.instance.m_nodeGrid[i * 270 + j];
                            int count = 0;
                            while (node != 0u)
                            {
                                if (IsNodeValid(ref nodeBuffer[node], itemLayers) && PointInRectangle(m_selection, nodeBuffer[node].m_position))
                                {
                                    id.NetNode = node;
                                    list.Add(id);
                                }
                                node = nodeBuffer[node].m_nextGridNode;

                                if (++count > 32768)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (filterSegments)
                        {
                            ushort segment = NetManager.instance.m_segmentGrid[i * 270 + j];
                            int count = 0;
                            while (segment != 0u)
                            {
                                if (IsSegmentValid(ref segmentBuffer[segment], itemLayers) && PointInRectangle(m_selection, segmentBuffer[segment].m_bounds.center))
                                {
                                    id.NetSegment = segment;
                                    list.Add(id);
                                }
                                segment = segmentBuffer[segment].m_nextGridSegment;

                                if (++count > 36864)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }

                if (filterTrees)
                {
                    gridMinX = Mathf.Max((int)((min.x - 8f) / 32f + 270f), 0);
                    gridMinZ = Mathf.Max((int)((min.z - 8f) / 32f + 270f), 0);
                    gridMaxX = Mathf.Min((int)((max.x + 8f) / 32f + 270f), 539);
                    gridMaxZ = Mathf.Min((int)((max.z + 8f) / 32f + 270f), 539);

                    for (int i = gridMinZ; i <= gridMaxZ; i++)
                    {
                        for (int j = gridMinX; j <= gridMaxX; j++)
                        {
                            uint tree = TreeManager.instance.m_treeGrid[i * 540 + j];
                            int count = 0;
                            while (tree != 0)
                            {
                                if (PointInRectangle(m_selection, treeBuffer[tree].Position))
                                {
                                    id.Tree = tree;
                                    list.Add(id);
                                }
                                tree = treeBuffer[tree].m_nextGridTree;

                                if (++count > 262144)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }

        private bool isLeft(Vector3 P0, Vector3 P1, Vector3 P2)
        {
            return ((P1.x - P0.x) * (P2.z - P0.z) - (P2.x - P0.x) * (P1.z - P0.z)) > 0;
        }

        private bool PointInRectangle(Quad3 rectangle, Vector3 p)
        {
            return isLeft(rectangle.a, rectangle.b, p) && isLeft(rectangle.b, rectangle.c, p) && isLeft(rectangle.c, rectangle.d, p) && isLeft(rectangle.d, rectangle.a, p);
        }

        private ItemClass.Layer GetItemLayers()
        {
            ItemClass.Layer itemLayers = ItemClass.Layer.Default;

            if (InfoManager.instance.CurrentMode == InfoManager.InfoMode.Water)
            {
                itemLayers = itemLayers | ItemClass.Layer.WaterPipes;
            }

            if (InfoManager.instance.CurrentMode == InfoManager.InfoMode.Underground || InfoManager.instance.CurrentMode == InfoManager.InfoMode.Traffic || InfoManager.instance.CurrentMode == InfoManager.InfoMode.Transport)
            {
                itemLayers = itemLayers | ItemClass.Layer.MetroTunnels;
            }

            return itemLayers;
        }


        private bool IsDecal(PropInfo prop)
        {
            if (prop != null && prop.m_material != null)
            {
                return (prop.m_material.shader == shaderBlend || prop.m_material.shader == shaderSolid);
            }

            return false;
        }

        private bool IsBuildingValid(ref Building building, ItemClass.Layer itemLayers)
        {
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created)
            {
                return (building.Info.m_class.m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private bool IsNodeValid(ref NetNode node, ItemClass.Layer itemLayers)
        {
            if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created)
            {
                return (node.Info.GetConnectionClass().m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private bool IsSegmentValid(ref NetSegment segment, ItemClass.Layer itemLayers)
        {
            if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.Created)
            {
                return (segment.Info.GetConnectionClass().m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private bool ProcessMoveKeys(Event e, out Vector3 direction, out float angle)
        {
            direction = Vector3.zero;
            angle = 0;

            float magnitude = 5f;
            if (e.shift) magnitude = magnitude * 5f;
            if (e.alt) magnitude = magnitude / 5f;

            if (IsKeyDown(OptionsKeymapping.moveXpos, e))
            {
                direction.x = direction.x + magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.moveXneg, e))
            {
                direction.x = direction.x - magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.moveYpos, e))
            {
                direction.y = direction.y + magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.moveYneg, e))
            {
                direction.y = direction.y - magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.moveZpos, e))
            {
                direction.z = direction.z + magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.moveZneg, e))
            {
                direction.z = direction.z - magnitude;
            }

            if (IsKeyDown(OptionsKeymapping.turnPos, e))
            {
                angle = angle - magnitude * 20f * 9.58738E-05f;
            }

            if (IsKeyDown(OptionsKeymapping.turnNeg, e))
            {
                angle = angle + magnitude * 20f * 9.58738E-05f;
            }

            if (direction != Vector3.zero || angle != 0)
            {
                if (m_keyTime == 0)
                {
                    m_keyTime = Stopwatch.GetTimestamp();
                    return true;
                }
                else if (ElapsedMilliseconds(m_keyTime) >= 250)
                {
                    return true;
                }
            }
            else
            {
                m_keyTime = 0;
            }

            return false;
        }

        private bool IsKeyDown(SavedInputKey inputKey, Event e)
        {
            int code = inputKey.value;
            KeyCode keyCode = (KeyCode)(code & 0xFFFFFFF);

            bool ctrl = ((code & 0x40000000) != 0);

            return Input.GetKey(keyCode) && ctrl == e.control;
        }

        private long ElapsedMilliseconds(long startTime)
        {
            long endTime = Stopwatch.GetTimestamp();
            long elapsed;

            if (endTime > startTime)
            {
                elapsed = endTime - startTime;
            }
            else
            {
                elapsed = startTime - endTime;
            }

            return elapsed / (Stopwatch.Frequency / 1000);
        }

        private Vector3 GetSnapDelta(Vector3 moveDelta, float angleDelta, Vector3 center, out bool autoCurve)
        {
            autoCurve = false;

            if (VectorUtils.XZ(moveDelta) == Vector2.zero)
            {
                return moveDelta;
            }

            Vector3 newMoveDelta = moveDelta;

            NetManager netManager = NetManager.instance;
            NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
            NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(angleDelta * 57.29578f, Vector3.down), Vector3.one);

            bool snap = false;

            HashSet<InstanceState> newStates = null;

            TransformAction transformAction = ActionQueue.instance.current as TransformAction;
            if (transformAction != null)
            {
                newStates = transformAction.CalculateStates(moveDelta, angleDelta, center, followTerrain);
            }

            CloneAction cloneAction = ActionQueue.instance.current as CloneAction;
            if (cloneAction != null)
            {
                newStates = cloneAction.CalculateStates(moveDelta, angleDelta, center, followTerrain);
            }

            // Snap to direction
            if (newStates.Count == 1)
            {
                foreach (InstanceState state in newStates)
                {
                    if (state.instance.id.Type == InstanceType.NetSegment)
                    {
                        return SnapSegmentDirections(state.instance.id.NetSegment, state.position, moveDelta);
                    }
                    else if (state.instance.id.Type == InstanceType.NetNode)
                    {
                        if (TrySnapNodeDirections(state.instance.id.NetNode, state.position, moveDelta, out newMoveDelta, out autoCurve))
                        {
                            DebugUtils.Log("Snap to direction: " + moveDelta + ", " + newMoveDelta);
                            return newMoveDelta;
                        }
                    }
                }
            }

            HashSet<ushort> ingnoreSegments = new HashSet<ushort>();
            HashSet<ushort> segmentList = new HashSet<ushort>();

            ushort[] closeSegments = new ushort[16];
            int closeSegmentCount;

            // Get list of closest segments
            foreach (InstanceState state in newStates)
            {
                netManager.GetClosestSegments(state.position, closeSegments, out closeSegmentCount);
                segmentList.UnionWith(closeSegments);

                if (toolState != ToolState.Cloning)
                {
                    ingnoreSegments.UnionWith(state.instance.segmentList);
                }
            }

            float distanceSq = float.MaxValue;

            // Snap to node
            foreach (ushort segment in segmentList)
            {
                if (!ingnoreSegments.Contains(segment))
                {
                    foreach (InstanceState state in newStates)
                    {
                        if (state.instance.id.Type == InstanceType.NetNode)
                        {
                            float minSqDistance = segmentBuffer[segment].Info.GetMinNodeDistance() / 2f;
                            minSqDistance *= minSqDistance;

                            ushort startNode = segmentBuffer[segment].m_startNode;
                            ushort endNode = segmentBuffer[segment].m_endNode;

                            snap = TrySnapping(nodeBuffer[startNode].m_position, state.position, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta) || snap;
                            snap = TrySnapping(nodeBuffer[endNode].m_position, state.position, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta) || snap;
                        }
                    }
                }
            }

            if (snap)
            {
                DebugUtils.Log("Snap to node: " + moveDelta + ", " + newMoveDelta);
                return newMoveDelta;
            }

            // Snap to segment
            foreach (ushort segment in segmentList)
            {
                if (!ingnoreSegments.Contains(segment))
                {
                    foreach (InstanceState state in newStates)
                    {
                        if (state.instance.id.Type == InstanceType.NetNode)
                        {
                            float minSqDistance = segmentBuffer[segment].Info.GetMinNodeDistance() / 2f;
                            minSqDistance *= minSqDistance;

                            Vector3 testPos;
                            Vector3 direction;
                            segmentBuffer[segment].GetClosestPositionAndDirection(state.position, out testPos, out direction);

                            snap = TrySnapping(testPos, state.position, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta) || snap;
                        }
                    }
                }
            }

            if (snap)
            {
                DebugUtils.Log("Snap to segment: " + moveDelta + ", " + newMoveDelta);
                return newMoveDelta;
            }

            // Snap to grid
            ushort block = 0;
            ushort previousBlock = 0;
            Vector3 refPosition = Vector3.zero;
            bool smallRoad = false;

            foreach (ushort segment in segmentList)
            {
                bool hasBlocks = segment != 0 && (segmentBuffer[segment].m_blockStartLeft != 0 || segmentBuffer[segment].m_blockStartRight != 0 || segmentBuffer[segment].m_blockEndLeft != 0 || segmentBuffer[segment].m_blockEndRight != 0);
                if (hasBlocks && !ingnoreSegments.Contains(segment))
                {
                    foreach (InstanceState state in newStates)
                    {
                        if (state.instance.id.Type != InstanceType.NetSegment)
                        {
                            Vector3 testPosition = state.position;

                            if (state.instance.id.Type == InstanceType.Building)
                            {
                                ushort building = state.instance.id.Building;
                                testPosition = GetBuildingSnapPoint(state.position, state.angle, buildingBuffer[building].Length, buildingBuffer[building].Width);
                            }

                            segmentBuffer[segment].GetClosestZoneBlock(testPosition, ref distanceSq, ref block);

                            if (block != previousBlock)
                            {
                                refPosition = testPosition;

                                if (state.instance.id.Type == InstanceType.NetNode)
                                {
                                    if (nodeBuffer[state.instance.id.NetNode].Info.m_halfWidth <= 4f)
                                    {
                                        smallRoad = true;
                                    }
                                }

                                previousBlock = block;
                            }
                        }
                    }
                }
            }

            if (block != 0)
            {
                Vector3 newPosition = refPosition;
                ZoneBlock zoneBlock = ZoneManager.instance.m_blocks.m_buffer[block];
                SnapToBlock(ref newPosition, zoneBlock.m_position, zoneBlock.m_angle, smallRoad);

                DebugUtils.Log("Snap to grid: " + moveDelta + ", " + (moveDelta + newPosition - refPosition));
                return moveDelta + newPosition - refPosition;
            }

            // Snap to editor grid
            if ((ToolManager.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None)
            {
                Vector3 assetGridPosition = Vector3.zero;
                float testMagnitude = 0;

                foreach (InstanceState state in newStates)
                {
                    Vector3 testPosition = state.position;

                    if (state.instance.id.Type == InstanceType.Building)
                    {
                        ushort building = state.instance.id.Building;
                        testPosition = GetBuildingSnapPoint(state.position, state.angle, buildingBuffer[building].Length, buildingBuffer[building].Width);
                    }


                    float x = Mathf.Round(testPosition.x / 8f) * 8f;
                    float z = Mathf.Round(testPosition.z / 8f) * 8f;

                    Vector3 newPosition = new Vector3(x, testPosition.y, z);
                    float deltaMagnitude = (newPosition - testPosition).sqrMagnitude;

                    if (assetGridPosition == Vector3.zero || deltaMagnitude < testMagnitude)
                    {
                        refPosition = testPosition;
                        assetGridPosition = newPosition;
                        deltaMagnitude = testMagnitude;
                    }
                }

                DebugUtils.Log("Snap to grid: " + moveDelta + ", " + (moveDelta + assetGridPosition - refPosition));
                return moveDelta + assetGridPosition - refPosition;
            }

            return moveDelta;
        }

        private bool TrySnapping(Vector3 testPos, Vector3 newPosition, float minSqDistance, ref float distanceSq, Vector3 moveDelta, ref Vector3 newMoveDelta)
        {
            float testSqDist = Vector2.SqrMagnitude(VectorUtils.XZ(testPos - newPosition));

            if (testSqDist < minSqDistance && testSqDist < distanceSq)
            {
                newMoveDelta = moveDelta + (testPos - newPosition);
                newMoveDelta.y = moveDelta.y;

                distanceSq = testSqDist;

                //DebugUtils.Log("-TrySnapping- testPos: " + testPos + " newPosition: " + newPosition + " minSqDistance: " + minSqDistance + " distanceSq: " + distanceSq + " moveDelta: " + moveDelta + " newMoveDelta: " + newMoveDelta);
                return true;
            }

            return false;
        }

        private Vector3 GetBuildingSnapPoint(Vector3 position, float angle, int length, int width)
        {
            float x = 0;
            float z = length * 4f;

            if (width % 2 != 0) x = 4f;

            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);

            return position + new Vector3(ca * x - sa * z, 0f, sa * x + ca * z);
        }

        private void SnapToBlock(ref Vector3 point, Vector3 refPoint, float refAngle, bool smallRoad)
        {
            Vector3 direction = new Vector3(Mathf.Cos(refAngle), 0f, Mathf.Sin(refAngle));
            Vector3 forward = direction * 8f;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            if (smallRoad)
            {
                refPoint.x += forward.x * 0.5f + right.x * 0.5f;
                refPoint.z += forward.z * 0.5f + right.z * 0.5f;
            }

            Vector2 delta = new Vector2(point.x - refPoint.x, point.z - refPoint.z);
            float num = Mathf.Round((delta.x * forward.x + delta.y * forward.z) * 0.015625f);
            float num2 = Mathf.Round((delta.x * right.x + delta.y * right.z) * 0.015625f);
            point.x = refPoint.x + num * forward.x + num2 * right.x;
            point.z = refPoint.z + num * forward.z + num2 * right.z;
        }

        private Vector3 SnapSegmentDirections(ushort segment, Vector3 newPosition, Vector3 moveDelta)
        {
            NetManager netManager = NetManager.instance;
            NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
            NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

            float minSqDistance = segmentBuffer[segment].Info.GetMinNodeDistance() / 2f;
            minSqDistance *= minSqDistance;

            ushort startNode = segmentBuffer[segment].m_startNode;
            ushort endNode = segmentBuffer[segment].m_endNode;

            Vector3 startPos = nodeBuffer[segmentBuffer[segment].m_startNode].m_position;
            Vector3 endPos = nodeBuffer[segmentBuffer[segment].m_endNode].m_position;

            Vector3 newMoveDelta = moveDelta;
            float distanceSq = minSqDistance;
            bool snap = false;

            // Snap to tangent intersection
            for (int i = 0; i < 8; i++)
            {
                ushort segmentA = nodeBuffer[startNode].GetSegment(i);
                if (segmentA != 0 && segmentA != segment)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ushort segmentB = nodeBuffer[endNode].GetSegment(j);

                        if (segmentB != 0 && segmentB != segment)
                        {
                            Vector3 startDir = segmentBuffer[segmentA].m_startNode == startNode ? segmentBuffer[segmentA].m_startDirection : segmentBuffer[segmentA].m_endDirection;
                            Vector3 endDir = segmentBuffer[segmentB].m_startNode == endNode ? segmentBuffer[segmentB].m_startDirection : segmentBuffer[segmentB].m_endDirection;

                            float num;
                            if (!NetSegment.IsStraight(startPos, startDir, endPos, endDir, out num))
                            {
                                float dot = startDir.x * endDir.x + startDir.z * endDir.z;
                                float u;
                                float v;
                                if (dot >= -0.999f && Line2.Intersect(VectorUtils.XZ(startPos), VectorUtils.XZ(startPos + startDir), VectorUtils.XZ(endPos), VectorUtils.XZ(endPos + endDir), out u, out v))
                                {
                                    snap = TrySnapping(startPos + startDir * u, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta) || snap;
                                }
                            }
                        }
                    }
                }
            }

            if (!snap)
            {
                // Snap to start tangent
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentA = nodeBuffer[startNode].GetSegment(i);
                    if (segmentA != 0 && segmentA != segment)
                    {
                        Vector3 startDir = segmentBuffer[segmentA].m_startNode == startNode ? segmentBuffer[segmentA].m_startDirection : segmentBuffer[segmentA].m_endDirection;
                        Vector3 offset = Line2.Offset(startDir, startPos - newPosition);
                        offset = newPosition + offset - startPos;
                        float num = offset.x * startDir.x + offset.z * startDir.z;

                        TrySnapping(startPos + startDir * num, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta);
                    }
                }

                // Snap to end tangent
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentB = nodeBuffer[endNode].GetSegment(i);

                    if (segmentB != 0 && segmentB != segment)
                    {
                        Vector3 endDir = segmentBuffer[segmentB].m_startNode == endNode ? segmentBuffer[segmentB].m_startDirection : segmentBuffer[segmentB].m_endDirection;
                        Vector3 offset = Line2.Offset(endDir, endPos - newPosition);
                        offset = newPosition + offset - endPos;
                        float num = offset.x * endDir.x + offset.z * endDir.z;

                        TrySnapping(endPos + endDir * num, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta);
                    }
                }
            }

            // Snap straight
            TrySnapping((startPos + endPos) / 2f, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta);

            return newMoveDelta;
        }

        private bool TrySnapNodeDirections(ushort node, Vector3 newPosition, Vector3 moveDelta, out Vector3 newMoveDelta, out bool autoCurve)
        {
            string snapType = "";

            m_segmentGuide = default(NetSegment);

            NetManager netManager = NetManager.instance;
            NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
            NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

            float minSqDistance = nodeBuffer[node].Info.GetMinNodeDistance() / 2f;
            minSqDistance *= minSqDistance;

            autoCurve = false;
            newMoveDelta = moveDelta;
            float distanceSq = minSqDistance;

            bool snap = false;

            // Snap to curve
            for (int i = 0; i < 8; i++)
            {
                ushort segmentA = nodeBuffer[node].GetSegment(i);
                if (segmentA != 0)
                {
                    for (int j = i + 1; j < 8; j++)
                    {
                        ushort segmentB = nodeBuffer[node].GetSegment(j);

                        if (segmentB != 0 && segmentB != segmentA)
                        {
                            NetSegment segment = default(NetSegment);
                            segment.m_startNode = segmentBuffer[segmentA].m_startNode == node ? segmentBuffer[segmentA].m_endNode : segmentBuffer[segmentA].m_startNode;
                            segment.m_endNode = segmentBuffer[segmentB].m_startNode == node ? segmentBuffer[segmentB].m_endNode : segmentBuffer[segmentB].m_startNode;

                            Vector3 testPos;
                            Vector3 direction;
                            segment.m_startDirection = (nodeBuffer[segment.m_endNode].m_position - nodeBuffer[segment.m_startNode].m_position).normalized;
                            segment.m_endDirection = -segment.m_startDirection;

                            segment.GetClosestPositionAndDirection(newPosition, out testPos, out direction);
                            // Straight
                            if (TrySnapping(testPos, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta))
                            {
                                autoCurve = true;
                                m_segmentGuide = segment;
                                snapType = "Straight";
                                snap = true;
                            }

                            for (int k = 0; k < 8; k++)
                            {
                                ushort segmentC = nodeBuffer[segment.m_startNode].GetSegment(k);
                                if (segmentC != 0 && segmentC != segmentA)
                                {
                                    for (int l = 0; l < 8; l++)
                                    {
                                        ushort segmentD = nodeBuffer[segment.m_endNode].GetSegment(l);

                                        if (segmentD != 0 && segmentD != segmentB)
                                        {
                                            segment.m_startDirection = segmentBuffer[segmentC].m_startNode == segment.m_startNode ? -segmentBuffer[segmentC].m_startDirection : -segmentBuffer[segmentC].m_endDirection;
                                            segment.m_endDirection = segmentBuffer[segmentD].m_startNode == segment.m_endNode ? -segmentBuffer[segmentD].m_startDirection : -segmentBuffer[segmentD].m_endDirection;

                                            Vector2 A = VectorUtils.XZ(nodeBuffer[segment.m_endNode].m_position - nodeBuffer[segment.m_startNode].m_position).normalized;
                                            Vector2 B = VectorUtils.XZ(segment.m_startDirection);
                                            float side1 = A.x * B.y - A.y * B.x;

                                            B = VectorUtils.XZ(segment.m_endDirection);
                                            float side2 = A.x * B.y - A.y * B.x;

                                            if (Mathf.Sign(side1) != Mathf.Sign(side2) ||
                                                (side1 != side2 && (side1 == 0 || side2 == 0)) ||
                                                Vector2.Dot(A, VectorUtils.XZ(segment.m_startDirection)) < 0 ||
                                                Vector2.Dot(A, VectorUtils.XZ(segment.m_endDirection)) > 0)
                                            {
                                                continue;
                                            }

                                            Bezier3 bezier = default(Bezier3);
                                            bezier.a = Singleton<NetManager>.instance.m_nodes.m_buffer[segment.m_startNode].m_position;
                                            bezier.d = Singleton<NetManager>.instance.m_nodes.m_buffer[segment.m_endNode].m_position;
                                            bool smoothStart = (Singleton<NetManager>.instance.m_nodes.m_buffer[segment.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
                                            bool smoothEnd = (Singleton<NetManager>.instance.m_nodes.m_buffer[segment.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
                                            NetSegment.CalculateMiddlePoints(bezier.a, segment.m_startDirection, bezier.d, segment.m_endDirection, smoothStart, smoothEnd, out bezier.b, out bezier.c);

                                            testPos = bezier.Position(0.5f);
                                            // Curve Middle
                                            if (TrySnapping(testPos, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta))
                                            {
                                                autoCurve = true;
                                                m_segmentGuide = segment;
                                                snapType = "Curve Middle";
                                                snap = true;
                                            }
                                            else
                                            {
                                                segment.GetClosestPositionAndDirection(newPosition, out testPos, out direction);
                                                // Curve
                                                if (TrySnapping(testPos, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta))
                                                {
                                                    autoCurve = true;
                                                    m_segmentGuide = segment;
                                                    snapType = "Curve";
                                                    snap = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Snap to tangent
            for (int i = 0; i < 8; i++)
            {
                ushort segment = nodeBuffer[node].GetSegment(i);
                if (segment != 0)
                {
                    ushort testNode = segmentBuffer[segment].m_startNode == node ? segmentBuffer[segment].m_endNode : segmentBuffer[segment].m_startNode;
                    Vector3 testPos = nodeBuffer[testNode].m_position;

                    for (int j = 0; j < 8; j++)
                    {
                        ushort segmentA = nodeBuffer[testNode].GetSegment(j);
                        if (segmentA != 0 && segmentA != segment)
                        {
                            // Straight
                            Vector3 startDir = segmentBuffer[segmentA].m_startNode == testNode ? segmentBuffer[segmentA].m_startDirection : segmentBuffer[segmentA].m_endDirection;
                            Vector3 offset = Line2.Offset(startDir, testPos - newPosition);
                            offset = newPosition + offset - testPos;
                            float num = offset.x * startDir.x + offset.z * startDir.z;

                            if (TrySnapping(testPos + startDir * num, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta))
                            {
                                m_segmentGuide = default(NetSegment);

                                m_segmentGuide.m_startNode = node;
                                m_segmentGuide.m_endNode = testNode;

                                m_segmentGuide.m_startDirection = startDir;
                                m_segmentGuide.m_endDirection = -startDir;
                                snapType = "Tangent Straight";
                                autoCurve = false;
                                snap = true;
                            }
                            else
                            {
                                // 90°
                                startDir = new Vector3(-startDir.z, startDir.y, startDir.x);
                                offset = Line2.Offset(startDir, testPos - newPosition);
                                offset = newPosition + offset - testPos;
                                num = offset.x * startDir.x + offset.z * startDir.z;

                                if (TrySnapping(testPos + startDir * num, newPosition, minSqDistance, ref distanceSq, moveDelta, ref newMoveDelta))
                                {
                                    m_segmentGuide = default(NetSegment);

                                    m_segmentGuide.m_startNode = node;
                                    m_segmentGuide.m_endNode = testNode;

                                    m_segmentGuide.m_startDirection = startDir;
                                    m_segmentGuide.m_endDirection = -startDir;
                                    snapType = "Tangent 90°";
                                    autoCurve = false;
                                    snap = true;
                                }
                            }
                        }
                    }
                }
            }

            if(snap)
            {
                DebugUtils.Log("Snapping " + snapType + " " + autoCurve);
            }

            return snap;
        }
    }
}
