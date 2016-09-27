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
                MoveItTool.instance.enabled = false;
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
        public const string settingsFileName = "MoveItTool";

        public static MoveItTool instance;
        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);

        private static Color m_hoverColor = new Color32(0, 181, 255, 255);
        private static Color m_selectedColor = new Color32(95, 166, 0, 244);

        private Moveable m_hoverInstance;
        private ToolBase m_prevTool;
        private UIMoveItButton m_button;

        private long m_keyTime;
        private long m_rightClickTime;
        private long m_leftClickTime;
        private long m_stopWatchfrequency = Stopwatch.Frequency / 1000;

        private long ElapsedMilliseconds(long startTime)
        {
            long endTime = Stopwatch.GetTimestamp();
            long elapsed;

            if(endTime > startTime)
            {
                elapsed = endTime - startTime;
            }
            else
            {
                elapsed = startTime - endTime;
            }

            return elapsed / m_stopWatchfrequency;
        }

        //private bool m_mouseDragging = false;
        private Vector3 m_startPosition;
        private Vector3 m_mouseStartPosition;

        //private bool m_mouseRotating = false;
        private float m_mouseStartX;
        private ushort m_startAngle;


        private struct MoveStep
        {
            public List<Moveable> instances;
            public Vector3 moveDelta;
            public ushort angleDelta;
            public Vector3 center;

            public bool hasMoved
            {
                get
                {
                    return moveDelta != Vector3.zero || angleDelta != 0;
                }
            }
        }

        private MoveStep[] m_moves = new MoveStep[50];
        private int m_moveCurrent = -1;
        private int m_moveHead = -1;
        private int m_moveTail = 0;

        private enum Actions
        {
            None,
            Undo,
            Redo,
            Transform
        }

        private Actions m_nextAction = Actions.None;

        protected override void Awake()
        {
            m_toolController = GameObject.FindObjectOfType<ToolController>();

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;
        }

        protected override void OnEnable()
        {
            m_prevTool = m_toolController.CurrentTool;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_toolController.NextTool == null && m_prevTool != null)
                m_prevTool.enabled = true;

            m_prevTool = null;
        }

        protected override void OnToolUpdate()
        {
            if (!this.m_toolController.IsInsideUI && Cursor.visible)
            {
                lock (m_moves)
                {
                    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastInput input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
                    RaycastOutput output;

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
                                id.Building = building;
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

                    if (m_nextAction == Actions.None)
                    {
                        if (m_rightClickTime != 0 && ElapsedMilliseconds(m_rightClickTime) > 200)
                        {
                            if (m_moveCurrent != -1)
                            {
                                m_moves[m_moveCurrent].angleDelta = (ushort)(m_startAngle + (ushort)(ushort.MaxValue * (Input.mousePosition.x - m_mouseStartX) / Screen.width));
                                m_nextAction = Actions.Transform;
                            }
                        }
                        else if (m_leftClickTime != 0 && ElapsedMilliseconds(m_leftClickTime) > 200)
                        {
                            input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
                            input.m_ignoreTerrain = false;
                            ToolBase.RayCast(input, out output);

                            if (m_moveCurrent != -1)
                            {
                                m_moves[m_moveCurrent].moveDelta = m_startPosition + output.m_hitPos - m_mouseStartPosition;
                                m_moves[m_moveCurrent].moveDelta.y = 0;
                                m_nextAction = Actions.Transform;
                            }
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        m_leftClickTime = 0;
                    }

                    if (Input.GetMouseButtonDown(0) && m_hoverInstance != null)
                    {
                        if (m_moveCurrent == -1)
                        {
                            m_moveCurrent = 0;
                            m_moveTail = 0;
                            m_moveHead = 0;
                            m_moves[m_moveCurrent].instances = new List<Moveable>();
                            m_moves[m_moveCurrent].moveDelta = Vector3.zero;
                            m_moves[m_moveCurrent].angleDelta = 0;
                        }

                        if (Event.current.shift)
                        {
                            if (m_moves[m_moveCurrent].hasMoved)
                            {
                                int previous = m_moveCurrent;

                                NextMove();

                                foreach (Moveable instance in m_moves[previous].instances)
                                {
                                    m_moves[m_moveCurrent].instances.Add(new Moveable(instance.id));
                                }
                            }

                            if (m_moves[m_moveCurrent].instances.Contains(m_hoverInstance))
                            {
                                m_moves[m_moveCurrent].instances.Remove(m_hoverInstance);
                            }
                            else
                            {
                                m_moves[m_moveCurrent].instances.Add(m_hoverInstance);
                            }

                            if (m_moves[m_moveCurrent].instances.Count > 0)
                            {
                                m_moves[m_moveCurrent].center = GetTotalBounds().center;
                            }
                        }
                        else
                        {

                            if (!m_moves[m_moveCurrent].instances.Contains(m_hoverInstance))
                            {
                                if (m_moves[m_moveCurrent].hasMoved)
                                {
                                    NextMove();
                                }

                                m_moves[m_moveCurrent].instances.Clear();
                                m_moves[m_moveCurrent].instances.Add(m_hoverInstance);
                                m_moves[m_moveCurrent].center = GetTotalBounds().center;
                            }

                            input = new RaycastInput(mouseRay, Camera.main.farClipPlane);
                            input.m_ignoreTerrain = false;
                            ToolBase.RayCast(input, out output);

                            m_mouseStartPosition = output.m_hitPos;
                            m_startPosition = m_moves[m_moveCurrent].moveDelta;
                            m_leftClickTime = Stopwatch.GetTimestamp();
                        }
                    }

                    if (Input.GetMouseButtonDown(1) && m_moveCurrent != -1)
                    {
                        m_rightClickTime = Stopwatch.GetTimestamp();
                        m_mouseStartX = Input.mousePosition.x;
                        m_startAngle = m_moves[m_moveCurrent].angleDelta;
                    }

                    if (Input.GetMouseButtonUp(1))
                    {
                        m_rightClickTime = 0;

                        if (Input.mousePosition.x - m_mouseStartX == 0)
                        {
                            if (m_moveCurrent != -1)
                            {
                                if (m_moves[m_moveCurrent].hasMoved)
                                {
                                    NextMove();
                                }
                                else
                                {
                                    m_moves[m_moveCurrent].instances.Clear();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnToolGUI(Event e)
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
            else if (m_moveCurrent != -1 && m_moves[m_moveCurrent].instances.Count > 0)
            {
                Vector3 direction = Vector3.zero;
                int angle = 0;

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
                    bool shouldMove = false;

                    if (m_keyTime == 0)
                    {
                        m_keyTime = Stopwatch.GetTimestamp();
                        shouldMove = true;
                    }
                    else if(ElapsedMilliseconds(m_keyTime) >= 250)
                    {
                        shouldMove = true;
                    }

                    if (shouldMove)
                    {
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

                        lock (m_moves)
                        {
                            m_moves[m_moveCurrent].moveDelta = m_moves[m_moveCurrent].moveDelta + direction;
                            m_moves[m_moveCurrent].angleDelta = (ushort)(m_moves[m_moveCurrent].angleDelta + angle);
                        }

                        m_nextAction = Actions.Transform;
                    }

                }
                else
                {
                    m_keyTime = 0;
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
            if (m_moveCurrent != -1 && m_moves[m_moveCurrent].instances.Count > 0)
            {
                foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                {
                    RenderInstanceOverlay(cameraInfo, instance.id, m_selectedColor);
                }

                Vector3 center = m_moves[m_moveCurrent].center + m_moves[m_moveCurrent].moveDelta;

                center.y = TerrainManager.instance.SampleRawHeightSmooth(center);
                RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, m_selectedColor, center, 1f, center.y - 1f, center.y + 1f, true, true);

                if (m_hoverInstance != null && !m_moves[m_moveCurrent].instances.Contains(m_hoverInstance))
                {
                    RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
                }
            }
            else if (m_hoverInstance != null)
            {
                RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
            }

            base.RenderOverlay(cameraInfo);
        }


        public void Undo()
        {
            lock (m_moves)
            {
                if (m_moveCurrent != -1)
                {
                    Bounds bounds = GetTotalBounds();
                    foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                    {
                        instance.Transform(Vector3.zero, 0, m_moves[m_moveCurrent].center);
                    }
                    TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);

                    if (m_moveCurrent == m_moveTail)
                    {
                        m_moveCurrent = -1;
                    }
                    else
                    {
                        m_moveCurrent = m_moveCurrent - 1;
                        if (m_moveCurrent < 0) m_moveCurrent = m_moves.Length - 1;
                    }
                }
            }
        }

        public void Redo()
        {
            lock (m_moves)
            {
                if (m_moveHead != -1 && m_moveCurrent != m_moveHead)
                {
                    if (m_moveCurrent == -1)
                    {
                        m_moveCurrent = m_moveTail;
                    }
                    else
                    {
                        m_moveCurrent = (m_moveCurrent + 1) % m_moves.Length;
                    }

                    Bounds bounds = GetTotalBounds();
                    foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                    {
                        instance.Transform(m_moves[m_moveCurrent].moveDelta, m_moves[m_moveCurrent].angleDelta, m_moves[m_moveCurrent].center);
                    }
                    TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
                }
            }
        }

        public void Transform()
        {
            lock (m_moves)
            {
                Bounds bounds = GetTotalBounds();
                foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                {
                    instance.Transform(m_moves[m_moveCurrent].moveDelta, m_moves[m_moveCurrent].angleDelta, m_moves[m_moveCurrent].center);
                }
                TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
            }
        }

        private void NextMove()
        {
            m_moveCurrent = (m_moveCurrent + 1) % m_moves.Length;
            m_moveHead = m_moveCurrent;
            if (m_moveTail == m_moveHead)
            {
                m_moveTail = (m_moveTail + 1) % m_moves.Length;
            }

            m_moves[m_moveCurrent].instances = new List<Moveable>();
            m_moves[m_moveCurrent].moveDelta = Vector3.zero;
            m_moves[m_moveCurrent].angleDelta = 0;
        }

        private bool IsKeyDown(SavedInputKey inputKey, Event e)
        {
            int code = inputKey.value;
            KeyCode keyCode = (KeyCode)(code & 0xFFFFFFF);

            bool ctrl = ((code & 0x40000000) != 0);

            return Input.GetKey(keyCode) && ctrl == e.control;
        }

        private Bounds GetTotalBounds()
        {
            Bounds totalBounds = default(Bounds);

            bool init = false;

            foreach (Moveable instance in m_moves[m_moveCurrent].instances)
            {
                if (init)
                {
                    totalBounds.Encapsulate(GetBounds(instance.id));
                }
                else
                {
                    totalBounds = GetBounds(instance.id);
                    init = true;
                }
            }

            return totalBounds;
        }

        private Bounds GetBounds(InstanceID instance)
        {
            switch (instance.Type)
            {
                case InstanceType.Building:
                    {
                        Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
                        NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

                        ushort id = instance.Building;
                        BuildingInfo info = buildingBuffer[id].Info;

                        float radius = Mathf.Max(info.m_cellWidth * 4f, info.m_cellLength * 4f);
                        Bounds bounds = new Bounds(buildingBuffer[id].m_position, new Vector3(radius, 0, radius));

                        ushort node = buildingBuffer[id].m_netNode;
                        while (node != 0)
                        {
                            if ((nodeBuffer[node].m_flags & NetNode.Flags.Fixed) == NetNode.Flags.None)
                            {
                                bounds.Encapsulate(nodeBuffer[node].m_bounds);
                            }

                            node = nodeBuffer[node].m_nextBuildingNode;
                        }

                        ushort subBuilding = buildingBuffer[id].m_subBuilding;
                        while (subBuilding != 0)
                        {
                            info = buildingBuffer[subBuilding].Info;
                            radius = Mathf.Max(info.m_cellWidth * 4f, info.m_cellLength * 4f);
                            bounds.Encapsulate(new Bounds(buildingBuffer[subBuilding].m_position, new Vector3(radius, 0, radius)));

                            subBuilding = buildingBuffer[subBuilding].m_subBuilding;
                        }

                        return bounds;
                    }
                case InstanceType.Prop:
                    {
                        PropInstance[] buffer = PropManager.instance.m_props.m_buffer;
                        ushort id = instance.Prop;
                        PropInfo info = buffer[id].Info;

                        Randomizer randomizer = new Randomizer(id);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                        float radius = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale;

                        return new Bounds(buffer[id].Position, new Vector3(radius, 0, radius));
                    }
                case InstanceType.Tree:
                    {
                        TreeInstance[] buffer = TreeManager.instance.m_trees.m_buffer;
                        uint id = instance.Tree;
                        TreeInfo info = buffer[id].Info;

                        Randomizer randomizer = new Randomizer(id);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                        float radius = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale;

                        return new Bounds(buffer[id].Position, new Vector3(radius, 0, radius));
                    }
                case InstanceType.NetNode:
                    {
                        return NetManager.instance.m_nodes.m_buffer[instance.NetNode].m_bounds;
                    }
            }

            return default(Bounds);
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

            }
        }
    }
}
