using UnityEngine;

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

            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    if (instance.id.Type == InstanceType.NetNode)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            ushort segment = NetManager.instance.m_nodes.m_buffer[instance.id.NetNode].GetSegment(i);
                            if (segment != 0)
                            {
                                InstanceID instanceID = default(InstanceID);
                                instanceID.NetSegment = segment;

                                newSelection.Add((Instance)instanceID);
                            }
                        }
                    }
                }
            }

            foreach (Instance instance in newSelection)
            {
                m_states.Add(instance.GetState());
            }
        }

        public override void Do()
        {
            m_oldSelection = selection;

            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
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

            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, null);
                    toReplace.Add(state.instance, clone);
                    clonedNodes.Add(state.instance.id.NetNode, clone.id.NetNode);
                }
            }

            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetNode) continue;

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
                }

                Instance clone = state.instance.Clone(state, clonedNodes);
                toReplace.Add(state.instance, clone);
            }

            if (replaceInstances)
            {
                ReplaceInstances(toReplace);
                ActionQueue.instance.ReplaceInstancesBackward(toReplace);

                selection = m_oldSelection;
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
