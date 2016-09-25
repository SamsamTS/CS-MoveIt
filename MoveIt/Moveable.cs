using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;

using ColossalFramework;

namespace MoveIt
{
    internal class Moveable
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
