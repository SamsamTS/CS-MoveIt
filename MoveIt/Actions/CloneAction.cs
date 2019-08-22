using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

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

        protected Dictionary<Instance, Instance> m_origToClone; // Original -> Clone mapping for updating action queue on undo/redo 
        protected Dictionary<Instance, Instance> m_origToCloneUpdate; // Updated map while processing clone job
        protected Dictionary<ushort, ushort> m_nodeOrigToclone; // Map of node clones to connect cloned segments

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
                    m_states.Add(instance.GetState());
                }
            }
        }

        public static HashSet<Instance> GetCleanSelection(out Vector3 center)
        {
            HashSet<Instance> newSelection = new HashSet<Instance>(selection);

            InstanceID id = new InstanceID();

            // Adding missing nodes
            foreach (Instance instance in Action.selection)
            {
                if (instance.id.Type == InstanceType.NetSegment)
                {
                    ushort segment = instance.id.NetSegment;

                    id.NetNode = segmentBuffer[segment].m_startNode;
                    newSelection.Add(id);

                    id.NetNode = segmentBuffer[segment].m_endNode;
                    newSelection.Add(id);
                }
            }

            // Adding missing segments
            foreach (Instance instance in Action.selection)
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
                Bounds totalBounds = default(Bounds);
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

            return newSelection;
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
            if (MoveItTool.POProcessing)
            {
                return;
            }

            MoveItTool.instance.m_lastInstance = null;
            m_clones = new HashSet<Instance>();
            m_origToCloneUpdate = new Dictionary<Instance, Instance>();
            m_nodeOrigToclone = new Dictionary<ushort, ushort>();

            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            // Clone nodes first
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type == InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToclone, this);
                    if (clone != null)
                    {
                        m_clones.Add(clone);
                        m_origToCloneUpdate.Add(state.instance.id, clone.id);
                        m_nodeOrigToclone.Add(state.instance.id.NetNode, clone.id.NetNode);
                    }
                }
            }

            // Clone everything else
            foreach (InstanceState state in m_states)
            {
                if (state.instance.id.Type != InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, m_nodeOrigToclone, this);
                    // Cloned PO returns null, because it is delayed
                    if (clone != null)
                    {
                        //if (state.instance.id.Type == InstanceType.Building)
                        //{
                        //    MoveableBuilding mb = (MoveableBuilding)state.instance;
                        //    Building b = (Building)mb.data;
                        //    Building c = (Building)clone.data;
                        //    InstanceID i1 = default(InstanceID);
                        //    i1.NetNode = b.m_netNode;
                        //    InstanceID i2 = default(InstanceID);
                        //    i2.NetNode = c.m_netNode;
                        //    Debug.Log($"SUBBUILDINGS\n{mb.id.Building}:{b.m_netNode} ({i1})\n{clone.id.Building}:{c.m_netNode} ({i2})");
                        //}
                        m_clones.Add(clone);
                        m_origToCloneUpdate.Add(state.instance.id, clone.id);
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
            //if (!_isImport)
            //{
            //    MoveItTool.m_debugPanel.Update();
            //}

            UpdateArea(GetTotalBounds(false));
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
            MoveItTool.m_debugPanel.Update();

            UpdateArea(bounds);
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
            Matrix4x4 matrix4x = default(Matrix4x4);
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

        public IEnumerable<Instance> cloneInstances
        {
            get
            {
                foreach (InstanceState state in m_states)
                {
                    yield return state.instance;
                }
            }
        }

        public int Count
        {
            get { return m_states.Count; }
        }
    }
}
