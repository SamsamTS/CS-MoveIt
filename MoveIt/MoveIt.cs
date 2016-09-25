using ICities;
using UnityEngine;

using System;
using System.Threading;
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
        public static MoveItTool instance;

        private static Color m_hoverColor = new Color32(0, 181, 255, 255);
        private static Color m_selectedColor = new Color32(95, 166, 0, 244);

        public const string settingsFileName = "MoveItTool";

        public static SavedBool useCardinalMoves = new SavedBool("useCardinalMoves", settingsFileName, false, true);

        private class Moveable
        {
            public InstanceID id;
            private Vector3 m_startPosition;
            private float m_startAngle;
            private Vector3[] m_startNodeDirections;

            public List<Moveable> subInstances;

            public Vector3 position
            {
                get
                {
                    switch (id.Type)
                    {
                        case InstanceType.Building:
                            {
                                return BuildingManager.instance.m_buildings.m_buffer[id.Building].m_position;
                            }
                        case InstanceType.Prop:
                            {
                                return PropManager.instance.m_props.m_buffer[id.Prop].Position;
                            }
                        case InstanceType.Tree:
                            {
                                return TreeManager.instance.m_trees.m_buffer[id.Tree].Position;
                            }
                        case InstanceType.NetNode:
                            {
                                return NetManager.instance.m_nodes.m_buffer[id.NetNode].m_position;
                            }
                    }

                    return Vector3.zero;
                }
            }

            public Moveable(InstanceID instance)
            {
                id = instance;

                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
                            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

                            m_startPosition = buildingBuffer[id.Building].m_position;
                            m_startAngle = buildingBuffer[id.Building].m_angle;

                            subInstances = new List<Moveable>();

                            ushort node = buildingBuffer[id.Building].m_netNode;
                            while (node != 0)
                            {
                                if ((nodeBuffer[node].m_flags & NetNode.Flags.Fixed) == NetNode.Flags.None)
                                {
                                    InstanceID nodeID = default(InstanceID);
                                    nodeID.NetNode = node;
                                    subInstances.Add(new Moveable(nodeID));
                                }

                                node = nodeBuffer[node].m_nextBuildingNode;
                            }

                            ushort building = buildingBuffer[id.Building].m_subBuilding;
                            while (building != 0)
                            {
                                Moveable subBuilding = new Moveable(InstanceID.Empty);
                                subBuilding.id.Building = building;
                                subBuilding.m_startPosition = buildingBuffer[building].m_position;
                                subBuilding.m_startAngle = buildingBuffer[building].m_angle;
                                subInstances.Add(subBuilding);

                                building = buildingBuffer[building].m_subBuilding;
                            }

                            if (subInstances.Count == 0)
                            {
                                subInstances = null;
                            }
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            m_startPosition = PropManager.instance.m_props.m_buffer[id.Prop].Position;
                            m_startAngle = PropManager.instance.m_props.m_buffer[id.Prop].m_angle;
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            m_startPosition = TreeManager.instance.m_trees.m_buffer[id.Tree].Position;
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            NetManager netManager = NetManager.instance;

                            m_startPosition = netManager.m_nodes.m_buffer[id.NetNode].m_position;

                            m_startNodeDirections = new Vector3[8];
                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[id.NetNode].GetSegment(i);
                                if (segment != 0)
                                {
                                    m_startNodeDirections[i] = netManager.m_segments.m_buffer[segment].GetDirection(id.NetNode);
                                }
                            }

                            break;
                        }
                }
            }

            private void RelocateBuilding(ushort building, ref Building data, Vector3 position, float angle)
            {
                BuildingManager buildingManager = BuildingManager.instance;

                BuildingInfo info = data.Info;
                RemoveFromGrid(building, ref data);
                data.UpdateBuilding(building);
                buildingManager.UpdateBuildingRenderer(building, true);
                if (info.m_hasParkingSpaces)
                {
                    buildingManager.UpdateParkingSpaces(building, ref data);
                }
                data.m_position = position;
                data.m_angle = angle;

                AddToGrid(building, ref data);
                data.CalculateBuilding(building);
                data.UpdateBuilding(building);
                buildingManager.UpdateBuildingRenderer(building, true);
            }

            public void Transform(Vector3 deltaPosition, ushort deltaAngle, Vector3 center)
            {
                float fAngle = deltaAngle * 9.58738E-05f;

                Matrix4x4 matrix4x = default(Matrix4x4);
                matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

                Vector3 newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;

                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            BuildingManager buildingManager = BuildingManager.instance;
                            NetManager netManager = NetManager.instance;
                            ushort building = id.Building;

                            RelocateBuilding(building, ref buildingManager.m_buildings.m_buffer[building], newPosition, m_startAngle + fAngle);

                            if (subInstances != null)
                            {
                                matrix4x.SetTRS(newPosition, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

                                foreach (Moveable subInstance in subInstances)
                                {
                                    Vector3 subPosition = subInstance.m_startPosition - m_startPosition;
                                    subPosition = matrix4x.MultiplyPoint(subPosition);

                                    subInstance.Move(subPosition, deltaAngle);
                                }
                            }

                            break;
                        }
                    case InstanceType.Prop:
                        {
                            PropManager.instance.m_props.m_buffer[id.Prop].m_angle = (ushort)((ushort)m_startAngle + deltaAngle);
                            PropManager.instance.MoveProp(id.Prop, newPosition);
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            TreeManager.instance.MoveTree(id.Tree, newPosition);
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            NetManager netManager = NetManager.instance;
                            ushort node = id.NetNode;

                            float netAngle = deltaAngle * 9.58738E-05f;

                            matrix4x.SetTRS(m_startPosition, Quaternion.AngleAxis(netAngle * 57.29578f, Vector3.down), Vector3.one);

                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[node].GetSegment(i);
                                if (segment != 0)
                                {
                                    Vector3 newDirection = matrix4x.MultiplyVector(m_startNodeDirections[i]);

                                    if (netManager.m_segments.m_buffer[segment].m_startNode == node)
                                    {
                                        netManager.m_segments.m_buffer[segment].m_startDirection = newDirection;
                                    }
                                    else
                                    {
                                        netManager.m_segments.m_buffer[segment].m_endDirection = newDirection;
                                    }
                                }
                            }
                            netManager.MoveNode(node, newPosition);
                            break;
                        }
                }
            }

            private void Move(Vector3 location, ushort delta)
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            BuildingManager buildingManager = BuildingManager.instance;
                            ushort building = id.Building;

                            RelocateBuilding(building, ref buildingManager.m_buildings.m_buffer[building], location, m_startAngle + delta * 9.58738E-05f);
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            PropManager.instance.m_props.m_buffer[id.Prop].m_angle = (ushort)((ushort)m_startAngle + delta);
                            PropManager.instance.MoveProp(id.Prop, location);
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            TreeManager.instance.MoveTree(id.Tree, location);
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            NetManager netManager = NetManager.instance;

                            float netAngle = delta * 9.58738E-05f;

                            Matrix4x4 matrix4x = default(Matrix4x4);
                            matrix4x.SetTRS(m_startPosition, Quaternion.AngleAxis(netAngle * 57.29578f, Vector3.down), Vector3.one);

                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[id.NetNode].GetSegment(i);
                                if (segment != 0)
                                {
                                    Vector3 newDirection = matrix4x.MultiplyVector(m_startNodeDirections[i]);

                                    if (netManager.m_segments.m_buffer[segment].m_startNode == id.NetNode)
                                    {
                                        netManager.m_segments.m_buffer[segment].m_startDirection = newDirection;
                                    }
                                    else
                                    {
                                        netManager.m_segments.m_buffer[segment].m_endDirection = newDirection;
                                    }
                                }
                            }

                            NetManager.instance.MoveNode(id.NetNode, location);
                            break;
                        }
                }
            }

            public override bool Equals(object obj)
            {
                Moveable instance = obj as Moveable;
                if (instance != null)
                {
                    return instance.id == id;
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }
        }

        private Moveable m_hoverInstance;

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

        private UIMoveItButton m_button;

        private ToolBase m_prevTool;

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

        protected override void Awake()
        {
            m_toolController = GameObject.FindObjectOfType<ToolController>();

            m_button = UIView.GetAView().AddUIComponent(typeof(UIMoveItButton)) as UIMoveItButton;
        }

        protected override void OnToolUpdate()
        {
            if (!this.m_toolController.IsInsideUI && Cursor.visible)
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

                if (Input.GetMouseButtonUp(0) && m_hoverInstance != null)
                {
                    lock (m_moves)
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
                            if (m_moves[m_moveCurrent].hasMoved)
                            {
                                NextMove();
                            }

                            m_moves[m_moveCurrent].instances.Clear();
                            m_moves[m_moveCurrent].instances.Add(m_hoverInstance);
                            m_moves[m_moveCurrent].center = GetTotalBounds().center;
                        }
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    //enabled = false;
                    lock (m_moves)
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
                if (e.alt) magnitude = 1f;

                OptionsKeymapping.moveXpos.Alt = e.alt;
                OptionsKeymapping.moveXneg.Alt = e.alt;
                OptionsKeymapping.moveYpos.Alt = e.alt;
                OptionsKeymapping.moveYneg.Alt = e.alt;
                OptionsKeymapping.moveZpos.Alt = e.alt;
                OptionsKeymapping.moveZneg.Alt = e.alt;
                OptionsKeymapping.turnPos.Alt = e.alt;
                OptionsKeymapping.turnNeg.Alt = e.alt;

                if (OptionsKeymapping.moveXpos.IsPressed(e))
                {
                    direction.x = direction.x + magnitude;
                }

                if (OptionsKeymapping.moveXneg.IsPressed(e))
                {
                    direction.x = direction.x - magnitude;
                }

                if (OptionsKeymapping.moveYpos.IsPressed(e))
                {
                    direction.y = direction.y + magnitude;
                }

                if (OptionsKeymapping.moveYneg.IsPressed(e))
                {
                    direction.y = direction.y - magnitude;
                }

                if (OptionsKeymapping.moveZpos.IsPressed(e))
                {
                    direction.z = direction.z + magnitude;
                }

                if (OptionsKeymapping.moveZneg.IsPressed(e))
                {
                    direction.z = direction.z - magnitude;
                }

                if (OptionsKeymapping.turnPos.IsPressed(e))
                {
                    angle = angle + (int)magnitude * 20;
                }

                if (OptionsKeymapping.turnNeg.IsPressed(e))
                {
                    angle = angle - (int)magnitude * 20;
                }

                OptionsKeymapping.moveXpos.Alt = false;
                OptionsKeymapping.moveXneg.Alt = false;
                OptionsKeymapping.moveYpos.Alt = false;
                OptionsKeymapping.moveYneg.Alt = false;
                OptionsKeymapping.moveZpos.Alt = false;
                OptionsKeymapping.moveZneg.Alt = false;
                OptionsKeymapping.turnPos.Alt = false;
                OptionsKeymapping.turnNeg.Alt = false;

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

                    lock (m_moves)
                    {
                        m_moves[m_moveCurrent].moveDelta = m_moves[m_moveCurrent].moveDelta + direction;
                    }
                    m_nextAction = Actions.Transform;
                }

                if (angle != 0)
                {
                    lock (m_moves)
                    {
                        m_moves[m_moveCurrent].angleDelta = (ushort)(m_moves[m_moveCurrent].angleDelta + angle);
                    }
                    m_nextAction = Actions.Transform;
                }
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

        private static void AddToGrid(ushort building, ref Building data)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                buildingManager.m_buildings.m_buffer[(int)building].m_nextGridBuilding = buildingManager.m_buildingGrid[num3];
                buildingManager.m_buildingGrid[num3] = building;
            }
            finally
            {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
        }

        private static void RemoveFromGrid(ushort building, ref Building data)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                ushort num4 = 0;
                ushort num5 = buildingManager.m_buildingGrid[num3];
                int num6 = 0;
                while (num5 != 0)
                {
                    if (num5 == building)
                    {
                        if (num4 == 0)
                        {
                            buildingManager.m_buildingGrid[num3] = data.m_nextGridBuilding;
                        }
                        else
                        {
                            buildingManager.m_buildings.m_buffer[(int)num4].m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = buildingManager.m_buildings.m_buffer[(int)num5].m_nextGridBuilding;
                    if (++num6 > 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                data.m_nextGridBuilding = 0;
            }
            finally
            {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
            if (info != null)
            {
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 270, num2 * 45 / 270, info.m_prefabDataLayer);
            }
        }
    }
}
