using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class AlignSlopeAction : Action
    {
        protected static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
        protected static PropInstance[] propBuffer = PropManager.instance.m_props.m_buffer;
        protected static TreeInstance[] treeBuffer = TreeManager.instance.m_trees.m_buffer;
        protected static NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

        public bool IsQuick = false;

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        private Instance[] keyInstance = new Instance[2];

        public Instance PointA
        {
            get
            {
                return keyInstance[0];
            }
            set
            {
                keyInstance[0] = value;
            }
        }
        public Instance PointB
        {
            get
            {
                return keyInstance[1];
            }
            set
            {
                keyInstance[1] = value;
            }
        }

        public bool followTerrain;

        public AlignSlopeAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.GetState());
                }
            }
        }


        public override void Do()
        {
            float angleDelta;
            float heightDelta;
            float distance;
            Matrix4x4 matrix = default(Matrix4x4);

            if (IsQuick)
            {
                if (selection.Count != 1) return;
                foreach (Instance instance in selection)// Is this really the best way to get the value of selection[0]?
                {
                    if (!instance.isValid || !(instance is MoveableNode nodeInstance)) return;

                    NetNode node = nodeBuffer[nodeInstance.id.NetNode];

                    int c = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        ushort segId = 0;
                        if ((segId = node.GetSegment(i)) > 0)
                        {
                            if (c > 1) return; // More than 2 segments found

                            NetSegment segment = segmentBuffer[segId];
                            if (segment.m_startNode == nodeInstance.id.NetNode)
                            {
                                InstanceID instanceID = default(InstanceID);
                                instanceID.NetNode = segment.m_endNode;
                                keyInstance[c] = new MoveableNode(instanceID);
                            }
                            else
                            {
                                InstanceID instanceID = default(InstanceID);
                                instanceID.NetNode = segment.m_startNode;
                                keyInstance[c] = new MoveableNode(instanceID);
                            }
                            c++;
                        }
                    }
                    if (c != 2) return;
                }
            }

            bool isAllNodes = true;
            foreach (Instance instance in selection)
            {
                if(instance.isValid && instance.id.Type != InstanceType.NetNode && instance.id.Type != InstanceType.NetSegment)
                {
                    isAllNodes = false;
                    break;
                }
            }

            if (isAllNodes)
            {
                List<ushort> unsortedNodes = new List<ushort>();
                List<ushort> connectingSegments = new List<ushort>();
                foreach(Instance instance in selection)
                {
                    if (instance.id.Type != InstanceType.NetSegment)
                    {
                        unsortedNodes.Add(instance.id.NetNode);
                    }
                }
                if (unsortedNodes.Count < 3)
                {
                    Debug.LogError("[Segment Slope Smoother] Not enough nodes to complete process. You probably won't see this error but it's best to put it here anyway, just in case.");
                    return;
                }
                List<List<ushort>> fragmentXZ = new List<List<ushort>>();
                List<ushort> endpoints = new List<ushort>();
                for (int i = 0; i < unsortedNodes.Count; i++)
                {
                    NetNode node = nodeBuffer[unsortedNodes[i]];
                    List<ushort> connections = new List<ushort>();
                    connections.Add(unsortedNodes[i]);
                    for (int j = 0; j < unsortedNodes.Count; j++)
                    {
                        if (i == j) continue;

                        if (node.IsConnectedTo(unsortedNodes[j]))
                        {
                            connections.Add(unsortedNodes[j]);
                        }

                        if (connections.Count > 3)
                        { // the node itself, then both connections
                            Debug.LogError("[Segment Slope Smoother] Validation error: Too many connections! Each node should be only connected by segments to at most two other nodes.");
                            return;
                        }
                    }

                    if (connections.Count == 2)
                    { // the node itself, and one connection
                        endpoints.Add(unsortedNodes[i]);
                    }

                    if (connections.Count == 1)
                    {
                        Debug.LogError("[Segment Slope Smoother] Validation error: No connections! Each node needs at least one other segment connection.");
                        return;
                    }
                    fragmentXZ.Add(connections);
                }


                List<ushort> sortedNodes = new List<ushort>();
                bool incomplete = true;
                int index = -1;
                for (int i = 0; i < fragmentXZ.Count; i++)
                {
                    if (fragmentXZ[i].Count == 2)
                    {
                        index = fragmentXZ.FindIndex(e => e[0] == fragmentXZ[i][1]);
                        sortedNodes.Add(fragmentXZ[i][0]);
                        sortedNodes.Add(fragmentXZ[i][1]);
                        if (index == -1)
                        {
                            Debug.LogError("[Segment Slope Smoother] Sort error: Invalid path! Endpoint is connected to undefined node.");
                            return;
                        }
                        break;
                    }
                }
                while (incomplete)
                {
                    for (int i = 0; i < fragmentXZ.Count; i++)
                    {
                        for (int j = 1; j <= 2; j++)
                        {
                            if ((fragmentXZ[i][0] == fragmentXZ[index][j] && !sortedNodes.Contains(fragmentXZ[index][j])))
                            {
                                sortedNodes.Add(fragmentXZ[i][0]);
                                if (fragmentXZ[i].Count == 2)
                                {
                                    incomplete = false;
                                    break;
                                }
                                index = i;
                            }
                        }
                    }
                }
                for (var i = 0; i < sortedNodes.Count - 1; i++)
                {
                    for (var j = 0; j < nodeBuffer[sortedNodes[i]].CountSegments(); j++)
                    {
                        ushort segmentID = nodeBuffer[sortedNodes[i]].GetSegment(j);
                        NetSegment testedSegment = segmentBuffer[segmentID];
                        if ((testedSegment.m_startNode == sortedNodes[i] || testedSegment.m_endNode == sortedNodes[i]) && (testedSegment.m_startNode == sortedNodes[i + 1] || testedSegment.m_endNode == sortedNodes[i + 1]))
                        {
                            connectingSegments.Add(segmentID);
                            break;
                        }
                    }
                }
                float totalLength = 0;
                List<float> segmentLinearLengthsXZ = new List<float>();
                for (int i = 0; i < connectingSegments.Count; i++)
                {
                    NetSegment calcSegment = segmentBuffer[connectingSegments[i]];
                    float linearDistanceXZ = (float)Math.Sqrt(Math.Pow(calcSegment.m_averageLength, 2) - Math.Pow(nodeBuffer[calcSegment.m_startNode].m_position.y - nodeBuffer[calcSegment.m_endNode].m_position.y, 2));
                    totalLength += linearDistanceXZ;
                    segmentLinearLengthsXZ.Add(linearDistanceXZ);

                }
                float incrementLength = 0;
                NetNode startNode = nodeBuffer[sortedNodes[0]];
                NetNode endNode = nodeBuffer[sortedNodes[sortedNodes.Count - 1]];
                for (int i = 0; i < sortedNodes.Count; i++)
                {
                    float Nln = ((endNode.m_position.y - startNode.m_position.y) / totalLength) * incrementLength + startNode.m_position.y;
                    NetManager.instance.MoveNode(sortedNodes[i], new Vector3(nodeBuffer[sortedNodes[i]].m_position.x, Nln, nodeBuffer[sortedNodes[i]].m_position.z));
                    if (i != sortedNodes.Count - 1) incrementLength += segmentLinearLengthsXZ[i];
                }
            } else {
                angleDelta = 0 - (float)Math.Atan2(PointB.position.z - PointA.position.z, PointB.position.x - PointA.position.x);
                heightDelta = PointB.position.y - PointA.position.y;
                distance = (float)Math.Sqrt(Math.Pow(PointB.position.z - PointA.position.z, 2) + Math.Pow(PointB.position.x - PointA.position.x, 2));

                string msg = $"\nA:{PointA.position}, B:{PointB.position}\nAng:{angleDelta} ({angleDelta * Mathf.Rad2Deg}) - H:{heightDelta} - D:{distance}";
                foreach (InstanceState state in m_states)
                {
                    float distanceOffset, heightOffset;
                    matrix.SetTRS(PointA.position, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                    distanceOffset = (matrix.MultiplyPoint(state.position - PointA.position) - PointA.position).x;
                    heightOffset = distanceOffset / distance * heightDelta;

                    state.instance.SetHeight(Mathf.Clamp(PointA.position.y + heightOffset, 0f, 4000f));

                    msg += $"\nx-offset:{distanceOffset} h-offset:{heightOffset}";
                }
            }
          
            //Debug.Log(msg);
        }


        public override void Undo()
        {
            foreach (InstanceState state in m_states)
            {
                state.instance.SetState(state);
            }

            UpdateArea(GetTotalBounds(false));
        }


        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("AlignSlopeAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }
}
