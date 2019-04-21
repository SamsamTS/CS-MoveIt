using UnityEngine;
using ColossalFramework;
using System;
using System.Collections.Generic;

namespace MoveIt
{
    public class BulldozeAction : Action
    {
        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        private HashSet<Instance> m_oldSelection;

        public bool replaceInstances = true;

        public BulldozeAction()
        {
            HashSet<Instance> newSelection = new HashSet<Instance>(selection);
            HashSet<Instance> extraNodes = new HashSet<Instance>();
            HashSet<ushort> segments = new HashSet<ushort>(); // Segments to be removed

            NetManager netManager = Singleton<NetManager>.instance;

            //string msg = $"\nBasic Selection: {selection.Count}\n";

            // Add any segments whose node is selected
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    if (instance.id.Type == InstanceType.NetSegment)
                    {
                        segments.Add(instance.id.NetSegment);
                    }
                    else if (instance.id.Type == InstanceType.NetNode)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            ushort segment = netManager.m_nodes.m_buffer[instance.id.NetNode].GetSegment(i);
                            if (segment != 0)
                            {
                                InstanceID instanceID = default(InstanceID);
                                instanceID.NetSegment = segment;

                                if (selection.Contains(instanceID)) continue;

                                newSelection.Add(new MoveableSegment(instanceID));
                                segments.Add(segment);
                            }
                        }
                    }
                    else if (instance.id.Type == InstanceType.Building)
                    {
                        Building building = (Building)((MoveableBuilding)instance).data;
                        ushort nodeId = building.m_netNode;

                        int c = 0;
                        while (nodeId != 0)
                        {
                            NetNode node = netManager.m_nodes.m_buffer[nodeId];

                            for (int i = 0; i < 8; i++)
                            {
                                ushort segmentId = node.GetSegment(i);
                                if (segmentId != 0 && MoveableSegment.isSegmentValid(segmentId) && 
                                        ((netManager.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Untouchable) == NetSegment.Flags.None))
                                {
                                    InstanceID instanceID = default(InstanceID);
                                    instanceID.NetSegment = segmentId;

                                    if (selection.Contains(instanceID)) continue;

                                    newSelection.Add(new MoveableSegment(instanceID));
                                    segments.Add(segmentId);
                                }
                            }

                            nodeId = node.m_nextBuildingNode;

                            if (++c > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Nodes: Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }
            }

            //msg += $"Selection With Extra Segments: {newSelection.Count}\nTotal Segments: {segments.Count}\n";

            // Add any nodes whose segments are all selected
            foreach (Instance instance in newSelection)
            {
                if (instance.isValid)
                {
                    if (instance.id.Type == InstanceType.NetSegment)
                    {
                        ushort segId = instance.id.NetSegment;
                        ushort[] nodeIds = { netManager.m_segments.m_buffer[segId].m_startNode, netManager.m_segments.m_buffer[segId].m_endNode };
                        foreach (ushort id in nodeIds)
                        {
                            bool toDelete = true;
                            NetNode node = netManager.m_nodes.m_buffer[id];
                            for (int i = 0; i < 8; i++)
                            {
                                if (node.GetSegment(i) != 0 && !segments.Contains(node.GetSegment(i)))
                                {
                                    toDelete = false;
                                    break;
                                }
                            }
                            if (toDelete)
                            {
                                InstanceID instanceId = default(InstanceID);
                                instanceId.NetNode = id;
                                MoveableNode mn = new MoveableNode(instanceId);
                                if (newSelection.Contains(mn)) continue;

                                extraNodes.Add(mn);
                            }
                        }
                    }
                }
            }

            foreach (Instance instance in newSelection)
            {
                m_states.Add(instance.GetState());
            }
            foreach (Instance instance in extraNodes)
            {
                m_states.Add(instance.GetState());
            }

            //Debug.Log(msg + $"Final Selection: {m_states.Count}");
        }

        public override void Do()
        {
            m_oldSelection = selection;

            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                if (state is BuildingState) continue;

                if (state.instance.isValid)
                {
                    state.instance.Delete();
                }
            }

            // Remove buildings last so attached nodes are cleaned up
            foreach (InstanceState state in m_states)
            {
                if (!(state is BuildingState)) continue;

                if (state.instance.isValid)
                {
                    state.instance.Delete();
                }
            }

            UpdateArea(bounds);

            selection = new HashSet<Instance>();
            MoveItTool.m_debugPanel.Update();
        }

        public override void Undo()
        {
            if (m_states == null) return;

            Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();

            // Recreate nodes
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, null);
                    toReplace.Add(state.instance, clone);
                    clonedNodes.Add(state.instance.id.NetNode, clone.id.NetNode);
                    ActionQueue.instance.UpdateNodeIdInStateHistory(state.instance.id.NetNode, clone.id.NetNode);
                    //Debug.Log($"Cloned N:{state.instance.id.NetNode}->{clone.id.NetNode}");
                }
            }

            // Recreate everything except nodes and segments
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetNode) continue;
                if (state.instance.id.Type == InstanceType.NetSegment) continue;

                Instance clone = state.instance.Clone(state, clonedNodes);
                toReplace.Add(state.instance, clone);

                // Add attached nodes to the clonedNode list so other segments reconnect
                if (state.instance.id.Type == InstanceType.Building)
                {
                    BuildingState buildingState = state as BuildingState;
                    List<ushort> origNodeIds = new List<ushort>();

                    MoveableBuilding cb = clone as MoveableBuilding;
                    ushort cloneNodeId = ((Building)cb.data).m_netNode;

                    if (cloneNodeId != 0)
                    {
                        int c = 0;
                        //string msg2 = "Original attached nodes:";
                        foreach (InstanceState i in buildingState.subStates)
                        {
                            if (i is NodeState ns)
                            {
                                InstanceID instanceID = default(InstanceID);
                                instanceID.RawData = ns.id;
                                //msg2 += $"\n{c} - Attached node #{instanceID.NetNode}: {ns.Info.Name}";
                                origNodeIds.Insert(c++, instanceID.NetNode);
                            }
                        }
                        //Debug.Log(msg2);

                        c = 0;
                        //msg2 = "";
                        while (cloneNodeId != 0)
                        {
                            ushort origNodeId = origNodeIds[c];

                            NetNode clonedAttachedNode = Singleton<NetManager>.instance.m_nodes.m_buffer[cloneNodeId];
                            if (clonedAttachedNode.Info.GetAI() is TransportLineAI)
                            {
                                cloneNodeId = clonedAttachedNode.m_nextBuildingNode;
                                continue;
                            }

                            if (clonedNodes.ContainsKey(origNodeId))
                            {
                                Debug.Log($"Node #{origNodeId} is already in clone list!");
                            }

                            clonedNodes.Add(origNodeId, cloneNodeId);

                            //msg2 += $"\n{c} - {origNodeId} -> {cloneNodeId} {clonedAttachedNode.Info.GetAI()}";
                            cloneNodeId = clonedAttachedNode.m_nextBuildingNode;

                            if (++c > 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Nodes: Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        //Debug.Log(msg2);
                    }
                }
            }

            //string msg = "Cloned Nodes:\n";
            //foreach (KeyValuePair<ushort, ushort> kvp in clonedNodes)
            //{
            //    msg += $"{kvp.Key} => {kvp.Value}\n";
            //}
            //Debug.Log(msg);

            // Recreate segments
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetSegment)
                {
                    SegmentState segState = state as SegmentState;

                    if (!clonedNodes.ContainsKey(segState.startNode))
                    {
                        InstanceID instanceID = InstanceID.Empty;
                        instanceID.NetNode = segState.startNode;

                        // Don't clone if node is missing
                        if (!((Instance)instanceID).isValid) continue;

                        clonedNodes.Add(segState.startNode, segState.startNode);
                    }

                    if (!clonedNodes.ContainsKey(segState.endNode))
                    {
                        InstanceID instanceID = InstanceID.Empty;
                        instanceID.NetNode = segState.endNode;

                        // Don't clone if node is missing
                        if (!((Instance)instanceID).isValid) continue;

                        clonedNodes.Add(segState.endNode, segState.endNode);
                    }

                    Instance clone = state.instance.Clone(state, clonedNodes);
                    toReplace.Add(state.instance, clone);
                }
            }

            if (replaceInstances)
            {
                ReplaceInstances(toReplace);
                ActionQueue.instance.ReplaceInstancesBackward(toReplace);

                selection = m_oldSelection;
                MoveItTool.m_debugPanel.Update();
            }
        }

        public override void UpdateNodeIdInSegmentState(ushort oldId, ushort newId)
        {
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetSegment)
                {
                    SegmentState segState = state as SegmentState;
                    
                    if (segState.startNode == oldId)
                    {
                        segState.startNode = newId;
                        //Debug.Log($"SWITCHED (start)\nSegment #{state.instance.id.NetSegment} ({segState.startNode}-{segState.endNode})\nOld node Id:{oldId}, new node Id:{newId}");
                    }
                    if (segState.endNode == oldId)
                    {
                        segState.endNode = newId;
                        //Debug.Log($"SWITCHED (end)\nSegment #{state.instance.id.NetSegment} ({segState.startNode}-{segState.endNode})\nOld node Id:{oldId}, new node Id:{newId}");
                    }
                }
            }
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("BulldozeAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }

            foreach (Instance instance in toReplace.Keys)
            {
                if (m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("BulldozeAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }
            }
        }
    }
}
