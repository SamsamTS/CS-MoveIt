using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MoveIt
{
    public class ResetAction : BulldozeAction
    {
        public override void Do()
        {
            DoImplementation(true);
            UndoImplementation(true);
        }

        public override void Undo()
        {
            ;
        }
    }


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
                                InstanceID instanceID = default;
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

                            if (node.m_building == 0 || node.Info.m_class.m_layer == ItemClass.Layer.WaterPipes)
                            { // Exclude attached nodes with attached buildings (e.g. water buildings)
                                for (int i = 0; i < 8; i++)
                                {
                                    ushort segmentId = node.GetSegment(i);
                                    if (segmentId != 0 && MoveableSegment.isSegmentValid(segmentId) && 
                                            ((netManager.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Untouchable) == NetSegment.Flags.None))
                                    {
                                        InstanceID instanceID = default;
                                        instanceID.NetSegment = segmentId;

                                        if (selection.Contains(instanceID)) continue;

                                        newSelection.Add(new MoveableSegment(instanceID));
                                        segments.Add(segmentId);
                                    }
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
            //Debug.Log(msg + $"AAA Initial Selection: {selection.Count}");

            //msg = $"\nSelection With Extra Segments: {newSelection.Count} (Total Segments: {segments.Count})\n";

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
                                if (node.Info.m_class.m_layer == ItemClass.Layer.WaterPipes)
                                {
                                    foreach (Building b in BuildingManager.instance.m_buildings.m_buffer)
                                    {
                                        if (b.m_netNode == id)
                                        {
                                            toDelete = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (toDelete)
                            {
                                InstanceID instanceId = default;
                                instanceId.NetNode = id;
                                MoveableNode mn = new MoveableNode(instanceId);
                                if (newSelection.Contains(mn)) continue;

                                extraNodes.Add(mn);
                            }
                        }
                    }
                }
            }

            //Debug.Log(msg + $"Selection Without Extra Nodes: {newSelection.Count + extraNodes.Count} (Extra Nodes: {extraNodes.Count})");

            // Sort segments by buildIndex
            HashSet<Instance> sorted = new HashSet<Instance>();
            List<uint> indexes = new List<uint>();
            foreach (Instance instance in newSelection)
            {
                if (instance.id.Type != InstanceType.NetSegment)
                {
                    sorted.Add(instance);
                }
                else
                {
                    uint bi = ((NetSegment)instance.data).m_buildIndex;
                    if (!indexes.Contains(bi))
                        indexes.Add(bi);
                }
            }

            indexes.Sort();

            foreach (uint i in indexes)
            {
                foreach (Instance instance in newSelection)
                {
                    if (instance.id.Type == InstanceType.NetSegment)
                    {
                        if (((NetSegment)instance.data).m_buildIndex == i)
                        {
                            sorted.Add(instance);
                        }
                    }
                }
            }

            //msg = $"Final States:\n";
            foreach (Instance instance in sorted)
            {
                m_states.Add(instance.GetState());
                //msg += $"{instance}\n";
            }
            foreach (Instance instance in extraNodes)
            {
                m_states.Add(instance.GetState());
                //msg += $"{instance}\n";
            }

            //Debug.Log(msg + $"AAA Final Selection: {m_states.Count}");
        }

        public override void Do()
        {
            bool po = false;
            foreach (InstanceState state in m_states)
            {
                if (state is ProcState)
                {
                    po = true;
                    break;
                }
            }
            if (!MoveItTool.POShowDeleteWarning || !po)
            {
                DoImplementation(false);
                return;
            }

            ConfirmPanel panel = UIView.library.ShowModal<ConfirmPanel>("ConfirmPanel", delegate(UIComponent comp, int value)
            {
                if (value == 1)
                    DoImplementation(false);
            });
            panel.SetMessage("Deleting PO", "Procedural Objects can not be undeleted. Are you sure?");
        }

        public void DoImplementation(bool skipPO = false)
        {
            m_oldSelection = selection;

            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                if (skipPO && state is ProcState) continue;
                if (state is BuildingState) continue;

                if (state.instance.isValid)
                {
                    state.instance.Delete();
                }
            }

            // Remove buildings last so attached nodes are cleaned up
            foreach (InstanceState state in m_states)
            {
                if (skipPO && state is ProcState) continue;
                if (!(state is BuildingState)) continue;

                if (state.instance.isValid)
                {
                    state.instance.Delete();
                }
            }

            UpdateArea(bounds);

            selection = new HashSet<Instance>();
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public override void Undo()
        {
            UndoImplementation(false);
        }

        public void UndoImplementation(bool reset = false)
        {
            if (m_states == null) return;

            Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();

            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

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
                if (state is ProcState) continue;

                Instance clone = state.instance.Clone(state, clonedNodes);
                toReplace.Add(state.instance, clone);

                if (state.instance.id.Type == InstanceType.Prop)
                {
                    PropManager.instance.m_props.m_buffer[clone.id.Prop].FixedHeight = ((PropState)state).fixedHeight;
                }
                else if (state.instance.id.Type == InstanceType.Building)
                {
                    // Add attached nodes to the clonedNode list so other segments reconnect
                    BuildingState buildingState = state as BuildingState;
                    List<ushort> origNodeIds = new List<ushort>();

                    MoveableBuilding cb = clone as MoveableBuilding;
                    ushort cloneNodeId = ((Building)cb.data).m_netNode;

                    if (reset)
                    {
                        ushort cloneId = cb.id.Building;

                        buildingBuffer[cloneId].m_flags = buildingBuffer[cloneId].m_flags & ~Building.Flags.BurnedDown;
                        buildingBuffer[cloneId].m_flags = buildingBuffer[cloneId].m_flags & ~Building.Flags.Collapsed;
                        buildingBuffer[cloneId].m_flags = buildingBuffer[cloneId].m_flags & ~Building.Flags.Abandoned;
                        buildingBuffer[cloneId].m_flags = buildingBuffer[cloneId].m_flags | Building.Flags.Active;
                        //Debug.Log($"After [{cloneId}]: {buildingBuffer[cloneId].m_flags}");
                        Thread.Sleep(50);
                    }

                    if (cloneNodeId != 0)
                    {
                        int c = 0;
                        //string msg2 = "Original attached nodes:";
                        foreach (InstanceState i in buildingState.subStates)
                        {
                            if (i is NodeState ns)
                            {
                                InstanceID instanceID = default;
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
                if (state is SegmentState segmentState)
                {
                    if (!clonedNodes.ContainsKey(segmentState.startNode))
                    {
                        InstanceID instanceID = InstanceID.Empty;
                        instanceID.NetNode = segmentState.startNode;

                        // Don't clone if node is missing
                        if (!((Instance)instanceID).isValid) continue;

                        clonedNodes.Add(segmentState.startNode, segmentState.startNode);
                    }

                    if (!clonedNodes.ContainsKey(segmentState.endNode))
                    {
                        InstanceID instanceID = InstanceID.Empty;
                        instanceID.NetNode = segmentState.endNode;

                        // Don't clone if node is missing
                        if (!((Instance)instanceID).isValid) continue;

                        clonedNodes.Add(segmentState.endNode, segmentState.endNode);
                    }

                    Instance clone = state.instance.Clone(state, clonedNodes);
                    toReplace.Add(state.instance, clone);
                    MoveItTool.NS.SetSegmentModifiers(clone.id.NetSegment, segmentState);
                }
            }

            if (replaceInstances)
            {
                ReplaceInstances(toReplace);
                ActionQueue.instance.ReplaceInstancesBackward(toReplace);

                selection = new HashSet<Instance>();
                foreach (Instance i in m_oldSelection)
                {
                    if (i is MoveableProc) continue;
                    selection.Add(i);
                }
                MoveItTool.m_debugPanel.UpdatePanel();
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

            if (m_oldSelection == null)
            {
                return;
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
