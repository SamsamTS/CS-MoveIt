using UnityEngine;

using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.Math;

namespace MoveIt
{
    internal class Moveable
    {
        public static NetManager netManager = NetManager.instance;
        public static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
        public static NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
        public static NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

        public InstanceID id;
        public HashSet<Moveable> subInstances;

        public Vector3 newPosition;
        public float newAngle;

        private Vector3 m_startPosition;
        private float m_terrainHeight;
        private float m_startAngle;
        private int m_flags;

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

        public bool autoCurve = false;
        public NetSegment segmentCurve;

        private static MethodInfo RenderSegment = typeof(NetTool).GetMethod("RenderSegment", BindingFlags.NonPublic | BindingFlags.Static);

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
                            return GetControlPoint(id.NetSegment);
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
                        m_startPosition = buildingBuffer[id.Building].m_position;
                        m_startAngle = buildingBuffer[id.Building].m_angle;
                        m_flags = (int)buildingBuffer[id.Building].m_flags;

                        subInstances = new HashSet<Moveable>();

                        ushort node = buildingBuffer[id.Building].m_netNode;
                        int count = 0;
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

                            if (++count > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }

                        ushort building = buildingBuffer[id.Building].m_subBuilding;
                        count = 0;
                        while (building != 0)
                        {
                            Moveable subBuilding = new Moveable(InstanceID.Empty);
                            subBuilding.id.Building = building;
                            subBuilding.m_startPosition = buildingBuffer[building].m_position;
                            subBuilding.m_startAngle = buildingBuffer[building].m_angle;
                            subBuilding.m_flags = (int)buildingBuffer[building].m_flags;
                            subInstances.Add(subBuilding);

                            node = buildingBuffer[building].m_netNode;
                            int count2 = 0;
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

                                if (++count2 > 32768)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }

                            building = buildingBuffer[building].m_subBuilding;

                            if (++count > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
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

                        if (nodeBuffer[node].m_building != 0)
                        {
                            ushort building = nodeBuffer[node].m_building;
                            subInstances = new HashSet<Moveable>();
                            Moveable subBuilding = new Moveable(InstanceID.Empty);
                            subBuilding.id.Building = building;
                            subBuilding.m_startPosition = buildingBuffer[building].m_position;
                            subBuilding.m_startAngle = buildingBuffer[building].m_angle;
                            subInstances.Add(subBuilding);
                        }

                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        ushort segment = id.NetSegment;

                        m_segmentSave.m_startDirection = segmentBuffer[segment].m_startDirection;
                        m_segmentSave.m_endDirection = segmentBuffer[segment].m_endDirection;

                        m_startPosition = GetControlPoint(id.NetSegment);

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

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

            Move(newPosition, deltaAngle, deltaPosition.y != 0f);

            if (subInstances != null)
            {
                foreach (Moveable subInstance in subInstances)
                {
                    Vector3 subPosition = subInstance.m_startPosition - center;
                    subPosition = matrix4x.MultiplyPoint(subPosition);
                    subPosition.y = subInstance.m_startPosition.y - m_startPosition.y + newPosition.y;

                    subInstance.Move(subPosition, deltaAngle, false);
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
                ushort segment = id.NetSegment;

                ushort startNode = segmentBuffer[segment].m_startNode;
                ushort endNode = segmentBuffer[segment].m_endNode;

                segmentBuffer[segment].m_startDirection = m_segmentSave.m_startDirection;
                segmentBuffer[segment].m_endDirection = m_segmentSave.m_endDirection;

                UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                netManager.UpdateNode(startNode);
                netManager.UpdateNode(endNode);
            }
            else if(id.Type == InstanceType.Building)
            {

                buildingBuffer[id.Building].m_flags = (Building.Flags)m_flags;
                Move(m_startPosition, 0, false);
            }
            else
            {
                Move(m_startPosition, 0, false);
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

        public void CalculateNewPosition(Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain)
        {
            float fAngle = deltaAngle * 9.58738E-05f;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

            newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;
            newPosition.y = m_startPosition.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

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

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        ushort building = id.Building;
                        BuildingInfo buildingInfo = buildingBuffer[building].Info;

                        if (WillBuildingDespawn(building))
                        {
                            toolColor = despawnColor;
                        }

                        float alpha = 1f;
                        BuildingTool.CheckOverlayAlpha(buildingInfo, ref alpha);
                        ushort node = buildingBuffer[building].m_netNode;
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
                        ushort subBuilding = buildingBuffer[building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingTool.CheckOverlayAlpha(buildingBuffer[subBuilding].Info, ref alpha);
                            subBuilding = buildingBuffer[subBuilding].m_subBuilding;
                            if (++count > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        toolColor.a *= alpha;
                        int length = buildingBuffer[building].Length;
                        Vector3 position = buildingBuffer[building].m_position;
                        float angle = buildingBuffer[building].m_angle;
                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, position, angle, toolColor, false);

                        node = buildingBuffer[building].m_netNode;
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
                        subBuilding = buildingBuffer[building].m_subBuilding;
                        count = 0;
                        while (subBuilding != 0)
                        {
                            BuildingInfo subBuildingInfo = buildingBuffer[subBuilding].Info;
                            int subLength = buildingBuffer[subBuilding].Length;
                            Vector3 subPosition = buildingBuffer[subBuilding].m_position;
                            float subAngle = buildingBuffer[subBuilding].m_angle;
                            BuildingTool.RenderOverlay(cameraInfo, subBuildingInfo, subLength, subPosition, subAngle, toolColor, false);
                            subBuilding = buildingBuffer[subBuilding].m_subBuilding;

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
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, toolColor, position, netInfo.m_halfWidth * 2f, -1f, 1280f, false, true);
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

                        RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, toolColor, bezier, netInfo.m_halfWidth * 4f / 3f, 100000f, -100000f, -1f, 1280f, false, true);

                        Segment3 segment1, segment2;

                        segment1.a = nodeBuffer[startNode].m_position;
                        segment2.a = nodeBuffer[endNode].m_position;

                        segment1.b = GetControlPoint(segment);
                        segment2.b = segment1.b;

                        toolColor.a = toolColor.a / 2;

                        RenderManager.instance.OverlayEffect.DrawSegment(cameraInfo, toolColor, segment1, segment2, 0, 10f, -1f, 1280f, false, true);
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, toolColor, segment1.b, netInfo.m_halfWidth / 2f, -1f, 1280f, false, true);

                        break;
                    }
            }
        }

        public void RenderCloneOverlay(RenderManager.CameraInfo cameraInfo, Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain, Color toolColor)
        {
            float fAngle = deltaAngle * 9.58738E-05f;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

            newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;
            newPosition.y = m_startPosition.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        ushort building = id.Building;

                        BuildingInfo buildingInfo = buildingBuffer[building].Info;
                        int length = buildingBuffer[building].Length;
                        Color color = buildingInfo.m_buildingAI.GetColor(0, ref buildingBuffer[building], InfoManager.instance.CurrentMode);

                        newAngle = m_startAngle + fAngle;
                        buildingInfo.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, newPosition, newAngle, default(Segment3));
                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, newPosition, newAngle, toolColor, false);
                        if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
                        {
                            matrix4x = default(Matrix4x4);
                            matrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * 57.29578f, Vector3.down), Vector3.one);
                            for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                            {
                                BuildingInfo buildingInfo2 = buildingInfo.m_subBuildings[i].m_buildingInfo;
                                Vector3 position = matrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                                float angle = buildingInfo.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;
                                buildingInfo2.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, position, angle, default(Segment3));
                                BuildingTool.RenderOverlay(cameraInfo, buildingInfo2, 0, position, angle, toolColor, true);
                            }
                        }
                        break;
                    }
                case InstanceType.Prop:
                    {
                        ushort prop = id.Prop;

                        PropInfo info = PropManager.instance.m_props.m_buffer[prop].Info;
                        Randomizer randomizer = new Randomizer(prop);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

                        newAngle = (m_startAngle + deltaAngle) * 9.58738E-05f;
                        PropTool.RenderOverlay(cameraInfo, info, newPosition, scale, newAngle, toolColor);
                        break;
                    }
                case InstanceType.Tree:
                    {
                        uint tree = id.Tree;

                        TreeInfo info = TreeManager.instance.m_trees.m_buffer[tree].Info;
                        Randomizer randomizer = new Randomizer(tree);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                        float brightness = info.m_minBrightness + (float)randomizer.Int32(10000u) * (info.m_maxBrightness - info.m_minBrightness) * 0.0001f;

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

                        if (followTerrain)
                        {
                            bezier.a.y = bezier.a.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(bezier.a) - TerrainManager.instance.SampleOriginalRawHeightSmooth(nodeBuffer[startNode].m_position);
                            bezier.d.y = bezier.d.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(bezier.d) - TerrainManager.instance.SampleOriginalRawHeightSmooth(nodeBuffer[endNode].m_position);
                        }
                        else
                        {
                            bezier.a.y = nodeBuffer[startNode].m_position.y + deltaPosition.y;
                            bezier.d.y = nodeBuffer[endNode].m_position.y + deltaPosition.y;
                        }

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

                        RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, toolColor, bezier, netInfo.m_halfWidth * 4f / 3f, 100000f, -100000f, -1f, 1280f, false, true);
                        break;
                    }
            }
        }

        public void RenderCloneGeometry(RenderManager.CameraInfo cameraInfo, Vector3 deltaPosition, ushort deltaAngle, Vector3 center, bool followTerrain, Color toolColor)
        {
            float fAngle = deltaAngle * 9.58738E-05f;

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center, Quaternion.AngleAxis(fAngle * 57.29578f, Vector3.down), Vector3.one);

            newPosition = matrix4x.MultiplyPoint(m_startPosition - center) + deltaPosition;
            newPosition.y = m_startPosition.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - m_terrainHeight;
            }

            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        ushort building = id.Building;

                        BuildingInfo buildingInfo = buildingBuffer[building].Info;
                        int length = buildingBuffer[building].Length;
                        float angle = buildingBuffer[building].m_angle;
                        Color color = buildingInfo.m_buildingAI.GetColor(0, ref buildingBuffer[building], InfoManager.instance.CurrentMode);

                        newAngle = m_startAngle + fAngle;

                        buildingInfo.m_buildingAI.RenderBuildGeometry(cameraInfo, newPosition, newAngle, 0);
                        BuildingTool.RenderGeometry(cameraInfo, buildingInfo, length, newPosition, newAngle, false, color);
                        if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
                        {
                            matrix4x = default(Matrix4x4);
                            matrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * 57.29578f, Vector3.down), Vector3.one);
                            for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                            {
                                BuildingInfo buildingInfo2 = buildingInfo.m_subBuildings[i].m_buildingInfo;
                                Vector3 position = matrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                                angle = buildingInfo.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;
                                buildingInfo2.m_buildingAI.RenderBuildGeometry(cameraInfo, position, angle, 0);
                                BuildingTool.RenderGeometry(cameraInfo, buildingInfo2, 0, position, angle, true, color);
                            }
                        }
                        break;
                    }
                case InstanceType.Prop:
                    {
                        ushort prop = id.Prop;

                        PropInfo info = PropManager.instance.m_props.m_buffer[prop].Info;
                        Randomizer randomizer = new Randomizer(prop);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

                        newAngle = (m_startAngle + deltaAngle) * 9.58738E-05f;

                        if (info.m_requireHeightMap)
                        {
                            Texture heightMap;
                            Vector4 heightMapping;
                            Vector4 surfaceMapping;
                            TerrainManager.instance.GetHeightMapping(newPosition, out heightMap, out heightMapping, out surfaceMapping);
                            PropInstance.RenderInstance(cameraInfo, info, id, newPosition, scale, newAngle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
                        }
                        else
                        {
                            PropInstance.RenderInstance(cameraInfo, info, id, newPosition, scale, newAngle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true);
                        }
                        break;
                    }
                case InstanceType.Tree:
                    {
                        uint tree = id.Tree;

                        TreeInfo info = TreeManager.instance.m_trees.m_buffer[tree].Info;
                        Randomizer randomizer = new Randomizer(tree);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                        float brightness = info.m_minBrightness + (float)randomizer.Int32(10000u) * (info.m_maxBrightness - info.m_minBrightness) * 0.0001f;

                        TreeInstance.RenderInstance(cameraInfo, info, newPosition, scale, brightness);
                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        ushort segment = id.NetSegment;

                        NetInfo netInfo = segmentBuffer[segment].Info;

                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        bool smoothStart = ((nodeBuffer[startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);
                        bool smoothEnd = ((nodeBuffer[endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None);

                        Bezier3 bezier;
                        bezier.a = matrix4x.MultiplyPoint(nodeBuffer[startNode].m_position - center) + deltaPosition;
                        bezier.d = matrix4x.MultiplyPoint(nodeBuffer[endNode].m_position - center) + deltaPosition;

                        if (followTerrain)
                        {
                            bezier.a.y = bezier.a.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(bezier.a) - TerrainManager.instance.SampleOriginalRawHeightSmooth(nodeBuffer[startNode].m_position);
                            bezier.d.y = bezier.d.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(bezier.d) - TerrainManager.instance.SampleOriginalRawHeightSmooth(nodeBuffer[endNode].m_position);
                        }
                        else
                        {
                            bezier.a.y = nodeBuffer[startNode].m_position.y + deltaPosition.y;
                            bezier.d.y = nodeBuffer[endNode].m_position.y + deltaPosition.y;
                        }

                        Vector3 startDirection = newPosition - bezier.a;
                        Vector3 endDirection = newPosition - bezier.d;

                        startDirection.y = 0;
                        endDirection.y = 0;

                        startDirection.Normalize();
                        endDirection.Normalize();

                        RenderSegment.Invoke(null, new object[] { netInfo, bezier.a, bezier.d, startDirection, -endDirection, smoothStart, smoothEnd });
                        break;
                    }
            }
        }

        private bool WillBuildingDespawn(ushort building)
        {
            BuildingInfo info = buildingBuffer[building].Info;

            ItemClass.Zone zone1 = info.m_class.GetZone();
            ItemClass.Zone zone2 = info.m_class.GetSecondaryZone();

            if (info.m_placementStyle != ItemClass.Placement.Automatic || zone1 == ItemClass.Zone.None)
            {
                return false;
            }

            info.m_buildingAI.CheckRoadAccess(building, ref buildingBuffer[building]);
            if ((buildingBuffer[building].m_problems & Notification.Problem.RoadNotConnected) == Notification.Problem.RoadNotConnected ||
                !buildingBuffer[building].CheckZoning(zone1, zone2, true))
            {
                return true;
            }

            return false;
        }

        private Vector3 GetControlPoint(ushort segment)
        {
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

        private Bounds GetBounds(InstanceID instance, bool ignoreSegments = true)
        {
            switch (instance.Type)
            {
                case InstanceType.Building:
                    {
                        ushort id = instance.Building;
                        BuildingInfo info = buildingBuffer[id].Info;

                        float radius = Mathf.Max(info.m_cellWidth * 4f, info.m_cellLength * 4f);
                        Bounds bounds = new Bounds(buildingBuffer[id].m_position, new Vector3(radius, 0, radius));

                        return bounds;
                    }
                case InstanceType.Prop:
                    {
                        ushort id = instance.Prop;
                        PropInfo info = PropManager.instance.m_props.m_buffer[id].Info;

                        Randomizer randomizer = new Randomizer(id);
                        float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                        float radius = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale;

                        return new Bounds(PropManager.instance.m_props.m_buffer[id].Position, new Vector3(radius, 0, radius));
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
                        ushort node = id.NetNode;

                        Bounds bounds = nodeBuffer[node].m_bounds;

                        if (!ignoreSegments)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = nodeBuffer[node].GetSegment(i);
                                if (segment != 0)
                                {
                                    ushort startNode = segmentBuffer[segment].m_startNode;
                                    ushort endNode = segmentBuffer[segment].m_endNode;

                                    if (node != startNode)
                                    {
                                        bounds.Encapsulate(nodeBuffer[startNode].m_bounds);
                                    }
                                    else
                                    {
                                        bounds.Encapsulate(nodeBuffer[endNode].m_bounds);
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

        private void Move(Vector3 location, ushort deltaAngle, bool elevation)
        {
            switch (id.Type)
            {
                case InstanceType.Building:
                    {
                        BuildingManager buildingManager = BuildingManager.instance;
                        ushort building = id.Building;

                        if(elevation)
                        {
                            buildingBuffer[building].m_flags = buildingBuffer[building].m_flags | Building.Flags.FixedHeight;
                        }
                        RelocateBuilding(building, ref buildingBuffer[building], location, m_startAngle + deltaAngle * 9.58738E-05f);
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

                                CalculateSegmentDirections(ref segmentBuffer[segment], segment);

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

                        // Auto curve
                        if (autoCurve && segmentCurve.m_startNode != 0 && segmentCurve.m_endNode != 0)
                        {
                            Vector3 p, tangent;
                            segmentCurve.GetClosestPositionAndDirection(location, out p, out tangent);

                            for (int i = 0; i < 8; i++)
                            {
                                ushort segment = nodeBuffer[segmentCurve.m_startNode].GetSegment(i);

                                if (segment != 0)
                                {
                                    ushort startNode = segmentBuffer[segment].m_startNode;
                                    ushort endNode = segmentBuffer[segment].m_endNode;

                                    if (startNode == node)
                                    {
                                        segmentBuffer[segment].m_startDirection = -tangent;
                                        segmentBuffer[segment].m_endDirection = segmentCurve.m_startDirection;

                                        CalculateSegmentDirections(ref segmentBuffer[segment], segment);
                                        netManager.UpdateSegmentRenderer(segment, true);
                                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                                        netManager.UpdateNode(endNode);
                                    }
                                    else if (endNode == node)
                                    {
                                        segmentBuffer[segment].m_startDirection = segmentCurve.m_startDirection;
                                        segmentBuffer[segment].m_endDirection = -tangent;

                                        CalculateSegmentDirections(ref segmentBuffer[segment], segment);
                                        netManager.UpdateSegmentRenderer(segment, true);
                                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                                        netManager.UpdateNode(startNode);
                                    }
                                }

                                segment = nodeBuffer[segmentCurve.m_endNode].GetSegment(i);

                                if (segment != 0)
                                {
                                    ushort startNode = segmentBuffer[segment].m_startNode;
                                    ushort endNode = segmentBuffer[segment].m_endNode;

                                    if (startNode == node)
                                    {
                                        segmentBuffer[segment].m_startDirection = tangent;
                                        segmentBuffer[segment].m_endDirection = segmentCurve.m_endDirection;

                                        CalculateSegmentDirections(ref segmentBuffer[segment], segment);
                                        netManager.UpdateSegmentRenderer(segment, true);
                                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                                        netManager.UpdateNode(endNode);
                                    }
                                    else if (endNode == node)
                                    {
                                        segmentBuffer[segment].m_startDirection = segmentCurve.m_endDirection;
                                        segmentBuffer[segment].m_endDirection = tangent;

                                        CalculateSegmentDirections(ref segmentBuffer[segment], segment);
                                        netManager.UpdateSegmentRenderer(segment, true);
                                        UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

                                        netManager.UpdateNode(startNode);
                                    }
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

                        CalculateSegmentDirections(ref segmentBuffer[segment], segment);

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
                        ushort building = id.Building;
                        BuildingInfo info = buildingBuffer[building].Info;

                        if (buildingBuffer[building].FindParentNode(building) == 0)
                        {
                            newAngle = buildingBuffer[building].m_angle + deltaAngle * 9.58738E-05f;
                            ushort clone;
                            if (BuildingManager.instance.CreateBuilding(out clone, ref SimulationManager.instance.m_randomizer,
                                info, location, newAngle,
                                buildingBuffer[building].Length, SimulationManager.instance.m_currentBuildIndex))
                            {
                                SimulationManager.instance.m_currentBuildIndex++;
                                cloneID.Building = clone;
                                if ((buildingBuffer[building].m_flags & Building.Flags.Completed) != Building.Flags.None)
                                {
                                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.Completed;
                                }
                                if ((buildingBuffer[building].m_flags & Building.Flags.FixedHeight) != Building.Flags.None)
                                {
                                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.FixedHeight;
                                }


                                if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
                                {
                                    Matrix4x4 matrix4x = default(Matrix4x4);
                                    matrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * 57.29578f, Vector3.down), Vector3.one);
                                    for (int i = 0; i < info.m_subBuildings.Length; i++)
                                    {
                                        BuildingInfo subInfo = info.m_subBuildings[i].m_buildingInfo;
                                        Vector3 subPosition = matrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                                        float subAngle = info.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;

                                        ushort subClone;
                                        if (BuildingManager.instance.CreateBuilding(out subClone, ref SimulationManager.instance.m_randomizer,
                                            subInfo, subPosition, subAngle, 0, SimulationManager.instance.m_currentBuildIndex))
                                        {
                                            SimulationManager.instance.m_currentBuildIndex++;
                                            if (info.m_fixedHeight)
                                            {
                                                buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.FixedHeight;
                                            }
                                        }
                                        if (clone != 0 && subClone != 0)
                                        {
                                            buildingBuffer[clone].m_subBuilding = subClone;
                                            buildingBuffer[subClone].m_parentBuilding = clone;
                                            buildingBuffer[subClone].m_flags = buildingBuffer[subClone].m_flags | Building.Flags.Untouchable;
                                            clone = subClone;
                                        }
                                    }
                                }

                                /*ItemClass.Zone zone = info.m_class.GetZone();

                                if (zone != ItemClass.Zone.None)
                                {
                                    Bounds bounds = GetBounds(cloneID);

                                    Vector2 minPos = VectorUtils.XZ(bounds.min);
                                    Vector2 maxPos = VectorUtils.XZ(bounds.max);

                                    ZoneManager instance = Singleton<ZoneManager>.instance;
                                    int minX = Mathf.Max((int)((minPos.x - 46f) / 64f + 75f), 0);
                                    int minY = Mathf.Max((int)((minPos.y - 46f) / 64f + 75f), 0);
                                    int maxX = Mathf.Min((int)((maxPos.x + 46f) / 64f + 75f), 149);
                                    int maxY = Mathf.Min((int)((maxPos.y + 46f) / 64f + 75f), 149);

                                    for (int i = minY; i <= maxY; i++)
                                    {
                                        for (int j = minX; j <= maxX; j++)
                                        {
                                            ushort gridBlock = instance.m_zoneGrid[i * 150 + j];
                                            while (gridBlock != 0)
                                            {
                                                ZoneBlock[] blockBuffer = instance.m_blocks.m_buffer;
                                                Vector3 position = blockBuffer[gridBlock].m_position;
                                                //float num11 = Mathf.Max(Mathf.Max(minPos.x - 46f - position.x, minPos.y - 46f - position.z), Mathf.Max(position.x - maxPos.x - 46f, position.z - maxPos.y - 46f));
                                                //if (num11 < 0f)
                                                {
                                                    int rowCount = blockBuffer[gridBlock].RowCount;
                                                    
                                                    Vector2 a = new Vector2(Mathf.Cos(blockBuffer[gridBlock].m_angle), Mathf.Sin(blockBuffer[gridBlock].m_angle)) * 8f;
                                                    Vector2 a2 = new Vector2(a.y, -a.x);
                                                    Vector2 a3 = VectorUtils.XZ(blockBuffer[gridBlock].m_position);

                                                    bool update = false;

                                                    for (int k = 0; k < rowCount; k++)
                                                    {
		                                                Vector2 b = ((float)k - 3.5f) * a2;
                                                        for (int l = 0; l < 4; l++)
                                                        {
                                                            Vector2 b2 = ((float)l - 3.5f) * a;
                                                            Vector2 p = a3 + b2 + b;
                                                            if (bounds.Contains(p))
                                                            {
                                                                instance.m_blocks.m_buffer[gridBlock].SetZone(l, k, zone);
                                                                update = true;
                                                            }
                                                        }
                                                    }

                                                    if(update)
                                                    {
                                                        blockBuffer[gridBlock].RefreshZoning(gridBlock);
                                                    }
                                                }
                                                gridBlock = instance.m_blocks.m_buffer[(int)gridBlock].m_nextGridBlock;
                                            }
                                        }
                                    }
                                }*/
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
                        ushort node = id.NetNode;

                        ushort clone;
                        if (NetManager.instance.CreateNode(out clone, ref SimulationManager.instance.m_randomizer, nodeBuffer[node].Info,
                            location, SimulationManager.instance.m_currentBuildIndex))
                        {
                            SimulationManager.instance.m_currentBuildIndex++;
                            cloneID.NetNode = clone;

                            nodeBuffer[clone].m_flags = nodeBuffer[node].m_flags;

                            BuildingInfo newBuilding;
                            float heightOffset;
                            nodeBuffer[clone].Info.m_netAI.GetNodeBuilding(clone, ref nodeBuffer[clone], out newBuilding, out heightOffset);
                            nodeBuffer[clone].UpdateBuilding(clone, newBuilding, heightOffset);
                        }

                        break;
                    }
                case InstanceType.NetSegment:
                    {
                        ushort segment = id.NetSegment;

                        ushort startNode = segmentBuffer[segment].m_startNode;
                        ushort endNode = segmentBuffer[segment].m_endNode;

                        if (clonedNodes.ContainsKey(startNode))
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
                        if (netManager.CreateSegment(out clone, ref SimulationManager.instance.m_randomizer, segmentBuffer[segment].Info,
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
            BuildingInfo info = data.Info;
            RemoveFromGrid(building, ref data);
            if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            {
                BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            }
            data.m_position = position;
            data.m_angle = angle;

            AddToGrid(building, ref data);
            data.CalculateBuilding(building);
            BuildingManager.instance.UpdateBuildingRenderer(building, true);
        }

        private static void AddToGrid(ushort building, ref Building data)
        {
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(BuildingManager.instance.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                buildingBuffer[(int)building].m_nextGridBuilding = BuildingManager.instance.m_buildingGrid[num3];
                BuildingManager.instance.m_buildingGrid[num3] = building;
            }
            finally
            {
                Monitor.Exit(BuildingManager.instance.m_buildingGrid);
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
                            buildingBuffer[(int)num4].m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = buildingBuffer[(int)num5].m_nextGridBuilding;
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

        public void CalculateSegmentDirections(ref NetSegment segment, ushort segmentID)
        {
            if (segment.m_flags != NetSegment.Flags.None)
            {
                segment.m_startDirection.y = 0;
                segment.m_endDirection.y = 0;

                segment.m_startDirection.Normalize();
                segment.m_endDirection.Normalize();

                segment.m_startDirection = segment.FindDirection(segmentID, segment.m_startNode);
                segment.m_endDirection = segment.FindDirection(segmentID, segment.m_endNode);
            }
        }
    }
}
