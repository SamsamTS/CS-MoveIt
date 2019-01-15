using UnityEngine;

using System.Collections.Generic;

namespace MoveIt
{

    public class CloneAction : Action
    {
        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public bool followTerrain;

        public HashSet<InstanceState> savedStates = new HashSet<InstanceState>();
        private HashSet<Instance> m_clones;
        private HashSet<Instance> m_oldSelection;

        private Dictionary<Instance, Instance> m_clonedOrigin;

        public CloneAction()
        {
            m_oldSelection = selection;

            HashSet<Instance> newSelection = GetCleanSelection(out center);
            if (newSelection.Count == 0) return;

            // Save states
            string msg = $"Selected: {newSelection.Count}\n";
            foreach (Instance instance in newSelection)
            {
                if (instance.isValid)
                {
                    msg += $"{instance.Info.Name}:{instance}\n";
                    savedStates.Add(instance.GetState());
                }
            }
            //Debug.Log(msg);
        }

        public static HashSet<Instance> GetCleanSelection(out Vector3 center)
        {
            HashSet<Instance> newSelection = new HashSet<Instance>(selection);

            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

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
                if (state.instance != null && state.Info != null)
                {
                    savedStates.Add(state);
                }
            }

            center = centerPoint;
        }

        public override void Do()
        {
            m_clones = new HashSet<Instance>();

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            Dictionary<Instance, Instance> clonedOrigin = new Dictionary<Instance, Instance>();
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();

            // Clone nodes first
            foreach (InstanceState state in savedStates)
            {
                if (state.instance.id.Type == InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, clonedNodes);
                    if (clone != null)
                    {
                        m_clones.Add(clone);
                        clonedOrigin.Add(state.instance.id, clone.id);
                        clonedNodes.Add(state.instance.id.NetNode, clone.id.NetNode);
                    }
                }
            }

            // Clone everything else
            foreach (InstanceState state in savedStates)
            {
                if (state.instance.id.Type != InstanceType.NetNode)
                {
                    Instance clone = state.instance.Clone(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain, clonedNodes);
                    if (clone != null)
                    {
                        m_clones.Add(clone);
                        clonedOrigin.Add(state.instance.id, clone.id);
                    }
                }
            }

            if (m_clonedOrigin != null)
            {
                Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();

                foreach (Instance key in m_clonedOrigin.Keys)
                {
                    toReplace.Add(m_clonedOrigin[key], clonedOrigin[key]);
                    DebugUtils.Log("To replace: " + m_clonedOrigin[key].id.RawData + " -> " + clonedOrigin[key].id.RawData);
                }

                ActionQueue.instance.ReplaceInstancesForward(toReplace);
            }

            m_clonedOrigin = clonedOrigin;

            // Select clones
            selection = m_clones;

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

            UpdateArea(bounds);
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in savedStates)
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

            if (m_clonedOrigin != null)
            {
                Dictionary<Instance, Instance> clonedOrigin = new Dictionary<Instance, Instance>();

                foreach (Instance key in m_clonedOrigin.Keys)
                {
                    if (toReplace.ContainsKey(key))
                    {
                        clonedOrigin.Add(toReplace[key], m_clonedOrigin[key]);
                        DebugUtils.Log("CloneAction Replacing: " + key.id.RawData + " -> " + toReplace[key].id.RawData);
                    }
                    else
                    {
                        clonedOrigin.Add(key, m_clonedOrigin[key]);
                    }
                }
                m_clonedOrigin = clonedOrigin;
            }
        }

        public HashSet<InstanceState> CalculateStates(Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in savedStates)
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
                        newState.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newState.position);
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
                foreach (InstanceState state in savedStates)
                {
                    yield return state.instance;
                }
            }
        }

        public int Count
        {
            get { return savedStates.Count; }
        }
    }
}
