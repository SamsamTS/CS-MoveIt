using ICities;
using UnityEngine;

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

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

        private class Moveable
        {
            public InstanceID id;
            private Vector3 position;
            private float angle;

            public Moveable(InstanceID instance)
            {
                id = instance;

                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            position = BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].m_position;
                            angle = BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].m_angle;
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            position = PropManager.instance.m_props.m_buffer[(int)id.Prop].Position;
                            angle = PropManager.instance.m_props.m_buffer[(int)id.Prop].m_angle;
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            position = TreeManager.instance.m_trees.m_buffer[(int)id.Tree].Position;
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            position = NetManager.instance.m_nodes.m_buffer[(int)id.NetNode].m_position;
                            break;
                        }
                }
            }

            public void Move(Vector3 delta)
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            BuildingManager buildingManager = BuildingManager.instance;
                            NetManager netManager = NetManager.instance;

                            Vector3 currentPos = buildingManager.m_buildings.m_buffer[id.Building].m_position;
                            Vector3 newPos = position + delta;
                            Vector3 moveDelta = newPos - currentPos;

                            BuildingManager.instance.m_buildings.m_buffer[id.Building].m_position = position + delta;

                            ushort node = buildingManager.m_buildings.m_buffer[id.Building].m_netNode;
                            while (node != 0)
                            {
                                netManager.MoveNode(node, netManager.m_nodes.m_buffer[node].m_position + moveDelta);
                                node = netManager.m_nodes.m_buffer[node].m_nextBuildingNode;

                            }

                            ushort subBuilding = buildingManager.m_buildings.m_buffer[id.Building].m_subBuilding;

                            while (subBuilding != 0)
                            {
                                buildingManager.m_buildings.m_buffer[subBuilding].m_position = buildingManager.m_buildings.m_buffer[subBuilding].m_position + moveDelta;
                                subBuilding = buildingManager.m_buildings.m_buffer[subBuilding].m_subBuilding;
                                buildingManager.UpdateBuilding(subBuilding);
                            }

                            BuildingManager.instance.UpdateBuilding(id.Building);
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            PropManager.instance.MoveProp(id.Prop, position + delta);
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            TreeManager.instance.MoveTree(id.Tree, position + delta);
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            NetManager.instance.MoveNode(id.NetNode, position + delta);
                            break;
                        }
                }
            }

            public void Rotate(ushort delta)
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].m_angle = angle + delta * 9.58738E-05f;
                            BuildingManager.instance.UpdateBuilding(id.Building);
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            PropManager.instance.m_props.m_buffer[(int)id.Prop].m_angle = (ushort)((ushort)angle + delta);
                            PropManager.instance.UpdateProp(id.Prop);
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

            public bool hasMove
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
            Undo,
            Redo,
            Move,
            Rotate
        }
        private Queue<Actions> m_actionQueue = new Queue<Actions>();

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
                input.m_ignoreNodeFlags = NetNode.Flags.None;
                input.m_ignoreBuildingFlags = Building.Flags.None;
                input.m_ignorePropFlags = PropInstance.Flags.None;
                input.m_ignoreTreeFlags = TreeInstance.Flags.None;

                m_hoverInstance = null;

                if (ToolBase.RayCast(input, out output))
                {
                    InstanceID id = default(InstanceID);
                    if (output.m_netNode != 0)
                    {
                        id.NetNode = output.m_netNode;
                        m_hoverInstance = new Moveable(id);
                    }
                    else if (output.m_netSegment != 0)
                    {
                        NetManager netManager = NetManager.instance;

                        NetSegment netSegment = netManager.m_segments.m_buffer[(int)output.m_netSegment];
                        NetNode startNode = netManager.m_nodes.m_buffer[(int)netSegment.m_startNode];
                        NetNode endNode = netManager.m_nodes.m_buffer[(int)netSegment.m_endNode];

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
                            if (m_moves[m_moveCurrent].hasMove)
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
                        }
                        else
                        {
                            if (m_moves[m_moveCurrent].hasMove)
                            {
                                NextMove();
                            }

                            m_moves[m_moveCurrent].instances.Clear();
                            m_moves[m_moveCurrent].instances.Add(m_hoverInstance);
                        }
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    enabled = false;
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
                if (m_actionQueue.Count > 0)
                {
                    switch (m_actionQueue.Dequeue())
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
                        case Actions.Move:
                            {
                                Move();
                                break;
                            }
                        case Actions.Rotate:
                            {
                                Rotate();
                                break;
                            }
                    }
                }
            }
        }

        public void Undo()
        {
            lock (m_moves)
            {
                if (m_moveCurrent != -1)
                {
                    foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                    {
                        instance.Move(Vector3.zero);
                        instance.Rotate(0);
                    }

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

                    foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                    {
                        instance.Move(m_moves[m_moveCurrent].moveDelta);
                        instance.Rotate(m_moves[m_moveCurrent].angleDelta);
                    }
                }
            }
        }

        public void Move()
        {
            lock (m_moves)
            {
                foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                {
                    instance.Move(m_moves[m_moveCurrent].moveDelta);
                }
            }
        }

        public void Rotate()
        {
            lock (m_moves)
            {
                foreach (Moveable instance in m_moves[m_moveCurrent].instances)
                {
                    instance.Rotate(m_moves[m_moveCurrent].angleDelta);
                }
            }
        }

        protected override void OnToolGUI(Event e)
        {
            if (OptionsKeymapping.undo.IsPressed(e))
            {
                m_actionQueue.Enqueue(Actions.Undo);
            }
            else if (OptionsKeymapping.redo.IsPressed(e))
            {
                m_actionQueue.Enqueue(Actions.Redo);
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

                    m_moves[m_moveCurrent].moveDelta = m_moves[m_moveCurrent].moveDelta + direction;
                    m_actionQueue.Enqueue(Actions.Move);
                }

                if (angle != 0)
                {
                    m_moves[m_moveCurrent].angleDelta = (ushort)(m_moves[m_moveCurrent].angleDelta + angle);
                    m_actionQueue.Enqueue(Actions.Rotate);
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

                if (m_hoverInstance != null && !m_moves[m_moveCurrent].instances.Contains(m_hoverInstance))
                    RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
            }
            else if (m_hoverInstance != null)
            {
                RenderInstanceOverlay(cameraInfo, m_hoverInstance.id, m_hoverColor);
            }

            base.RenderOverlay(cameraInfo);
        }

        private void MovePosition(InstanceID id, Vector3 direction)
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        Vector3 position = BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].m_position;
                        position.x = position.x + direction.x * 0.263671875f;
                        position.y = position.y + direction.y * 0.015625f;
                        position.z = position.z + direction.z * 0.263671875f;
                        BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].m_position = position;
                        BuildingManager.instance.UpdateBuilding(id.Building);
                        BuildingManager.instance.m_buildings.m_buffer[(int)id.Building].UpdateBuilding(id.Building);
                        break;
                    }
                case InstanceType.Prop:
                    {
                        PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posX = (short)(direction.x + PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posX);
                        PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posY = (ushort)(direction.y + PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posY);
                        PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posZ = (short)(direction.z + PropManager.instance.m_props.m_buffer[(int)id.Prop].m_posZ);
                        PropManager.instance.UpdateProp(id.Prop);
                        PropManager.instance.m_props.m_buffer[(int)id.Prop].UpdateProp(id.Prop);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posX = (short)(direction.x + TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posX);
                        TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posY = (ushort)(direction.y + TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posY);
                        TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posZ = (short)(direction.z + TreeManager.instance.m_trees.m_buffer[(int)id.Tree].m_posZ);
                        TreeManager.instance.UpdateTree(id.Tree);
                        TreeManager.instance.m_trees.m_buffer[(int)id.Tree].UpdateTree(id.Tree);
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        Vector3 position = NetManager.instance.m_nodes.m_buffer[(int)id.NetNode].m_position;
                        position.x = position.x + direction.x * 0.263671875f;
                        position.y = position.y + direction.y * 0.015625f;
                        position.z = position.z + direction.z * 0.263671875f;
                        NetManager.instance.m_nodes.m_buffer[(int)id.NetNode].m_position = position;
                        NetManager.instance.UpdateNode(id.NetNode);
                        NetManager.instance.m_nodes.m_buffer[(int)id.NetNode].UpdateNode(id.NetNode);
                        break;
                    }
            }
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
                        BuildingInfo buildingInfo = buildingManager.m_buildings.m_buffer[(int)building].Info;
                        float alpha = 1f;
                        BuildingTool.CheckOverlayAlpha(buildingInfo, ref alpha);
                        ushort node = buildingManager.m_buildings.m_buffer[(int)building].m_netNode;
                        int count = 0;
                        while (node != 0)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[(int)node].GetSegment(j);
                                if (segment != 0 && netManager.m_segments.m_buffer[(int)segment].m_startNode == node && (netManager.m_segments.m_buffer[(int)segment].m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
                                {
                                    NetTool.CheckOverlayAlpha(ref netManager.m_segments.m_buffer[(int)segment], ref alpha);
                                }
                            }
                            node = netManager.m_nodes.m_buffer[(int)node].m_nextBuildingNode;
                            if (++count > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        ushort subBuilding = buildingManager.m_buildings.m_buffer[(int)building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingTool.CheckOverlayAlpha(buildingManager.m_buildings.m_buffer[(int)subBuilding].Info, ref alpha);
                            subBuilding = buildingManager.m_buildings.m_buffer[(int)subBuilding].m_subBuilding;
                            if (++count > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        toolColor.a *= alpha;
                        int length = buildingManager.m_buildings.m_buffer[(int)building].Length;
                        Vector3 position = buildingManager.m_buildings.m_buffer[(int)building].m_position;
                        float angle = buildingManager.m_buildings.m_buffer[(int)building].m_angle;
                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, position, angle, toolColor, false);

                        node = buildingManager.m_buildings.m_buffer[(int)building].m_netNode;
                        count = 0;
                        while (node != 0)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                ushort segment2 = netManager.m_nodes.m_buffer[(int)node].GetSegment(k);
                                if (segment2 != 0 && netManager.m_segments.m_buffer[(int)segment2].m_startNode == node && (netManager.m_segments.m_buffer[(int)segment2].m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
                                {
                                    NetTool.RenderOverlay(cameraInfo, ref netManager.m_segments.m_buffer[(int)segment2], toolColor, toolColor);
                                }
                            }
                            node = netManager.m_nodes.m_buffer[(int)node].m_nextBuildingNode;
                            if (++count > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        subBuilding = buildingManager.m_buildings.m_buffer[(int)building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingInfo subBuildingInfo = buildingManager.m_buildings.m_buffer[(int)subBuilding].Info;
                            int subLength = buildingManager.m_buildings.m_buffer[(int)subBuilding].Length;
                            Vector3 subPosition = buildingManager.m_buildings.m_buffer[(int)subBuilding].m_position;
                            float subAngle = buildingManager.m_buildings.m_buffer[(int)subBuilding].m_angle;
                            BuildingTool.RenderOverlay(cameraInfo, subBuildingInfo, subLength, subPosition, subAngle, toolColor, false);
                            subBuilding = buildingManager.m_buildings.m_buffer[(int)subBuilding].m_subBuilding;
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
                        PropInfo propInfo = propManager.m_props.m_buffer[(int)prop].Info;
                        Vector3 position = propManager.m_props.m_buffer[(int)prop].Position;
                        float angle = propManager.m_props.m_buffer[(int)prop].Angle;
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
                        TreeInfo treeInfo = treeManager.m_trees.m_buffer[(int)((UIntPtr)tree)].Info;
                        Vector3 position = treeManager.m_trees.m_buffer[(int)((UIntPtr)tree)].Position;
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
                        NetInfo netInfo = netManager.m_nodes.m_buffer[(int)((UIntPtr)node)].Info;
                        Vector3 position = netManager.m_nodes.m_buffer[(int)((UIntPtr)node)].m_position;
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
