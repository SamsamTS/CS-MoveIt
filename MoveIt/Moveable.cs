using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.Math;

namespace MoveIt
{
    internal class Moveable
    {
        public InstanceID id;
        public HashSet<Moveable> subInstances;

        public Vector3 newPosition;
        public float newAngle;

        private Vector3 m_startPosition;
        private float m_terrainHeight;
        private float m_startAngle;

        private HashSet<ushort> m_segmentsHashSet = new HashSet<ushort>();
        private SegmentSave[] m_nodeSegmentsSave;

        private SegmentSave m_segmentSave;

        private struct SegmentSave
        {
            public Quaternion m_startRotation;
            public Quaternion m_endRotation;

            public Vector3 m_startDirection;
            public Vector3 m_endDirection;
        }

        public object data
        {
            get
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            return BuildingManager.instance.m_buildings.m_buffer[id.Building];
                        }
                    case InstanceType.Prop:
                        {
                            return PropManager.instance.m_props.m_buffer[id.Prop];
                        }
                    case InstanceType.Tree:
                        {
                            return TreeManager.instance.m_trees.m_buffer[id.Tree];
                        }
                    case InstanceType.NetNode:
                        {
                            return NetManager.instance.m_nodes.m_buffer[id.NetNode];
                        }
                    case InstanceType.NetSegment:
                        {
                            return NetManager.instance.m_segments.m_buffer[id.NetSegment];
                        }
                }

                return null;
            }
        }

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
                    case InstanceType.NetSegment:
                        {
                            return MoveItTool.GetControlPoint(id.NetSegment);
                        }
                }

                return Vector3.zero;
            }
        }

        public HashSet<ushort> segmentList
        {
            get
            {
                return m_segmentsHashSet;
            }
        }

        public bool isValid
        {
            get
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            return (BuildingManager.instance.m_buildings.m_buffer[id.Building].m_flags & Building.Flags.Created) != Building.Flags.None;
                        }
                    case InstanceType.Prop:
                        {
                            return PropManager.instance.m_props.m_buffer[id.Prop].m_flags != 0;
                        }
                    case InstanceType.Tree:
                        {
                            return TreeManager.instance.m_trees.m_buffer[id.Tree].m_flags != 0;
                        }
                    case InstanceType.NetNode:
                        {
                            return (NetManager.instance.m_nodes.m_buffer[id.NetNode].m_flags & NetNode.Flags.Created) != NetNode.Flags.None;
                        }
                    case InstanceType.NetSegment:
                        {
                            return (NetManager.instance.m_segments.m_buffer[id.NetSegment].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None;
                        }
                }

                return false;
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

                        subInstances = new HashSet<Moveable>();

                        ushort node = buildingBuffer[id.Building].m_netNode;
                        while (node != 0)
                        {
                            ItemClass.Layer layer = nodeBuffer[node].Info.m_class.m_layer;
                            if (layer != ItemClass.Layer.PublicTransport)
                            {
                                InstanceID nodeID = default(InstanceID);
                                nodeID.NetNode = node;
                                Moveable subInstance = new Moveable(nodeID);
                                subInstances.Add(subInstance);

                                m_segmentsHashSet.UnionWith(subInstance.m_segmentsHashSet);
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

                            node = buildingBuffer[building].m_netNode;
                            while (node != 0)
                            {
                                ItemClass.Layer layer = nodeBuffer[node].Info.m_class.m_layer;

                                if (layer != ItemClass.Layer.PublicTransport)
                                {
                                    InstanceID nodeID = default(InstanceID);
                                    nodeID.NetNode = node;
                                    Moveable subInstance = new Moveable(nodeID);
                                    subInstances.Add(subInstance);

                                    m_segmentsHashSet.UnionWith(subInstance.m_segmentsHashSet);
                                }

                                node = nodeBuffer[node].m_nextBuildingNode;
                            }

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
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
                        ushort node = id.NetNode;

                        m_startPosition = nodeBuffer[node].m_position;

                        m_nodeSegmentsSave = new SegmentSave[8];

                        for (int i = 0; i < 8; i++)
                        {
                            ushort segment = nodeBuffer[node].GetSegment(i);
                            if (segment != 0)
                            {
                                m_segmentsHashSet.Add(segment);

                                ushort startNode = segmentBuffer[segment].m_startNode;
                                ushort endNode = segmentBuffer[segment].m_endNode;

                                Vector3 segVector = nodeBuffer[endNode].m_position - nodeBuffer[startNode].m_position;
                                segVector.Normalize();

                                m_nodeSegmentsSave[i].m_startDirection = segmentBuffer[segment].m_startDirection;
                                m_nodeSegmentsSave[i].m_endDirection = segmentBuffer[segment].m_endDirection;

                                Vector3 startDirection = new Vector3(segmentBuffer[segment].m_startDirection.x, 0, segmentBuffer[segment].m_startDirection.z);
                                Vector3 endDirection = new Vector3(segmentBuffer[segment].m_endDirection.x, 0, segmentBuffer[segment].m_endDirection.z);

                                m_nodeSegmentsSave[i].m_startRotation = Quaternion.FromToRotation(segVector, startDirection.normalized);
                                m_nodeSegmentsSave[i].m_endRotation = Quaternion.FromToRotation(-segVector, endDirection.normalized);
                            }
                        }

                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;

                        ushort segment = id.NetSegment;

                        m_segmentSave.m_startDirection = segmentBuffer[segment].m_startDirection;
                        m_segmentSave.m_endDirection = segmentBuffer[segment].m_endDirection;

                        m_startPosition = MoveItTool.GetControlPoint(id.NetSegment);

                        m_segmentsHashSet.Add(segment);
                        break;
                    }
            }

            if (!id.IsEmpty)
            {
                m_terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(position);
            }
        }

        public void Transform(ref Matrix4x4 matrix4x, Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(m_startPosition - center);
            newPosition.y = m_startPosition.y + deltaPosition.y;

            if(followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

            Move(newPosition, deltaAngle);

            if (subInstances != null)
            {
                foreach (Moveable subInstance in subInstances)
                {
                    Vector3 subPosition = subInstance.m_startPosition - center;
                    subPosition = matrix4x.MultiplyPoint(subPosition);
                    subPosition.y = subInstance.m_startPosition.y - m_startPosition.y + newPosition.y;

                    subInstance.Move(subPosition, deltaAngle);
                }
            }
        }

        public InstanceID Clone(ref Matrix4x4 matrix4x, Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(m_startPosition - center);
            newPosition.y = m_startPosition.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

            return Clone(newPosition, deltaAngle, clonedNodes);
        }


        public void Restore()
        {
            if (id.Type == InstanceType.NetNode)
            {
                NetManager netManager = NetManager.instance;
                NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
                ushort node = id.NetNode;

                netManager.MoveNode(node, m_startPosition);

                for (int i = 0; i < 8; i++)
                {
                    ushort segment = nodeBuffer[node].GetSegment(i);
                    if (segment != 0)
                    {
                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        segmentBuffer[segment].m_startDirection = m_nodeSegmentsSave[i].m_startDirection;
                        segmentBuffer[segment].m_endDirection = m_nodeSegmentsSave[i].m_endDirection;

                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                        netManager.UpdateNode(startNode);
                        netManager.UpdateNode(endNode);
                    }
                }
            }
            else if (id.Type == InstanceType.NetSegment)
            {
                NetManager netManager = NetManager.instance;
                NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;

                ushort segment = id.NetSegment;

                ushort startNode = segmentBuffer[segment].m_startNode;
                ushort endNode = segmentBuffer[segment].m_endNode;

                segmentBuffer[segment].m_startDirection = m_segmentSave.m_startDirection;
                segmentBuffer[segment].m_endDirection = m_segmentSave.m_endDirection;

                UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                netManager.UpdateNode(startNode);
                netManager.UpdateNode(endNode);
            }
            else
            {
                Move(m_startPosition, 0);
            }

            if (subInstances != null)
            {
                foreach (Moveable subInstance in subInstances)
                {
                    subInstance.Restore();
                }
            }
        }

        public void Delete()
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        BuildingManager.instance.ReleaseBuilding(id.Building);
                        break;
                    }
                case InstanceType.Prop:
                    {
                        PropManager.instance.ReleaseProp(id.Prop);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        TreeManager.instance.ReleaseTree(id.Tree);
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        NetManager.instance.ReleaseNode(id.NetNode);
                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        NetManager.instance.ReleaseSegment(id.NetSegment, true);
                        break;
                    }
            }
        }

        public void CalculateNewPosition(Vector3 deltaPosition, ushort deltaAngle, Vector3 center)
        {
            float fAngle = deltaAngle * 9.58738E-05f;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

            newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;
            newPosition.y = m_startPosition.y + deltaPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;

            newAngle = m_startAngle + fAngle;
        }

        public Bounds GetBounds(bool ignoreSegments = true)
        {
            Bounds bounds = GetBounds(id, ignoreSegments);

            if (subInstances != null)
            {
                foreach (Moveable subInstance in subInstances)
                {
                    bounds.Encapsulate(subInstance.GetBounds(ignoreSegments));
                }
            }

            return bounds;
        }

        public void RenderClone(RenderManager.CameraInfo cameraInfo, Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain, Color toolColor)
        {
            float fAngle = deltaAngle * 9.58738E-05f;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

            newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;
            newPosition.y = m_startPosition.y + deltaPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;

            newAngle = m_startAngle + fAngle;

            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        BuildingManager buildingManager = BuildingManager.instance;
                        Building[] buildingBuffer = buildingManager.m_buildings.m_buffer;
                        ushort building = id.Building;

                        BuildingInfo buildingInfo = buildingManager.m_buildings.m_buffer[building].Info;
                        int length = buildingManager.m_buildings.m_buffer[building].Length;
                        float angle = buildingManager.m_buildings.m_buffer[building].m_angle;

                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, newPosition, newAngle, toolColor, false);
                        break;
                    }
                case InstanceType.Prop:
                    {
                        PropInstance[] buffer = PropManager.instance.m_props.m_buffer;
                        ushort prop = id.Prop;

                        PropInfo info = buffer[prop].Info;
                        Randomizer randomizer = new Randomizer(prop);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

                        PropTool.RenderOverlay(cameraInfo, info, newPosition, scale, newAngle, toolColor);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        TreeInstance[] buffer = TreeManager.instance.m_trees.m_buffer;
                        uint tree = id.Tree;

                        TreeInfo info = buffer[tree].Info;
                        Randomizer randomizer = new Randomizer(tree);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

                        TreeTool.RenderOverlay(cameraInfo, info, newPosition, scale, toolColor);
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
                        bezier.a = matrix4x.MultiplyPoint(nodeBuffer[startNode].m_position - center) + deltaPosition;
                        bezier.d = matrix4x.MultiplyPoint(nodeBuffer[endNode].m_position - center) + deltaPosition;

                        Vector3 startDirection = newPosition - bezier.a;
                        Vector3 endDirection = newPosition - bezier.d;

                        startDirection.y = 0;
                        endDirection.y = 0;

                        startDirection.Normalize();
                        endDirection.Normalize();

                        NetSegment.CalculateMiddlePoints(
                            bezier.a, startDirection,
                            bezier.d, endDirection,
                            smoothStart, smoothEnd, out bezier.b, out bezier.c);

                        float minY = Mathf.Min(bezier.a.y, bezier.d.y);
                        float maxY = Mathf.Max(bezier.a.y, bezier.d.y);

                        RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, toolColor, bezier, netInfo.m_halfWidth * 4f / 3f, 100000f, -100000f, minY, maxY, true, true);

                        break;
                    }
            }
        }

        private Bounds GetBounds(InstanceID instance, bool ignoreSegments = true)
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
                        NetManager netManager = NetManager.instance;
                        NetNode[] buffer = netManager.m_nodes.m_buffer;
                        ushort node = id.NetNode;

                        Bounds bounds = buffer[node].m_bounds;

                        if (!ignoreSegments)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = buffer[node].GetSegment(i);
                                if (segment != 0)
                                {
                                    ushort startNode = netManager.m_segments.m_buffer[segment].m_startNode;
                                    ushort endNode = netManager.m_segments.m_buffer[segment].m_endNode;

                                    if (node != startNode)
                                    {
                                        bounds.Encapsulate(buffer[startNode].m_bounds);
                                    }
                                    else
                                    {
                                        bounds.Encapsulate(buffer[endNode].m_bounds);
                                    }
                                }
                            }
                        }

                        return bounds;
                    }
                case InstanceType.NetSegment:
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        ushort segment = id.NetSegment;

                        return segmentBuffer[segment].m_bounds;
                    }
            }

            return default(Bounds);
        }

        private void Move(Vector3 location, ushort deltaAngle)
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        BuildingManager buildingManager = BuildingManager.instance;
                        ushort building = id.Building;

                        RelocateBuilding(building, ref buildingManager.m_buildings.m_buffer[building], location, m_startAngle + deltaAngle * 9.58738E-05f);
                        break;
                    }
                case InstanceType.Prop:
                    {
                        ushort prop = id.Prop;
                        PropManager.instance.m_props.m_buffer[prop].m_angle = (ushort)((ushort)m_startAngle + deltaAngle);
                        PropManager.instance.MoveProp(prop, location);
                        PropManager.instance.UpdatePropRenderer(prop, true);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        uint tree = id.Tree;
                        TreeManager.instance.MoveTree(tree, location);
                        TreeManager.instance.UpdateTreeRenderer(tree, true);
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
                        ushort node = id.NetNode;

                        netManager.MoveNode(node, location);

                        for (int i = 0; i < 8; i++)
                        {
                            ushort segment = nodeBuffer[node].GetSegment(i);
                            if (segment != 0 && !MoveItTool.instance.IsSegmentSelected(segment))
                            {
                                ushort startNode = segmentBuffer[segment].m_startNode;
                                ushort endNode = segmentBuffer[segment].m_endNode;

                                Vector3 segVector = nodeBuffer[endNode].m_position - nodeBuffer[startNode].m_position;
                                segVector.Normalize();

                                segmentBuffer[segment].m_startDirection = m_nodeSegmentsSave[i].m_startRotation * segVector;
                                segmentBuffer[segment].m_endDirection = m_nodeSegmentsSave[i].m_endRotation * -segVector;

                                netManager.UpdateSegmentRenderer(segment, true);
                                UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                                if (node != startNode)
                                {
                                    netManager.UpdateNode(startNode);
                                }
                                else
                                {
                                    netManager.UpdateNode(endNode);
                                }
                            }
                        }
                        netManager.UpdateNode(node);

                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
                        ushort segment = id.NetSegment;

                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        segmentBuffer[segment].m_startDirection = location - nodeBuffer[segmentBuffer[segment].m_startNode].m_position;
                        segmentBuffer[segment].m_endDirection = location - nodeBuffer[segmentBuffer[segment].m_endNode].m_position;

                        segmentBuffer[segment].m_startDirection.y = 0;
                        segmentBuffer[segment].m_endDirection.y = 0;

                        segmentBuffer[segment].m_startDirection.Normalize();
                        segmentBuffer[segment].m_endDirection.Normalize();

                        netManager.UpdateSegmentRenderer(segment, true);
                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                        netManager.UpdateNode(startNode);
                        netManager.UpdateNode(endNode);
                        break;
                    }
            }
        }

        private InstanceID Clone(Vector3 location, ushort deltaAngle, Dictionary<ushort, ushort> clonedNodes)
        {
            InstanceID cloneID = new InstanceID();

            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
                        ushort building = id.Building;

                        //if ((buildingBuffer[building].m_flags & Building.Flags.Untouchable) == Building.Flags.None)
                        if (buildingBuffer[building].FindParentNode(building) == 0)
                        {
                            ushort clone;
                            if (BuildingManager.instance.CreateBuilding(out clone, ref SimulationManager.instance.m_randomizer,
                                buildingBuffer[building].Info, location, buildingBuffer[building].m_angle + deltaAngle * 9.58738E-05f,
                                buildingBuffer[building].Length, SimulationManager.instance.m_currentBuildIndex))
                            {
                                SimulationManager.instance.m_currentBuildIndex++;
                                cloneID.Building = clone;
                                if ((buildingBuffer[building].m_flags & Building.Flags.Completed) != Building.Flags.None)
                                {
                                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.Completed;
                                }
                            }
                        }
                        break;
                    }
                case InstanceType.Prop:
                    {
                        PropInstance[] buffer = PropManager.instance.m_props.m_buffer;
                        ushort prop = id.Prop;

                        ushort clone;
                        if (PropManager.instance.CreateProp(out clone, ref SimulationManager.instance.m_randomizer,
                            buffer[prop].Info, location, (buffer[prop].m_angle + deltaAngle) * 9.58738E-05f, buffer[prop].Single))
                        {
                            cloneID.Prop = clone;
                        }
                        break;
                    }
                case InstanceType.Tree:
                    {
                        TreeInstance[] buffer = TreeManager.instance.m_trees.m_buffer;
                        uint tree = id.Tree;

                        uint clone;
                        if (TreeManager.instance.CreateTree(out clone, ref SimulationManager.instance.m_randomizer,
                            buffer[tree].Info, location, buffer[tree].Single))
                        {
                            cloneID.Tree = clone;
                        }
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;
                        ushort node = id.NetNode;

                        ushort clone;
                        if (NetManager.instance.CreateNode(out clone, ref SimulationManager.instance.m_randomizer, nodeBuffer[node].Info,
                            location, SimulationManager.instance.m_currentBuildIndex))
                        {
                            SimulationManager.instance.m_currentBuildIndex++;
                            cloneID.NetNode = clone;
                        }
                        
                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        NetManager netManager = NetManager.instance;
                        NetSegment[] segmentBuffer = netManager.m_segments.m_buffer;
                        NetNode[] nodeBuffer = netManager.m_nodes.m_buffer;
                        ushort segment = id.NetSegment;

                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        if(clonedNodes.ContainsKey(startNode))
                        {
                            startNode = clonedNodes[startNode];
                        }
                        else
                        {
                            break;
                        }

                        if (clonedNodes.ContainsKey(endNode))
                        {
                            endNode = clonedNodes[endNode];
                        }
                        else
                        {
                            break;
                        }

                        Vector3 startDirection = location - nodeBuffer[startNode].m_position;
                        Vector3 endDirection = location - nodeBuffer[endNode].m_position;

                        startDirection.y = 0;
                        endDirection.y = 0;

                        startDirection.Normalize();
                        endDirection.Normalize();

                        ushort clone;
                        if(netManager.CreateSegment(out clone, ref SimulationManager.instance.m_randomizer, segmentBuffer[segment].Info,
                            startNode, endNode, startDirection, endDirection,
                            SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex,
                            (segmentBuffer[segment].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.Invert))
                        {
                            SimulationManager.instance.m_currentBuildIndex++;
                            cloneID.NetSegment = clone;
                        }

                        break;
                    }
            }
            return cloneID;
        }

        private void RelocateBuilding(ushort building, ref Building data, Vector3 position, float angle)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            RemoveFromGrid(building, ref data);
            if (info.m_hasParkingSpaces)
            {
                buildingManager.UpdateParkingSpaces(building, ref data);
            }
            data.m_position = position;
            data.m_angle = angle;

            AddToGrid(building, ref data);
            data.CalculateBuilding(building);
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

        private void UpdateSegmentBlocks(ushort segment, ref NetSegment data)
        {
            if (data.m_flags != NetSegment.Flags.None)
            {
                ReleaseSegmentBlock(segment, ref data.m_blockStartLeft);
                ReleaseSegmentBlock(segment, ref data.m_blockStartRight);
                ReleaseSegmentBlock(segment, ref data.m_blockEndLeft);
                ReleaseSegmentBlock(segment, ref data.m_blockEndRight);
            }

            data.Info.m_netAI.CreateSegment(segment, ref data);
        }

        private void ReleaseSegmentBlock(ushort segment, ref ushort segmentBlock)
        {
            if (segmentBlock != 0)
            {
                ZoneManager.instance.ReleaseBlock(segmentBlock);
                segmentBlock = 0;
            }
        }
    }
}
