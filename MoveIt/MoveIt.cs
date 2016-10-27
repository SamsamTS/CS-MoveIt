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
    public class MoveItLoader : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (MoveItTool.instance == null)
            {
                // Creating the instance
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();

                MoveItTool.instance = toolController.gameObject.AddComponent<MoveItTool>();
            }
        }

        public override void OnLevelUnloading()
        {
            if (MoveItTool.instance != null)
            {
                MoveItTool.instance.enabled = false;
            }
        }
    }

    public class MoveItTool : ToolBase
    {
        private enum Actions
        {
            None,
            Undo,
            Redo,
            Transform
        }

        public const string settingsFileName = "MoveItTool";

        public static MoveItTool instance;
        public static SavedBool hideTips = new SavedBool("hideTips", settingsFileName, false, true);
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);

        public static bool marqueeSelection = false;

        public static bool filterBuildings = true;
        public static bool filterProps = true;
        public static bool filterTrees = true;
        public static bool filterNodes = true;
        public static bool filterSegments = true;

        private static Color m_hoverColor = new Color32(0, 181, 255, 255);
        private static Color m_selectedColor = new Color32(95, 166, 0, 244);
        private static Color m_removeColor = new Color32(255, 160, 47, 191);

        private bool m_snapping = false;
        private bool m_prevRenderZones;

        private Moveable m_hoverInstance;
        private HashSet<Moveable> m_marqueeInstances;

        private ToolBase m_prevTool;
        private UIMoveItButton m_button;

        private long m_keyTime;
        private long m_rightClickTime;
        private long m_leftClickTime;
        private long m_stopWatchfrequency = Stopwatch.Frequency / 1000;

        private Vector3 m_startPosition;
        private Vector3 m_mouseStartPosition;

        private bool m_drawingSelection;
        private Quad3 m_selection;

        private float m_mouseStartX;
        private ushort m_startAngle;

        private MoveQueue m_moves = new MoveQueue();
        private Actions m_nextAction = Actions.None;

        private Dictionary<string, Statistics.stats> m_counters;

        public bool snapping
        {
            get
            {
                if (m_leftClickTime != 0)
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

        protected override void Awake()
        {
            m_counters = Statistics.counters;
            m_counters.Clear();

            m_toolController = GameObject.FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;
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
                UITipsWindow.instance.NextTip();
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
        }

        protected override void OnToolUpdate()
        {
            if (!this.m_toolController.IsInsideUI && Cursor.visible)
            {
                lock (m_moves)
                {
                    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHoverInstance(mouseRay);

                    if (m_drawingSelection)
                    {
                        m_marqueeInstances = GetMarqueeList(mouseRay);
                    }

                    if (m_nextAction == Actions.None && m_moves.currentType != MoveQueue.StepType.Invalid)
                    {
                        if (m_rightClickTime != 0 && ElapsedMilliseconds(m_rightClickTime) > 200)
                        {
                            if (m_moves.currentType == MoveQueue.StepType.Selection)
                            {
                                m_moves.Push(MoveQueue.StepType.Move, true);
                            }
                            MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;

                            ushort newAngle = (ushort)(m_startAngle + (ushort)(ushort.MaxValue * (Input.mousePosition.x - m_mouseStartX) / Screen.width));
                            if (step.angleDelta != newAngle)
                            {
                                step.angleDelta = newAngle;
                                m_nextAction = Actions.Transform;
                            }

                        }
                        else if (m_leftClickTime != 0 && ElapsedMilliseconds(m_leftClickTime) > 200)
                        {
                            if (m_moves.currentType == MoveQueue.StepType.Selection)
                            {
                                m_moves.Push(MoveQueue.StepType.Move, true);
                            }
                            MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;

                            float y = step.moveDelta.y;
                            Vector3 newMove = m_startPosition + RaycastMouseLocation(mouseRay) - m_mouseStartPosition;
                            newMove.y = y;

                            if (step.moveDelta != newMove)
                            {
                                step.moveDelta = newMove;
                                m_nextAction = Actions.Transform;
                            }
                        }
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        OnLeftMouseDown(mouseRay);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        OnLeftMouseUp(mouseRay);
                    }

                    if (Input.GetMouseButtonDown(1))
                    {
                        OnRightMouseDown();
                    }

                    if (Input.GetMouseButtonUp(1))
                    {
                        OnRightMouseUp();
                    }

                    if (m_leftClickTime != 0 || m_rightClickTime != 0)
                    {
                        m_hoverInstance = null;
                    }
                }
            }
        }

        protected override void OnToolGUI(Event e)
        {
            lock (m_moves)
            {
                if (m_nextAction != Actions.None)
                {
                    return;
                }

                if (OptionsKeymapping.undo.IsPressed(e))
                {
                    m_nextAction = Actions.Undo;
                }
                else if (OptionsKeymapping.redo.IsPressed(e))
                {
                    m_nextAction = Actions.Redo;
                }
                else if (m_moves.hasSelection || (m_hoverInstance != null && !marqueeSelection))
                {
                    Vector3 direction;
                    int angle;

                    if (ProcessMoveKeys(e, out direction, out angle))
                    {
                        if (m_moves.currentType == MoveQueue.StepType.Selection)
                        {
                            m_moves.Push(MoveQueue.StepType.Move, true);
                        }
                        else if (m_moves.currentType == MoveQueue.StepType.Invalid ||
                            (!m_moves.hasSelection && !m_moves.current.instances.Contains(m_hoverInstance)))
                        {
                            m_moves.Push(MoveQueue.StepType.Move);
                            m_moves.current.isSelection = false;
                            m_moves.current.instances.Add(m_hoverInstance);
                        }

                        if (direction != Vector3.zero)
                        {
                            direction.x = direction.x * 0.263671875f;
                            direction.y = direction.y * 0.015625f;
                            direction.z = direction.z * 0.263671875f;

                            if (!useCardinalMoves)
                            {
                                Matrix4x4 matrix4x = default(Matrix4x4);
                                matrix4x.SetTRS(Vector3.zero, Quaternion.AngleAxis(Camera.main.transform.localEulerAngles.y, Vector3.up), Vector3.one);

                                direction = matrix4x.MultiplyVector(direction);
                            }
                        }

                        MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;

                        step.moveDelta = step.moveDelta + direction;
                        step.angleDelta = (ushort)(step.angleDelta + angle);

                        m_nextAction = Actions.Transform;
                    }

                    UIToolOptionPanel.RefreshSnapButton();
                }
            }
        }

        public override void SimulationStep()
        {
            base.SimulationStep();

            lock (m_moves)
            {
                switch (m_nextAction)
                {
                    case Actions.Undo:
                        {
                            Undo();
                            break;
                        }
                    case Actions.Redo:
                        {
                            Redo();
                            break;
                        }
                    case Actions.Transform:
                        {
                            Transform();
                            break;
                        }
                }

                m_nextAction = Actions.None;
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (m_drawingSelection)
            {
                bool removing = Event.current.alt;

                Color color = m_hoverColor;
                if (removing)
                {
                    color = m_removeColor;
                }

                RenderManager.instance.OverlayEffect.DrawQuad(cameraInfo, color, m_selection, m_selection.Min().y - 100f, m_selection.Max().y + 100f, true, true);

                if (m_marqueeInstances != null)
                {
                    MoveQueue.Step step = m_moves.current;
                    removing = removing && step != null;

                    foreach (Moveable instance in m_marqueeInstances)
                    {
                        if (instance.isValid && (!removing || step.instances.Contains(instance)))
                        {
                            RenderInstanceOverlay(cameraInfo, instance.id, color);
                        }
                    }
                }
            }

            if (m_moves.hasSelection)
            {
                bool removing = m_drawingSelection && Event.current.alt && m_marqueeInstances != null;

                MoveQueue.Step step = m_moves.current;
                foreach (Moveable instance in step.instances)
                {
                    if (instance.isValid && !(removing && m_marqueeInstances.Contains(instance)))
                    {
                        RenderInstanceOverlay(cameraInfo, instance.id, m_selectedColor);
                    }
                }

                Vector3 center = step.center;
                if (m_moves.currentType == MoveQueue.StepType.Move)
                {
                    center = center + (step as MoveQueue.MoveStep).moveDelta;
                }

                center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, center.y - 100f, center.y + 100f, true, true);

                if (!marqueeSelection && m_hoverInstance != null && !step.instances.Contains(m_hoverInstance))
                {
                    RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
                }
            }
            else if (!marqueeSelection && m_hoverInstance != null)
            {
                RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
            }

            base.RenderOverlay(cameraInfo);
        }


        public void Undo()
        {
            lock (m_moves)
            {
                if (m_moves.currentType == MoveQueue.StepType.Move)
                {
                    MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;

                    Bounds bounds = GetTotalBounds(false);
                    foreach (Moveable instance in step.instances)
                    {
                        if (instance.isValid)
                        {
                            instance.Restore();
                        }
                    }
                    UpdateArea(bounds);
                    UpdateArea(GetTotalBounds(false));
                }

                m_moves.Previous();
            }
        }

        public void Redo()
        {
            lock (m_moves)
            {
                if (m_moves.Next() && m_moves.currentType == MoveQueue.StepType.Move)
                {
                    MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;

                    Vector3 moveDelta = step.moveDelta;
                    if (step.snap)
                    {
                        moveDelta = GetSnapPosition(step);
                    }

                    Bounds bounds = GetTotalBounds(false);
                    float fAngle = step.angleDelta * 9.58738E-05f;

                    Matrix4x4 matrix4x = default(Matrix4x4);
                    matrix4x.SetTRS(step.center + moveDelta, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

                    foreach (Moveable instance in step.instances)
                    {
                        if (instance.isValid)
                        {
                            instance.Transform(ref matrix4x, moveDelta, step.angleDelta, step.center);
                        }
                    }
                    UpdateArea(bounds);
                    UpdateArea(GetTotalBounds(false));
                }
            }
        }

        public void Transform()
        {
            lock (m_moves)
            {
                if (m_moves.currentType == MoveQueue.StepType.Selection)
                {
                    m_moves.Push(MoveQueue.StepType.Move, true);
                }

                MoveQueue.MoveStep step = m_moves.current as MoveQueue.MoveStep;
                step.snap = snapping;

                Vector3 moveDelta = step.moveDelta;
                if (step.snap)
                {
                    moveDelta = GetSnapPosition(step);
                }

                Bounds bounds = GetTotalBounds(false);
                float fAngle = step.angleDelta * 9.58738E-05f;

                Matrix4x4 matrix4x = default(Matrix4x4);
                matrix4x.SetTRS(step.center + moveDelta, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

                foreach (Moveable instance in step.instances)
                {
                    if (instance.isValid)
                    {
                        instance.Transform(ref matrix4x, moveDelta, step.angleDelta, step.center);
                    }
                }
                UpdateArea(bounds);
                UpdateArea(GetTotalBounds(false));
            }
        }

        public bool IsSegmentSelected(ushort segment)
        {
            if (m_moves.currentType == MoveQueue.StepType.Invalid)
            {
                return false;
            }

            Moveable instance = new Moveable(InstanceID.Empty);
            instance.id.NetSegment = segment;

            return m_moves.current.instances.Contains(instance);
        }

        private void OnLeftMouseDown(Ray mouseRay)
        {
            bool shouldMove = m_hoverInstance != null;

            if (shouldMove && marqueeSelection)
            {
                shouldMove = m_moves.currentType != MoveQueue.StepType.Invalid && m_moves.current.instances.Contains(m_hoverInstance);
            }

            if (shouldMove)
            {
                if (m_moves.currentType == MoveQueue.StepType.Invalid || !m_moves.current.isSelection)
                {
                    m_moves.Push(MoveQueue.StepType.Selection);
                }

                if (Event.current.shift)
                {
                    if (m_moves.currentType == MoveQueue.StepType.Move && (m_moves.current as MoveQueue.MoveStep).hasMoved)
                    {
                        m_moves.Push(MoveQueue.StepType.Selection, true);
                    }

                    MoveQueue.Step step = m_moves.current;

                    if (step.instances.Contains(m_hoverInstance))
                    {
                        step.instances.Remove(m_hoverInstance);
                    }
                    else
                    {
                        step.instances.Add(m_hoverInstance);
                    }

                    if (step.instances.Count > 0)
                    {
                        step.center = GetTotalBounds().center;
                    }
                }
                else
                {
                    if (!m_moves.current.instances.Contains(m_hoverInstance))
                    {
                        if (m_moves.currentType == MoveQueue.StepType.Move && (m_moves.current as MoveQueue.MoveStep).hasMoved)
                        {
                            m_moves.Push(MoveQueue.StepType.Selection, false);
                        }

                        MoveQueue.Step step = m_moves.current;

                        step.instances.Clear();
                        step.instances.Add(m_hoverInstance);
                        step.center = GetTotalBounds().center;
                    }

                    if (m_moves.currentType == MoveQueue.StepType.Move)
                    {
                        m_startPosition = (m_moves.current as MoveQueue.MoveStep).moveDelta;
                    }
                    else
                    {
                        m_startPosition = Vector3.zero;
                    }

                    m_mouseStartPosition = RaycastMouseLocation(mouseRay);
                    m_leftClickTime = Stopwatch.GetTimestamp();
                }
            }
            else if (marqueeSelection)
            {
                m_selection = default(Quad3);
                m_marqueeInstances = null;

                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
                m_drawingSelection = true;
            }
        }

        private void OnLeftMouseUp(Ray mouseRay)
        {
            m_leftClickTime = 0;

            if (m_drawingSelection)
            {
                if (m_moves.currentType == MoveQueue.StepType.Invalid || !m_moves.current.isSelection)
                {
                    m_moves.Push(MoveQueue.StepType.Selection);
                }
                else if (m_moves.currentType == MoveQueue.StepType.Move && (m_moves.current as MoveQueue.MoveStep).hasMoved)
                {
                    m_moves.Push(MoveQueue.StepType.Selection, true);
                }

                Event e = Event.current;

                if (e.alt)
                {
                    m_moves.current.instances.ExceptWith(m_marqueeInstances);
                }
                else
                {
                    if (!e.shift)
                    {
                        m_moves.current.instances.Clear();
                    }
                    m_moves.current.instances.UnionWith(m_marqueeInstances);
                }

                MoveQueue.Step step = m_moves.current;
                if (step.instances.Count > 0)
                {
                    step.center = GetTotalBounds().center;
                }

                m_drawingSelection = false;
            }
        }

        private void OnRightMouseDown()
        {
            if (m_drawingSelection)
            {
                m_drawingSelection = false;
            }
            else if (m_moves.currentType != MoveQueue.StepType.Invalid && m_moves.current.isSelection)
            {
                m_rightClickTime = Stopwatch.GetTimestamp();
                m_mouseStartX = Input.mousePosition.x;

                if (m_moves.currentType == MoveQueue.StepType.Move)
                {
                    m_startAngle = (m_moves.current as MoveQueue.MoveStep).angleDelta;
                }
                else
                {
                    m_startAngle = 0;
                }
            }

        }

        private void OnRightMouseUp()
        {
            if (m_rightClickTime != 0 && ElapsedMilliseconds(m_rightClickTime) < 200)
            {
                if (m_moves.currentType != MoveQueue.StepType.Invalid)
                {
                    if (m_moves.currentType == MoveQueue.StepType.Move && (m_moves.current as MoveQueue.MoveStep).hasMoved)
                    {
                        m_moves.Push(MoveQueue.StepType.Selection, false);
                    }
                    else
                    {
                        m_moves.current.instances.Clear();
                    }
                }
            }

            m_rightClickTime = 0;
        }

        private void RaycastHoverInstance(Ray mouseRay)
        {
            RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
            RaycastOutput output;

            input.m_netService.m_itemLayers = (ItemClass.Layer)11;
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
                        m_hoverInstance = new Moveable(id);
                    }
                    else
                    {
                        NetSegment netSegment = netManager.m_segments.m_buffer[output.m_netSegment];
                        NetNode startNode = netManager.m_nodes.m_buffer[netSegment.m_startNode];
                        NetNode endNode = netManager.m_nodes.m_buffer[netSegment.m_endNode];

                        if (startNode.m_bounds.IntersectRay(mouseRay))
                        {
                            id.NetNode = netSegment.m_startNode;
                            m_hoverInstance = new Moveable(id);
                        }
                        else if (endNode.m_bounds.IntersectRay(mouseRay))
                        {
                            id.NetNode = netSegment.m_endNode;
                            m_hoverInstance = new Moveable(id);
                        }
                        else
                        {
                            id.NetSegment = output.m_netSegment;
                            m_hoverInstance = new Moveable(id);
                        }
                    }
                }
                else if (output.m_building != 0)
                {
                    id.Building = Building.FindParentBuilding(output.m_building);
                    if (id.Building == 0) id.Building = output.m_building;
                    m_hoverInstance = new Moveable(id);
                }
                else if (output.m_propInstance != 0)
                {
                    id.Prop = output.m_propInstance;
                    m_hoverInstance = new Moveable(id);
                }
                else if (output.m_treeInstance != 0u)
                {
                    id.Tree = output.m_treeInstance;
                    m_hoverInstance = new Moveable(id);
                }
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

        private HashSet<Moveable> GetMarqueeList(Ray mouseRay)
        {
            HashSet<Moveable> list = new HashSet<Moveable>();

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

                for (int i = gridMinZ; i <= gridMaxZ; i++)
                {
                    for (int j = gridMinX; j <= gridMaxX; j++)
                    {
                        if (filterBuildings)
                        {
                            ushort building = BuildingManager.instance.m_buildingGrid[i * 270 + j];
                            while (building != 0u)
                            {
                                if (buildingBuffer[building].m_parentBuilding <= 0 && PointInRectangle(m_selection, buildingBuffer[building].m_position))
                                {
                                    id.Building = building;
                                    list.Add(new Moveable(id));
                                }
                                building = buildingBuffer[building].m_nextGridBuilding;
                            }
                        }

                        if (filterProps)
                        {
                            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
                            while (prop != 0u)
                            {
                                if (PointInRectangle(m_selection, propBuffer[prop].Position))
                                {
                                    id.Prop = prop;
                                    list.Add(new Moveable(id));
                                }
                                prop = propBuffer[prop].m_nextGridProp;
                            }
                        }

                        if (filterNodes)
                        {
                            ushort node = NetManager.instance.m_nodeGrid[i * 270 + j];
                            while (node != 0u)
                            {
                                if (PointInRectangle(m_selection, nodeBuffer[node].m_position))
                                {
                                    id.NetNode = node;
                                    list.Add(new Moveable(id));
                                }
                                node = nodeBuffer[node].m_nextGridNode;
                            }
                        }

                        if (filterSegments)
                        {
                            ushort segment = NetManager.instance.m_segmentGrid[i * 270 + j];
                            while (segment != 0u)
                            {
                                if (PointInRectangle(m_selection, segmentBuffer[segment].m_bounds.center))
                                {
                                    id.NetSegment = segment;
                                    list.Add(new Moveable(id));
                                }
                                segment = segmentBuffer[segment].m_nextGridSegment;
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
                            while (tree != 0)
                            {
                                if (PointInRectangle(m_selection, treeBuffer[tree].Position))
                                {
                                    id.Tree = tree;
                                    list.Add(new Moveable(id));
                                }
                                tree = treeBuffer[tree].m_nextGridTree;
                            }
                        }
                    }
                }
            }
            return list;
        }

        private Vector3 GetSnapPosition(MoveQueue.MoveStep step)
        {
            Vector3 moveDelta = step.moveDelta;

            NetManager netManager = NetManager.instance;
            NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
            NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

            HashSet<ushort> ingnoreSegments = new HashSet<ushort>();
            HashSet<ushort> segmentList = new HashSet<ushort>();

            ushort[] closeSegments = new ushort[16];
            int closeSegmentCount;

            // Get list of closest segments
            foreach (Moveable instance in step.instances)
            {
                if (instance.isValid)
                {
                    instance.CalculateNewPosition(moveDelta, step.angleDelta, step.center);
                    netManager.GetClosestSegments(instance.newPosition, closeSegments, out closeSegmentCount);
                    segmentList.UnionWith(closeSegments);

                    ingnoreSegments.UnionWith(instance.segmentList);
                }
            }

            float distanceSq = 256f;
            ushort block = 0;
            ushort previousBlock = 0;
            Vector3 refPosition = Vector3.zero;
            bool smallRoad = false;

            /*foreach (Moveable instance in step.instances)
            {
                if (instance.id.Type == InstanceType.NetSegment && instance.isValid)
                {
                    //TODO
                }
            }*/

            // Snap to grid
            foreach (ushort segment in segmentList)
            {
                bool hasBlocks = segment != 0 && (segmentBuffer[segment].m_blockStartLeft != 0 || segmentBuffer[segment].m_blockStartRight != 0 || segmentBuffer[segment].m_blockEndLeft != 0 || segmentBuffer[segment].m_blockEndRight != 0);
                if (hasBlocks && !ingnoreSegments.Contains(segment))
                {
                    foreach (Moveable instance in step.instances)
                    {
                        if (instance.id.Type != InstanceType.NetSegment && instance.isValid)
                        {
                            Vector3 testPosition = instance.newPosition;

                            if (instance.id.Type == InstanceType.Building)
                            {
                                testPosition = CalculateSnapPosition(instance.newPosition, instance.newAngle,
                                    buildingBuffer[instance.id.Building].Length, buildingBuffer[instance.id.Building].Width);
                            }

                            segmentBuffer[segment].GetClosestZoneBlock(testPosition, ref distanceSq, ref block);

                            if (block != previousBlock)
                            {
                                refPosition = testPosition;

                                if (instance.id.Type == InstanceType.NetNode)
                                {
                                    if (nodeBuffer[instance.id.NetNode].Info.m_halfWidth <= 4f)
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
                Snap(ref newPosition, zoneBlock.m_position, zoneBlock.m_angle, smallRoad);

                moveDelta = moveDelta + newPosition - refPosition;
            }
            else if ((ToolManager.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None)
            {
                Vector3 assetGridPosition = Vector3.zero;
                float testMagnitude = 0;

                foreach (Moveable instance in step.instances)
                {
                    Vector3 testPosition = instance.newPosition;

                    if (instance.id.Type == InstanceType.Building)
                    {
                        testPosition = CalculateSnapPosition(instance.newPosition, instance.newAngle,
                            buildingBuffer[instance.id.Building].Length, buildingBuffer[instance.id.Building].Width);
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

                moveDelta = moveDelta + assetGridPosition - refPosition;
            }

            return moveDelta;
        }

        private Vector3 CalculateSnapPosition(Vector3 position, float angle, int length, int width)
        {
            float x = 0;
            float z = length * 4f;

            if (width % 2 != 0) x = 4f;

            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);

            return position + new Vector3(ca * x - sa * z, 0f, sa * x + ca * z);
        }

        private void Snap(ref Vector3 point, Vector3 refPoint, float refAngle, bool smallRoad)
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

        private bool ProcessMoveKeys(Event e, out Vector3 direction, out int angle)
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
                angle = angle - (int)magnitude * 20;
            }

            if (IsKeyDown(OptionsKeymapping.turnNeg, e))
            {
                angle = angle + (int)magnitude * 20;
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

        private void UpdateArea(Bounds bounds)
        {
            bounds.Expand(64f);

            BuildingManager.instance.ZonesUpdated(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            ZoneManager.instance.UpdateBlocks(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            PropManager.instance.UpdateProps(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            TreeManager.instance.UpdateTrees(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            VehicleManager.instance.UpdateParkedVehicles(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

            bounds.Expand(64f);

            ElectricityManager.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            WaterManager.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

            TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
            UpdateRender(bounds);
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

            return elapsed / m_stopWatchfrequency;
        }

        private void UpdateRender(Bounds bounds)
        {
            int num1 = Mathf.Clamp((int)(bounds.min.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(bounds.min.z / 64f + 135f), 0, 269);
            int x0 = num1 * 45 / 270 - 1;
            int z0 = num2 * 45 / 270 - 1;

            num1 = Mathf.Clamp((int)(bounds.max.x / 64f + 135f), 0, 269);
            num2 = Mathf.Clamp((int)(bounds.max.z / 64f + 135f), 0, 269);
            int x1 = num1 * 45 / 270 + 1;
            int z1 = num2 * 45 / 270 + 1;

            RenderManager renderManager = RenderManager.instance;
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

        private bool IsKeyDown(SavedInputKey inputKey, Event e)
        {
            int code = inputKey.value;
            KeyCode keyCode = (KeyCode)(code & 0xFFFFFFF);

            bool ctrl = ((code & 0x40000000) != 0);

            return Input.GetKey(keyCode) && ctrl == e.control;
        }

        private Bounds GetTotalBounds(bool ignoreSegments = true)
        {
            Bounds totalBounds = default(Bounds);

            bool init = false;

            foreach (Moveable instance in m_moves.current.instances)
            {
                if (init)
                {
                    totalBounds.Encapsulate(instance.GetBounds(ignoreSegments));
                }
                else
                {
                    totalBounds = instance.GetBounds(ignoreSegments);
                    init = true;
                }
            }

            return totalBounds;
        }

        private bool isLeft(Vector3 P0, Vector3 P1, Vector3 P2)
        {
            return ((P1.x - P0.x) * (P2.z - P0.z) - (P2.x - P0.x) * (P1.z - P0.z)) > 0;
        }

        private bool PointInRectangle(Quad3 rectangle, Vector3 p)
        {
            return isLeft(rectangle.a, rectangle.b, p) && isLeft(rectangle.b, rectangle.c, p) && isLeft(rectangle.c, rectangle.d, p) && isLeft(rectangle.d, rectangle.a, p);
        }

        private void RenderInstanceOverlay(RenderManager.CameraInfo cameraInfo, InstanceID id, Color toolColor)
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        ushort building = id.Building;
                        NetManager netManager = NetManager.instance;
                        BuildingManager buildingManager = BuildingManager.instance;
                        BuildingInfo buildingInfo = buildingManager.m_buildings.m_buffer[building].Info;
                        float alpha = 1f;
                        BuildingTool.CheckOverlayAlpha(buildingInfo, ref alpha);
                        ushort node = buildingManager.m_buildings.m_buffer[building].m_netNode;
                        int count = 0;
                        while (node != 0)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[node].GetSegment(j);
                                if (segment != 0 && netManager.m_segments.m_buffer[segment].m_startNode == node && (netManager.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
                                {
                                    NetTool.CheckOverlayAlpha(ref netManager.m_segments.m_buffer[segment], ref alpha);
                                }
                            }
                            node = netManager.m_nodes.m_buffer[node].m_nextBuildingNode;
                            if (++count > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        ushort subBuilding = buildingManager.m_buildings.m_buffer[building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingTool.CheckOverlayAlpha(buildingManager.m_buildings.m_buffer[subBuilding].Info, ref alpha);
                            subBuilding = buildingManager.m_buildings.m_buffer[subBuilding].m_subBuilding;
                            if (++count > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        toolColor.a *= alpha;
                        int length = buildingManager.m_buildings.m_buffer[building].Length;
                        Vector3 position = buildingManager.m_buildings.m_buffer[building].m_position;
                        float angle = buildingManager.m_buildings.m_buffer[building].m_angle;
                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, position, angle, toolColor, false);

                        node = buildingManager.m_buildings.m_buffer[building].m_netNode;
                        count = 0;
                        while (node != 0)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                ushort segment2 = netManager.m_nodes.m_buffer[node].GetSegment(k);
                                if (segment2 != 0 && netManager.m_segments.m_buffer[segment2].m_startNode == node && (netManager.m_segments.m_buffer[segment2].m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
                                {
                                    NetTool.RenderOverlay(cameraInfo, ref netManager.m_segments.m_buffer[segment2], toolColor, toolColor);
                                }
                            }
                            node = netManager.m_nodes.m_buffer[node].m_nextBuildingNode;
                            if (++count > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        subBuilding = buildingManager.m_buildings.m_buffer[building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingInfo subBuildingInfo = buildingManager.m_buildings.m_buffer[subBuilding].Info;
                            int subLength = buildingManager.m_buildings.m_buffer[subBuilding].Length;
                            Vector3 subPosition = buildingManager.m_buildings.m_buffer[subBuilding].m_position;
                            float subAngle = buildingManager.m_buildings.m_buffer[subBuilding].m_angle;
                            BuildingTool.RenderOverlay(cameraInfo, subBuildingInfo, subLength, subPosition, subAngle, toolColor, false);
                            subBuilding = buildingManager.m_buildings.m_buffer[subBuilding].m_subBuilding;
                            if (++count > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        break;
                    }
                case InstanceType.Prop:
                    {
                        ushort prop = id.Prop;
                        PropManager propManager = PropManager.instance;
                        PropInfo propInfo = propManager.m_props.m_buffer[prop].Info;
                        Vector3 position = propManager.m_props.m_buffer[prop].Position;
                        float angle = propManager.m_props.m_buffer[prop].Angle;
                        Randomizer randomizer = new Randomizer((int)prop);
                        float scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                        float alpha = 1f;
                        PropTool.CheckOverlayAlpha(propInfo, scale, ref alpha);
                        toolColor.a *= alpha;
                        PropTool.RenderOverlay(cameraInfo, propInfo, position, scale, angle, toolColor);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        uint tree = id.Tree;
                        TreeManager treeManager = TreeManager.instance;
                        TreeInfo treeInfo = treeManager.m_trees.m_buffer[tree].Info;
                        Vector3 position = treeManager.m_trees.m_buffer[tree].Position;
                        Randomizer randomizer = new Randomizer(tree);
                        float scale = treeInfo.m_minScale + (float)randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                        float alpha = 1f;
                        TreeTool.CheckOverlayAlpha(treeInfo, scale, ref alpha);
                        toolColor.a *= alpha;
                        TreeTool.RenderOverlay(cameraInfo, treeInfo, position, scale, toolColor);
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        ushort node = id.NetNode;
                        NetManager netManager = NetManager.instance;
                        NetInfo netInfo = netManager.m_nodes.m_buffer[node].Info;
                        Vector3 position = netManager.m_nodes.m_buffer[node].m_position;
                        Randomizer randomizer = new Randomizer(node);
                        float alpha = 1f;
                        NetTool.CheckOverlayAlpha(netInfo, ref alpha);
                        toolColor.a *= alpha;
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, toolColor, position, netInfo.m_halfWidth * 2f, position.y - 1f, position.y + 1f, true, true);
                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        ushort segment = id.NetSegment;
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

                        NetInfo netInfo = segmentBuffer[segment].Info;

                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        bool smoothStart = ((nodeBuffer[startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);
                        bool smoothEnd = ((nodeBuffer[endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);

                        Bezier3 bezier;
                        bezier.a = nodeBuffer[startNode].m_position;
                        bezier.d = nodeBuffer[endNode].m_position;

                        NetSegment.CalculateMiddlePoints(
                            bezier.a, segmentBuffer[segment].m_startDirection,
                            bezier.d, segmentBuffer[segment].m_endDirection,
                            smoothStart, smoothEnd, out bezier.b, out bezier.c);

                        RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, toolColor, bezier, netInfo.m_halfWidth * 4f / 3f, 100000f, -100000f, -1f, 1280f, true, true);

                        Segment3 segment1, segment2;

                        segment1.a = nodeBuffer[startNode].m_position;
                        segment2.a = nodeBuffer[endNode].m_position;

                        segment1.b = GetControlPoint(segment);
                        segment2.b = segment1.b;

                        toolColor.a = toolColor.a / 2;
                        RenderManager.instance.OverlayEffect.DrawSegment(cameraInfo, toolColor, segment1, segment2, 0, 10f, -1f, 1280f, true, true);
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, toolColor, segment1.b, netInfo.m_halfWidth / 2f, -1f, 1280f, true, true);

                        break;
                    }
            }
        }

        public static Vector3 GetControlPoint(ushort segment)
        {
            NetManager netManager = NetManager.instance;
            NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
            NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

            Vector3 startPos = nodeBuffer[segmentBuffer[segment].m_startNode].m_position;
            Vector3 startDir = segmentBuffer[segment].m_startDirection;
            Vector3 endPos = nodeBuffer[segmentBuffer[segment].m_endNode].m_position;
            Vector3 endDir = segmentBuffer[segment].m_endDirection;

            float num;
            if (!NetSegment.IsStraight(startPos, startDir, endPos, endDir, out num))
            {
                float dot = startDir.x * endDir.x + startDir.z * endDir.z;
                float u;
                float v;
                if (dot >= -0.999f && Line2.Intersect(VectorUtils.XZ(startPos), VectorUtils.XZ(startPos + startDir), VectorUtils.XZ(endPos), VectorUtils.XZ(endPos + endDir), out u, out v))
                {
                    return startPos + startDir * u;
                }
            }

            return (startPos + endPos) / 2f;
        }
    }
}
