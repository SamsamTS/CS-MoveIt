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

        //HashSet<ushort> buildingNodes = new HashSet<ushort>(); // Buildings already selected to be removed

        public BulldozeAction()
        {
            HashSet<Instance> newSelection = new HashSet<Instance>(selection);
            HashSet<Instance> extraNodes = new HashSet<Instance>();
            HashSet<ushort> segments = new HashSet<ushort>(); // Segments to be removed

            Debug.Log("Selection: " + selection.Count);

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
                            ushort segment = NetManager.instance.m_nodes.m_buffer[instance.id.NetNode].GetSegment(i);
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
                }
            }
            Debug.Log($"newSelection: {newSelection.Count}, Segments found: {segments.Count}");

            // Add any nodes attached to selected buildings
            //foreach (Instance instance in newSelection)
            //{
            //    if (instance.isValid)
            //    {
            //        if (instance is MoveableBuilding mb)
            //        {
            //            foreach (Instance i in mb.subInstances)
            //            {
            //                if (i is MoveableNode mn)
            //                {
            //                    if (!newSelection.Contains(mn))
            //                    {
            //                        Debug.Log($"Adding node:{mn.id.NetNode}");
            //                        buildingNodes.Add(mn.id.NetNode);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            // Add any nodes whose segments are all selected
            foreach (Instance instance in newSelection)
            {
                if (instance.isValid)
                {
                    if (instance.id.Type == InstanceType.NetSegment)
                    {
                        ushort segId = instance.id.NetSegment;
                        ushort[] nodeIds = { NetManager.instance.m_segments.m_buffer[segId].m_startNode, NetManager.instance.m_segments.m_buffer[segId].m_endNode };
                        foreach (ushort id in nodeIds)
                        {
                            bool toDelete = true;
                            NetNode node = NetManager.instance.m_nodes.m_buffer[id];

                            ushort ownerBuilding = NetNode.FindOwnerBuilding(id, 363f);
                            //if (!(ownerBuilding > 0 && buildings.Contains(ownerBuilding)))
                            //{
                                for (int i = 0; i < 8; i++)
                                {
                                    if (node.GetSegment(i) != 0 && !segments.Contains(node.GetSegment(i)))
                                    {
                                        toDelete = false;
                                        break;
                                    }
                                }
                            //}
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
            Debug.Log("m_states: " + m_states.Count);
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
        }

        public override void Undo()
        {
            if (m_states == null) return;

            Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();
            //Dictionary<ushort, ushort> attachedNodes = new Dictionary<ushort, ushort>();

            // Recreate nodes
            foreach (InstanceState state in m_states)
            {
                Debug.Log($"{state.instance.id.Type}");
                if (state.instance.id.Type == InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, null);
                    toReplace.Add(state.instance, clone);
                    clonedNodes.Add(state.instance.id.NetNode, clone.id.NetNode);
                    ActionQueue.instance.UpdateNodeIdInStateHistory(state.instance.id.NetNode, clone.id.NetNode);
                    Debug.Log($"Cloned N:{state.instance.id.NetNode}->{clone.id.NetNode}");
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
                    List<ushort> origNodeIds = new List<ushort>();

                    int c = 0;
                    foreach (InstanceState i in ((BuildingState)state).subStates)
                    {
                        if (i is NodeState ns)
                        {
                            InstanceID instanceID = default(InstanceID);
                            instanceID.RawData = ns.id;
                            origNodeIds.Insert(c++, instanceID.NetNode);
                            Debug.Log($"\n{c} - orig:{instanceID.NetNode}, {ns.Info.Name}");
                        }
                    }

                    MoveableBuilding cb = clone as MoveableBuilding;
                    ushort cloneNodeId = ((Building)cb.data).m_netNode;

                    Debug.Log($"{((Building)cb.data).Info.name} attached - orig:{origNodeIds[0]}, new:{cloneNodeId}");

                    c = 0;
                    while (cloneNodeId != 0)
                    {
                        ushort origNodeId = origNodeIds[c];

                        NetNode clonedAttachedNode = NetManager.instance.m_nodes.m_buffer[cloneNodeId];
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

                        Debug.Log($"{c} - orig:{origNodeId}, new:{cloneNodeId} (next:{clonedAttachedNode.m_nextBuildingNode})\n{clonedAttachedNode.Info.GetAI()}");
                        cloneNodeId = clonedAttachedNode.m_nextBuildingNode;

                        if (++c > 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }

                    #region Old
                    //List<ushort> attachedNodeOriginal = new List<ushort>();
                    //List<ushort> attachedNodeClone = new List<ushort>();

                    //foreach (Instance i in mb.subInstances)
                    //{
                    //    if (i is MoveableNode mn)
                    //    {
                    //        attachedNodeOriginal.Add(mn.id.NetNode);
                    //    }
                    //}
                    //foreach (Instance i in cb.subInstances)
                    //{
                    //    if (i is MoveableNode mn)
                    //    {
                    //        attachedNodeClone.Add(mn.id.NetNode);
                    //    }
                    //}
                    #endregion
                }
            }

            Debug.Log("Hello");
            string msg = "Cloned Nodes:\n";
            foreach (KeyValuePair<ushort, ushort> kvp in clonedNodes)
            {
                msg += $"{kvp.Key} => {kvp.Value}\n";
            }
            Debug.Log(msg);

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
