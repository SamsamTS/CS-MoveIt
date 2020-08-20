using ColossalFramework;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using ColossalFramework.PlatformServices;

namespace MoveIt
{
    public class CloneAction : Action
    {
        protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
        protected static PropInstance[] propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;
        protected static TreeInstance[] treeBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public bool followTerrain;

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>(); // the InstanceStates to be cloned
        internal HashSet<Instance> m_clones; // the resulting Instances
        internal HashSet<Instance> m_oldSelection; // The selection before cloning

        protected bool _isImport = false;

        internal Dictionary<Instance, Instance> m_origToClone; // Original -> Clone mapping for updating action queue on undo/redo 
        internal Dictionary<Instance, Instance> m_origToCloneUpdate; // Updated map while processing clone job
        internal Dictionary<ushort, ushort> m_nodeOrigToClone; // Map of node clones, to connect cloned segments

        protected Matrix4x4 matrix4x = default;

        public CloneAction()
        {
            m_oldSelection = selection;

            HashSet<Instance> newSelection = GetCleanSelection(out center);
            if (newSelection.Count == 0) return;

            // Save states
            foreach (Instance instance in newSelection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.SaveToState());
                }
            }
        }

        public static HashSet<Instance> GetCleanSelection(out Vector3 center)
        {
            HashSet<Instance> newSelection = new HashSet<Instance>(selection);

            InstanceID id = new InstanceID();

            // Adding missing nodes
            foreach (Instance instance in selection)
            {
                if (instance is MoveableSegment)
                {
                    ushort segment = instance.id.NetSegment;

                    id.NetNode = segmentBuffer[segment].m_startNode;
                    newSelection.Add(id);

                    id.NetNode = segmentBuffer[segment].m_endNode;
                    newSelection.Add(id);
                }
            }

            // Adding missing segments
            foreach (Instance instance in selection)
            {
                if (instance.id.Type == InstanceType.NetNode)
                {
                    ushort node = instance.id.NetNode;
                    for (int i = 0; i < 8; i++)
                    {
                        ushort segment = nodeBuffer[node].GetSegment(i);
                        id.NetSegment = segment;

                        if (segment != 0 && !newSelection.Contains(id))
                        {
                            ushort startNode = segmentBuffer[segment].m_startNode;
                            ushort endNode = segmentBuffer[segment].m_endNode;

                            if (node == startNode)
                            {
                                id.NetNode = endNode;
                            }
                            else
                            {
                                id.NetNode = startNode;
                            }

                            if (newSelection.Contains(id))
                            {
                                id.NetSegment = segment;
                                newSelection.Add(id);
                            }
                        }
                    }
                }
            }

            // Remove single nodes
            HashSet<Instance> toRemove = new HashSet<Instance>();
            foreach (Instance instance in newSelection)
            {
                if (instance.id.Type == InstanceType.NetNode)
                {
                    bool found = false;
                    ushort node = instance.id.NetNode;

                    for (int i = 0; i < 8; i++)
                    {
                        ushort segment = nodeBuffer[node].GetSegment(i);
                        id.NetSegment = segment;

                        if (segment != 0 && newSelection.Contains(id))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        toRemove.Add(instance);
                    }
                }
            }
            newSelection.ExceptWith(toRemove);

            if (newSelection.Count > 0)
            {
                // Calculate center
                Bounds totalBounds = default;
                bool init = false;

                foreach (Instance instance in newSelection)
                {
                    if (init)
                    {
                        totalBounds.Encapsulate(instance.GetBounds());
                    }
                    else
                    {
                        totalBounds = instance.GetBounds();
                        init = true;
                    }
                }

                center = totalBounds.center;
            }
            else
            {
                center = Vector3.zero;
            }

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

            return sorted;
        }

        // Constructor for imported selections
        public CloneAction(InstanceState[] states, Vector3 centerPoint)
        {
            m_oldSelection = selection;

            foreach (InstanceState state in states)
            {
                if (state.instance != null && state.Info.Prefab != null)
                {
                    m_states.Add(state);
                }
            }

            center = centerPoint;
            _isImport = true;
        }

        public override void Do()
        {
            if (MoveItTool.POProcessing > 0)
            {
                return;
            }

            MoveItTool.instance.m_lastInstance = null;
            m_clones = new HashSet<Instance>();
            m_origToCloneUpdate = new Dictionary<Instance, Instance>();
            m_nodeOrigToClone = new Dictionary<ushort, ushort>();
            var stateToClone = new Dictionary<InstanceState, Instance>();
            var InstanceID_origToClone = new Dictionary<InstanceID, InstanceID>();

            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            // Clone nodes first
            foreach (InstanceState state in m_states)
            {
                if (state is NodeState)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToClone, this);

                    if (clone != null)
                    {
                        m_clones.Add(clone);
                        stateToClone.Add(state, clone);
                        InstanceID_origToClone.Add(state.instance.id, clone.id);
                        m_origToCloneUpdate.Add(state.instance.id, clone.id);
                        m_nodeOrigToClone.Add(state.instance.id.NetNode, clone.id.NetNode);
                    }
                }
            }

            // Clone everything else except PO
            foreach (InstanceState state in m_states)
            {
                if (!(state is NodeState || state is ProcState))
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToClone, this);

                    if (clone == null)
                    {
                        Debug.Log($"Failed to clone {state}");
                        continue;
                    }

                    m_clones.Add(clone);
                    stateToClone.Add(state, clone);
                    InstanceID_origToClone.Add(state.instance.id, clone.id);
                    m_origToCloneUpdate.Add(state.instance.id, clone.id);
;
                    if (state is SegmentState segmentState)
                    {
                        MoveItTool.NS.SetSegmentModifiers(clone.id.NetSegment, segmentState);
                        if(segmentState.LaneIDs!=null)
                        {
                            // old version does not store lane ids
                            var clonedLaneIds = MoveableSegment.GetLaneIds(clone.id.NetSegment);
                            DebugUtils.AssertEq(clonedLaneIds.Count, segmentState.LaneIDs.Count, "clonedLaneIds.Count, segmentState.LaneIDs.Count");
                            for (int i=0;i< clonedLaneIds.Count; ++i)
                            {
                                var lane0 = new InstanceID { NetLane = segmentState.LaneIDs[i] };
                                var lane = new InstanceID { NetLane = clonedLaneIds[i] };
                                // Debug.Log($"Mapping lane:{lane0.NetLane} to {lane.NetLane}");
                                InstanceID_origToClone.Add(lane0, lane);
                            }
                        }
                    }
                }
            }

            // backward compatibility.
            // Clone NodeController after segments have been added.
            foreach (var item in stateToClone)
            {
                if (item.Key is NodeState nodeState)
                {
                    Instance clone = item.Value;
                    ushort nodeID = clone.id.NetNode;
                    MoveItTool.NodeController.PasteNode(nodeID, nodeState);
                }
            }

            // Clone TMPE rules // TODO remove when TMPE switches to integration
            foreach (var state in m_states)
            {
                if (state is NodeState nodeState)
                {
                    MoveItTool.TMPE.Paste(nodeState.TMPE_NodeRecord, InstanceID_origToClone);
                }
                else if (state is SegmentState segmentState)
                {
                    MoveItTool.TMPE.Paste(segmentState.TMPE_SegmentRecord, InstanceID_origToClone);
                    MoveItTool.TMPE.Paste(segmentState.TMPE_SegmentStartRecord, InstanceID_origToClone);
                    MoveItTool.TMPE.Paste(segmentState.TMPE_SegmentEndRecord, InstanceID_origToClone);
                }
            }

            // Clone PO
            foreach (InstanceState state in m_states)
            {
                if (state is ProcState)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToClone, this);
                }
            }

            // clone integrations.
            foreach (var item in stateToClone)
            {
                foreach (var data in item.Key.IntegrationData)
                {
                    try
                    {
                        data.Key.Paste(item.Value.id, data.Value, InstanceID_origToClone);
                    }
                    catch (Exception e)
                    {
                        InstanceID sourceInstanceID = item.Key.instance.id;
                        InstanceID targetInstanceID = item.Value.id;
                        Debug.LogError($"integration {data.Key} Failed to paste from " +
                            $"{sourceInstanceID.Type}:{sourceInstanceID.Index} to {targetInstanceID.Type}:{targetInstanceID.Index}");
                        DebugUtils.LogException(e);
                    }
                }
            }

            if (m_origToClone != null)
            {
                Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();

                foreach (Instance key in m_origToClone.Keys)
                {
                    toReplace.Add(m_origToClone[key], m_origToCloneUpdate[key]);
                    DebugUtils.Log("To replace: " + m_origToClone[key].id.RawData + " -> " + m_origToCloneUpdate[key].id.RawData);
                }

                ActionQueue.instance.ReplaceInstancesForward(toReplace);
            }

            m_origToClone = m_origToCloneUpdate;

            // Select clones
            selection = m_clones;
            MoveItTool.m_debugPanel.UpdatePanel();

            UpdateArea(GetTotalBounds(false));
            MoveItTool.UpdatePillarMap();
        }

        public override void Undo()
        {
            if (m_clones == null) return;

            Bounds bounds = GetTotalBounds(false);

            foreach (Instance instance in m_clones)
            {
                instance.Delete();
            }

            m_clones = null;

            // Restore selection
            selection = m_oldSelection;
            MoveItTool.m_debugPanel.UpdatePanel();

            UpdateArea(bounds);
            MoveItTool.UpdatePillarMap();
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("CloneAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }

            foreach (Instance instance in toReplace.Keys)
            {
                if (m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("CloneAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }
            }

            if (m_origToClone != null)
            {
                Dictionary<Instance, Instance> clonedOrigin = new Dictionary<Instance, Instance>();

                foreach (Instance key in m_origToClone.Keys)
                {
                    if (toReplace.ContainsKey(key))
                    {
                        clonedOrigin.Add(toReplace[key], m_origToClone[key]);
                        DebugUtils.Log("CloneAction Replacing: " + key.id.RawData + " -> " + toReplace[key].id.RawData);
                    }
                    else
                    {
                        clonedOrigin.Add(key, m_origToClone[key]);
                    }
                }
                m_origToClone = clonedOrigin;
            }
        }

        public HashSet<InstanceState> CalculateStates(Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Matrix4x4 matrix4x = default;
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    InstanceState newState = new InstanceState();
                    newState.instance = state.instance;
                    newState.Info = state.Info;

                    newState.position = matrix4x.MultiplyPoint(state.position - center);
                    newState.position.y = state.position.y + deltaPosition.y;

                    if (followTerrain)
                    {
                        newState.terrainHeight = Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(newState.position);
                        newState.position.y = newState.position.y + newState.terrainHeight - state.terrainHeight;
                    }

                    newState.angle = state.angle + deltaAngle;

                    newStates.Add(newState);
                }
            }
            return newStates;
        }

        public int Count
        {
            get { return m_states.Count; }
        }
    }


    public class DuplicateAction : CloneAction
    {
        public DuplicateAction() : base()
        {
            angleDelta = 0f;
            moveDelta = Vector3.zero;
        }
    }
}
