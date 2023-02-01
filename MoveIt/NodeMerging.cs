using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    internal class NodeMerging
    {
        internal const float MAX_SNAP_DISTANCE = 9f;

        /// <summary>
        /// Check if two nodes can be merged and if possible, merge and delete child
        /// </summary>
        /// <param name="parentId">The node InstanceID to merge into</param>
        /// <param name="childId">The node InstanceID to merge from, is then deleted</param>
        /// <param name="tolerance">The max positional distance to be considered overlapping</param>
        /// <returns></returns>
        internal static bool MergeNodes(InstanceID parentId, InstanceID childId, float tolerance = MAX_SNAP_DISTANCE)
        {
            if (!CanMergeNodes(parentId, childId, tolerance)) return false;
            return DoMergeNodes(parentId, childId);
        }

        /// <summary>
        /// Get the distance between two nodes
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <returns>Distance in metres</returns>
        internal static float GetNodeDistance(NetNode a, NetNode b)
        {
            return (a.m_position - b.m_position).magnitude;
        }

        /// <summary>
        /// Get the distance between a node and a vector
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Position</param>
        /// <returns>Distance in metres</returns>
        internal static float GetNodeDistance(NetNode a, Vector3 b)
        {
            return (a.m_position - b).magnitude;
        }

        /// <summary>
        /// Get the distance between a node and a vector
        /// </summary>
        /// <param name="data">NodeMergeData object</param>
        /// <returns>Distance in metres</returns>
        internal static float GetNodeDistance(NodeMergeData data)
        {
            return (data.GetParentNode().m_position - data.adjustedState.position).magnitude;
        }

        /// <summary>
        /// Check if two nodes can be combined
        /// </summary>
        /// <param name="parentId">The node InstanceID to be merged into</param>
        /// <param name="childId">The node InstanceID to merged from</param>
        /// <param name="tolerance">The max positional distance to be considered overlapping</param>
        /// <returns></returns>
        internal static bool CanMergeNodes(InstanceID parentId, InstanceID childId, float tolerance = MAX_SNAP_DISTANCE)
        {
            if (parentId == childId) return false;

            MoveableNode node = new MoveableNode(childId);
            NodeState state = (NodeState)node.SaveToState();

            return CanMergeNodes(parentId.NetNode, state, tolerance);
        }

        /// <summary>
        /// Check if two nodes can be combined
        /// </summary>
        /// <param name="parentId">The node id to be merged into</param>
        /// <param name="childId">The node InstanceID to merged from</param>
        /// <param name="tolerance">The max positional distance to be considered overlapping</param>
        /// <returns></returns>
        //internal static bool CanMergeNodes(ushort parentId, InstanceID childId, float tolerance = MAX_SNAP_DISTANCE)
        //{
        //    if (parentId == childId.NetNode) return false;

        //    MoveableNode node = new MoveableNode(childId);
        //    NodeState state = (NodeState)node.SaveToState();

        //    return CanMergeNodes(parentId, state, tolerance);

        //    //NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

        //    //ref NetNode parent = ref nodeBuffer[parentId];
        //    //ref NetNode child = ref nodeBuffer[childId.NetNode];

        //    //if (!((parent.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created && (child.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created)) return false;
        //    //if (!(parent.Info.m_class.m_service == child.Info.m_class.m_service && parent.Info.m_class.m_subService == child.Info.m_class.m_subService)) return false;

        //    //return GetNodeDistance(parent, child) < tolerance;
        //}

        /// <summary>
        /// Check if two nodes can be combined
        /// </summary>
        /// <param name="parentId">The node id to be merged into</param>
        /// <param name="state">The node state to merged from</param>
        /// <param name="tolerance">The max positional distance to be considered overlapping</param>
        /// <returns></returns>
        internal static bool CanMergeNodes(ushort parentId, NodeState state, float tolerance = MAX_SNAP_DISTANCE)
        {
            NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

            ref NetNode parent = ref nodeBuffer[parentId];
            NetInfo childInfo = (NetInfo)state.Info.Prefab;

            //Log.Debug($"FFF01 {parentId} ({parent.m_flags}) {tolerance}m\n  {parent.m_position},{state.position}={GetNodeDistance(parent, state.position)}\n" +
            //    $"  {parent.Info.m_class.m_service} / {parent.Info.m_class.m_subService}\n  {childInfo.m_class.m_service} / {childInfo.m_class.m_subService}");
            if (!((parent.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created)) return false;
            if (!(parent.Info.m_class.m_service == childInfo.m_class.m_service && parent.Info.m_class.m_subService == childInfo.m_class.m_subService)) return false;

            int segCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (parent.GetSegment(i) > 0) segCount++;
                if (((NetNode)state.instance.data).GetSegment(i) > 0) segCount++;
            }
            if (segCount > 8) return false;

            return GetNodeDistance(parent, state.position) < tolerance;
        }

        /// <summary>
        /// Combine two nodes into one
        /// </summary>
        /// <param name="parentId">The node InstanceID to merge into</param>
        /// <param name="childId">The node InstanceID to merge from, is then deleted</param>
        /// <returns>True if successful (child is deleted), false if failed and child remains</returns>
        private static bool DoMergeNodes(InstanceID parentId, InstanceID childId)
        {
            NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

            ref NetNode parent = ref nodeBuffer[parentId.NetNode];
            ref NetNode child = ref nodeBuffer[childId.NetNode];

            List<ushort> segments = new List<ushort>();
            for (int i = 0; i < 8; i++)
            {
                if (parent.GetSegment(i) > 0) segments.Add(parent.GetSegment(i));
                if (child.GetSegment(i) > 0) segments.Add(child.GetSegment(i));
            }
            string msg = "";
            foreach (ushort x in segments) msg += $"{x}, ";
            Log.Debug($"BBB03 {parentId},{childId} {segments.Count}: {msg}");
            if (segments.Count < 2 || segments.Count > 8) return false;

            parent.m_segment0 = segments[0];
            parent.m_segment1 = segments[1];
            if (segments.Count > 2) parent.m_segment2 = segments[2]; else parent.m_segment2 = 0;
            if (segments.Count > 3) parent.m_segment3 = segments[3]; else parent.m_segment3 = 0;
            if (segments.Count > 4) parent.m_segment4 = segments[4]; else parent.m_segment4 = 0;
            if (segments.Count > 5) parent.m_segment5 = segments[5]; else parent.m_segment5 = 0;
            if (segments.Count > 6) parent.m_segment6 = segments[6]; else parent.m_segment6 = 0;
            if (segments.Count > 7) parent.m_segment7 = segments[7]; else parent.m_segment7 = 0;

            for (int i = 0; i < 8; i++)
            {
                if (child.GetSegment(i) > 0) SwitchSegmentNode(child.GetSegment(i), childId, parentId);
            }

            if (child.m_segment0 > 0) { child.m_segment0 = 0; }
            if (child.m_segment1 > 0) { child.m_segment1 = 0; }
            if (child.m_segment2 > 0) { child.m_segment2 = 0; }
            if (child.m_segment3 > 0) { child.m_segment3 = 0; }
            if (child.m_segment4 > 0) { child.m_segment4 = 0; }
            if (child.m_segment5 > 0) { child.m_segment5 = 0; }
            if (child.m_segment6 > 0) { child.m_segment6 = 0; }
            if (child.m_segment7 > 0) { child.m_segment7 = 0; }

            MoveableNode childNode = new MoveableNode(childId);
            childNode.Delete();

            Log.Info($"Merged node {childId} into {parentId} ({segments.Count} segments)");

            return true;
        }

        private static void SwitchSegmentNode(ushort segmentId, InstanceID fromId, InstanceID toId)
        {
            NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;

            ref NetSegment segment = ref segmentBuffer[segmentId];
            if (segment.m_startNode == fromId.NetNode)
                segment.m_startNode = toId.NetNode;
            else if (segment.m_endNode == fromId.NetNode)
                segment.m_endNode = toId.NetNode;
            else
                Log.Info($"Node not found for segment #{segmentId} (switching {fromId} to {toId})");
            Log.Debug($"BBB06 Node switch segment #{segmentId} (switching {fromId} to {toId}) - {segmentBuffer[segmentId].m_startNode},{segmentBuffer[segmentId].m_endNode}");
        }
    }

    internal class NodeMergeData
    {
        internal NodeState nodeState;
        internal NodeState adjustedState;
        internal ushort parentNode;
        internal NodeMergeStatuses status;

        private float _distance = -1;
        internal float Distance
        {
            get
            {
                if (_distance == -1)
                {
                    _distance = NodeMerging.GetNodeDistance(this);
                }
                return _distance;
            }
        }

        internal ushort StateId => nodeState.instance.id.NetNode;

        internal NetNode GetParentNode()
        {
            return Singleton<NetManager>.instance.m_nodes.m_buffer[parentNode];
        }

        public override string ToString()
        {
            return $"{nodeState.instance.id.NetNode}:{parentNode}={Distance}";
        }


        internal static NodeMergeData Get(List<NodeMergeData> list, NodeState state)
        {
            foreach (NodeMergeData data in list)
            {
                if (data.nodeState == state && (data.status == NodeMergeStatuses.Snap || data.status == NodeMergeStatuses.Merge)) return data;
            }
            return null;
        }

        internal static bool CanMerge(List<NodeMergeData> list, NodeState state)
        {
            return Get(list, state) != null;
        }

        internal static NodeMergeData GetSnap(List<NodeMergeData> nodes)
        {
            foreach (NodeMergeData data in nodes)
            {
                if (data.status == NodeMergeStatuses.Snap) return data;
            }
            return null;
        }
    }

    internal enum NodeMergeStatuses
    {
        None,
        Merge,
        Snap,
        Invalid
    }
}
