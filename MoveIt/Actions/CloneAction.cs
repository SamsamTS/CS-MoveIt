using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MoveIt
{
    public class CloneActionImport : CloneActionBase
    {
        // Constructor for imported selections
        public CloneActionImport(InstanceState[] states, Vector3 centerPoint)
        {
            bool includesPO = false;

            m_oldSelection = selection;
            m_states.Clear();

            foreach (InstanceState state in states)
            {
                if (state.instance != null && state.Info.Prefab != null)
                {
                    if (state is ProcState)
                    {
                        continue;
                    }

                    m_states.Add(state);

                    //if (state is ProcState)
                    //{
                    //    includesPO = true;
                    //}
                }
            }

            if (includesPO && !MoveItTool.PO.Active)
            {
                MoveItTool.PO.InitialiseTool(true);
            }

            center = centerPoint;
        }
    }

    public class CloneAction : CloneActionMain
    {
        public CloneAction() : base() {}

        //public override void Do()
        //{
        //    base.Do();

        //    // Clone integrations
        //    foreach (var item in m_stateToClone)
        //    {
        //        foreach (var data in item.Key.IntegrationData)
        //        {
        //            try
        //            {
        //                data.Key.Paste(item.Value.id, data.Value, m_InstanceID_origToClone);
        //            }
        //            catch (Exception e)
        //            {
        //                InstanceID sourceInstanceID = item.Key.instance.id;
        //                InstanceID targetInstanceID = item.Value.id;
        //                Log.Error($"integration {data.Key} Failed to paste from " +
        //                    $"{sourceInstanceID.Type}:{sourceInstanceID.Index} to {targetInstanceID.Type}:{targetInstanceID.Index}");
        //                DebugUtils.LogException(e);
        //            }
        //        }
        //    }
        //}
    }

    public class DuplicateAction : CloneActionMain
    {
        public DuplicateAction() : base()
        {
            angleDelta = 0f;
            moveDelta = Vector3.zero;
        }
    }

    public class CloneActionMain : CloneActionBase
    {
        public CloneActionMain() : base()
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
    }

    public class CloneActionBase : Action
    {
        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public bool followTerrain;

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>(); // the InstanceStates to be cloned
        internal HashSet<Instance> m_clones; // the resulting Instances
        internal HashSet<Instance> m_oldSelection; // The selection before cloning

        internal Dictionary<Instance, Instance> m_origToClone; // Original -> Clone mapping for updating action queue on undo/redo 
        internal Dictionary<Instance, Instance> m_origToCloneUpdate; // Updated map while processing clone job
        internal Dictionary<ushort, ushort> m_nodeOrigToClone; // Map of node clones, to connect cloned segments
        protected Dictionary<InstanceState, Instance> m_stateToClone = new Dictionary<InstanceState, Instance>(); // Map of original state to clone's instance
        protected Dictionary<InstanceID, InstanceID> m_InstanceID_origToClone = new Dictionary<InstanceID, InstanceID>();
        internal Dictionary<PO_Group, PO_Group> m_POGroupMap = new Dictionary<PO_Group, PO_Group>();

        protected Matrix4x4 matrix4x = default;

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
            m_stateToClone = new Dictionary<InstanceState, Instance>();
            m_InstanceID_origToClone = new Dictionary<InstanceID, InstanceID>();

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
                        m_stateToClone.Add(state, clone);
                        m_InstanceID_origToClone.Add(state.instance.id, clone.id);
                        m_origToCloneUpdate.Add(state.instance, clone);
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
                        Log.Debug($"Failed to clone {state}");
                        continue;
                    }

                    m_clones.Add(clone);
                    m_stateToClone.Add(state, clone);
                    m_InstanceID_origToClone.Add(state.instance.id, clone.id);
                    m_origToCloneUpdate.Add(state.instance, clone);

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
                                // Log.Debug($"Mapping lane:{lane0.NetLane} to {lane.NetLane}");
                                m_InstanceID_origToClone.Add(lane0, lane);
                            }
                        }
                    }
                }
            }

            // Clone PO
            MoveItTool.PO.MapGroupClones(m_states, this);
            foreach (InstanceState state in m_states)
            {
                if (state is ProcState)
                {
                    _ = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToClone, this);
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
            try
            { 
                MoveItTool.UpdatePillarMap();
            }
            catch (Exception e)
            {
                DebugUtils.Log("CloneActionBase.Do failed");
                DebugUtils.LogException(e);
            }

            // Clone integrations
            foreach (var item in m_stateToClone)
            {
                foreach (var data in item.Key.IntegrationData)
                {
                    try
                    {
                        //Debug.Log($"Integrated-Paste\n- {item.Value.id} {item.Value.id.Debug()}\n- {data.Value}");
                        data.Key.Paste(item.Value.id, data.Value, m_InstanceID_origToClone);
                    }
                    catch (Exception e)
                    {
                        InstanceID sourceInstanceID = item.Key.instance.id;
                        InstanceID targetInstanceID = item.Value.id;
                        Log.Error($"integration {data.Key} Failed to paste from " +
                            $"{sourceInstanceID.Type}:{sourceInstanceID.Index} to {targetInstanceID.Type}:{targetInstanceID.Index}");
                        DebugUtils.LogException(e);
                    }
                }
            }
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
                    InstanceState newState = new InstanceState
                    {
                        instance = state.instance,
                        Info = state.Info,

                        position = matrix4x.MultiplyPoint(state.position - center)
                    };
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
}
