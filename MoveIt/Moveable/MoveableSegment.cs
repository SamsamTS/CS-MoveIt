using UnityEngine;

using System.Reflection;
using System.Collections.Generic;
using ColossalFramework.Math;

using System.Xml.Serialization;

namespace MoveIt
{
    public class SegmentState : InstanceState
    {
        [XmlIgnore]
        public Instance startNode;

        [XmlIgnore]
        public Instance endNode;

        public ushort startNodeId
        {
            get
            {
                return startNode.id.NetNode;
            }

            set
            {
                InstanceID instanceID = InstanceID.Empty;
                instanceID.NetNode = value;

                startNode = new MoveableNode(instanceID);
            }
        }

        public ushort endNodeId
        {
            get
            {
                return endNode.id.NetNode;
            }

            set
            {
                InstanceID instanceID = InstanceID.Empty;
                instanceID.NetNode = value;

                endNode = new MoveableNode(instanceID);
            }
        }

        public Vector3 startDirection;
        public Vector3 endDirection;

        public bool invert;

        public override void ReplaceInstance(Instance instance)
        {
            base.ReplaceInstance(instance);

            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;

            InstanceID instanceID = default(InstanceID);

            instanceID.NetNode = segmentBuffer[instance.id.NetSegment].m_startNode;
            startNode = (Instance)instanceID;

            instanceID.NetNode = segmentBuffer[instance.id.NetSegment].m_endNode;
            endNode = (Instance)instanceID;
        }
    }

    public class MoveableSegment : Instance
    {
        public override HashSet<ushort> segmentList
        {
            get
            {
                return new HashSet<ushort>{id.NetSegment};
            }
        }

        public MoveableSegment(InstanceID instanceID) : base(instanceID) { }

        public override InstanceState GetState()
        {
            ushort segment  = id.NetSegment;

            SegmentState state = new SegmentState();

            state.instance = this;
            state.info = info;

            state.position = GetControlPoint(segment);

            InstanceID instanceID = default(InstanceID);

            instanceID.NetNode = segmentBuffer[segment].m_startNode;
            state.startNode = (Instance)instanceID;

            instanceID.NetNode = segmentBuffer[segment].m_endNode;
            state.endNode = (Instance)instanceID;

            state.startDirection = segmentBuffer[segment].m_startDirection;
            state.endDirection = segmentBuffer[segment].m_endDirection;

            state.invert = ((segmentBuffer[segment].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.Invert);

            return state;
        }

        public override void SetState(InstanceState state)
        {
            SegmentState segmentState = state as SegmentState;
            if (segmentState == null) return;

            ushort segment = id.NetSegment;

            segmentBuffer[segment].m_startDirection = segmentState.startDirection;
            segmentBuffer[segment].m_endDirection = segmentState.endDirection;

            UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

            netManager.UpdateNode(segmentBuffer[segment].m_startNode);
            netManager.UpdateNode(segmentBuffer[segment].m_endNode);
        }

        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return GetControlPoint(id.NetSegment);
            }
        }

        public override float angle
        {
            get
            {
                return 0f;
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                return (segmentBuffer[id.NetSegment].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None;
            }
        }

        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);

            Move(newPosition, 0);
        }

        public override void Move(Vector3 location, float angle)
        {
            if (!isValid) return;

            ushort segment = id.NetSegment;

            segmentBuffer[segment].m_startDirection = location - nodeBuffer[segmentBuffer[segment].m_startNode].m_position;
            segmentBuffer[segment].m_endDirection = location - nodeBuffer[segmentBuffer[segment].m_endNode].m_position;

            CalculateSegmentDirections(ref segmentBuffer[segment], segment);

            netManager.UpdateSegmentRenderer(segment, true);
            UpdateSegmentBlocks(segment, ref segmentBuffer[segment]);

            netManager.UpdateNode(segmentBuffer[segment].m_startNode);
            netManager.UpdateNode(segmentBuffer[segment].m_endNode);
        }

        public override void SetHeight(float height) { }

        public override Instance Clone(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);

            Instance cloneInstance = null;

            ushort segment = id.NetSegment;

            ushort startNode = segmentBuffer[segment].m_startNode;
            ushort endNode = segmentBuffer[segment].m_endNode;

            // Nodes should exist
            startNode = clonedNodes[startNode];
            endNode = clonedNodes[endNode];

            Vector3 startDirection = newPosition - nodeBuffer[startNode].m_position;
            Vector3 endDirection = newPosition - nodeBuffer[endNode].m_position;

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

                InstanceID cloneID = default(InstanceID);
                cloneID.NetSegment = clone;
                cloneInstance = new MoveableSegment(cloneID);
            }

            return cloneInstance;
        }

        public override Instance Clone(InstanceState instanceState)
        {
            SegmentState state = instanceState as SegmentState;

            Instance cloneInstance = null;

            ushort clone;
            if (netManager.CreateSegment(out clone, ref SimulationManager.instance.m_randomizer, state.info as NetInfo,
                state.startNode.id.NetNode, state.endNode.id.NetNode, state.startDirection, state.endDirection,
                SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex,
                state.invert))
            {
                SimulationManager.instance.m_currentBuildIndex++;

                InstanceID cloneID = default(InstanceID);
                cloneID.NetSegment = clone;
                cloneInstance = new MoveableSegment(cloneID);
            }

            return cloneInstance;
        }

        public override void Delete()
        {
            if (isValid) NetManager.instance.ReleaseSegment(id.NetSegment, true);
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            return segmentBuffer[id.NetSegment].m_bounds;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;

            ushort segment = id.NetSegment;
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
        }

        public override void RenderCloneOverlay(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
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

            Vector3 startDirection = state.position - bezier.a;
            Vector3 endDirection = state.position - bezier.d;

            startDirection.y = 0;
            endDirection.y = 0;

            startDirection.Normalize();
            endDirection.Normalize();

            NetSegment.CalculateMiddlePoints(
                bezier.a, startDirection,
                bezier.d, endDirection,
                smoothStart, smoothEnd, out bezier.b, out bezier.c);

            RenderManager.instance.OverlayEffect.DrawBezier(cameraInfo, toolColor, bezier, netInfo.m_halfWidth * 4f / 3f, 100000f, -100000f, -1f, 1280f, false, true);
        }

        public override void RenderCloneGeometry(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
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

            Vector3 startDirection = state.position - bezier.a;
            Vector3 endDirection = state.position - bezier.d;

            startDirection.y = 0;
            endDirection.y = 0;

            startDirection.Normalize();
            endDirection.Normalize();
            //private static void RenderSegment(NetInfo info, NetSegment.Flags flags, Vector3 startPosition, Vector3 endPosition, Vector3 startDirection, Vector3 endDirection, bool smoothStart, bool smoothEnd)
            RenderSegment.Invoke(null, new object[] { netInfo, NetSegment.Flags.All, bezier.a, bezier.d, startDirection, -endDirection, smoothStart, smoothEnd });
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
                else
                {
                    DebugUtils.Log("Warning! Invalid segment directions!");
                }
            }

            return (startPos + endPos) / 2f;
        }

        private static MethodInfo RenderSegment = typeof(NetTool).GetMethod("RenderSegment", BindingFlags.NonPublic | BindingFlags.Static);
    }
}
