using UnityEngine;

using System.Collections.Generic;

namespace MoveIt
{
    public abstract class Action
    {
        public static HashSet<Instance> selection = new HashSet<Instance>();

        public abstract void Do();
        public abstract void Undo();

        public abstract void ReplaceInstances(Dictionary<Instance, Instance> toReplace);

        public static bool IsSegmentSelected(ushort segment)
        {
            InstanceID id = InstanceID.Empty;
            id.NetSegment = segment;

            return selection.Contains(id);
        }

        public static Vector3 GetCenter()
        {
            return GetTotalBounds().center;
        }

        public static Bounds GetTotalBounds(bool ignoreSegments = true)
        {
            Bounds totalBounds = default(Bounds);

            bool init = false;

            foreach (Instance instance in Action.selection)
            {
                if (init)
                {
                    totalBounds.Encapsulate(instance.GetBounds(ignoreSegments));
                }
                else
                {
                    totalBounds = instance.GetBounds(ignoreSegments);
                    init = true;
                }
            }

            return totalBounds;
        }

        public static void UpdateArea(Bounds bounds)
        {
            bounds.Expand(64f);

            BuildingManager.instance.ZonesUpdated(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            //ZoneManager.instance.UpdateBlocks(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            PropManager.instance.UpdateProps(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            TreeManager.instance.UpdateTrees(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            VehicleManager.instance.UpdateParkedVehicles(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

            bounds.Expand(64f);

            ElectricityManager.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            WaterManager.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

            TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
            UpdateRender(bounds);
        }

        private static void UpdateRender(Bounds bounds)
        {
            int num1 = Mathf.Clamp((int)(bounds.min.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(bounds.min.z / 64f + 135f), 0, 269);
            int x0 = num1 * 45 / 270 - 1;
            int z0 = num2 * 45 / 270 - 1;

            num1 = Mathf.Clamp((int)(bounds.max.x / 64f + 135f), 0, 269);
            num2 = Mathf.Clamp((int)(bounds.max.z / 64f + 135f), 0, 269);
            int x1 = num1 * 45 / 270 + 1;
            int z1 = num2 * 45 / 270 + 1;

            RenderManager renderManager = RenderManager.instance;
            RenderGroup[] renderGroups = renderManager.m_groups;

            for (int i = z0; i < z1; i++)
            {
                for (int j = x0; j < x1; j++)
                {
                    int n = Mathf.Clamp(i * 45 + j, 0, renderGroups.Length - 1);

                    if (n < 0)
                    {
                        continue;
                    }
                    else if (n >= renderGroups.Length)
                    {
                        break;
                    }

                    if (renderGroups[n] != null)
                    {
                        renderGroups[n].SetAllLayersDirty();
                        renderManager.m_updatedGroups1[n >> 6] |= 1uL << n;
                        renderManager.m_groupsUpdated1 = true;
                    }
                }
            }
        }
    }

    public class SelectAction: Action
    {
        private HashSet<Instance> m_oldSelection;
        private HashSet<Instance> m_newSelection;

        public SelectAction(bool copy = false)
        {
            m_oldSelection = selection;

            if (copy && selection != null)
            {
                m_newSelection = new HashSet<Instance>(selection);
            }
            else
            {
                m_newSelection = new HashSet<Instance>();
            }

            selection = m_newSelection;
        }

        public void Add(Instance instance)
        {
            if(!selection.Contains(instance))
            {
                m_newSelection.Add(instance);
            }
        }

        public void Remove(Instance instance)
        {
            m_newSelection.Remove(instance);
        }

        public override void Do()
        {
            selection = m_newSelection;
        }

        public override void Undo()
        {
            selection = m_oldSelection;
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach(Instance instance in toReplace.Keys)
            {
                if(m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("SelectAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }

                if (m_newSelection.Remove(instance))
                {
                    DebugUtils.Log("SelectAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_newSelection.Add(toReplace[instance]);
                }
            }
        }
    }

    public class TransformAction: Action
    {
        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public bool followTerrain;

        public bool autoCurve;
        public NetSegment segmentCurve;

        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public TransformAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.GetState());
                }
            }

            center = GetCenter();
        }

        public override void Do()
        {
            Bounds bounds = GetTotalBounds(false);

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis(angleDelta * 57.29578f, Vector3.down), Vector3.one);

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    state.instance.Transform(state, ref matrix4x, moveDelta.y, angleDelta, center, followTerrain);

                    if(autoCurve)
                    {
                        MoveableNode node = state.instance as MoveableNode;
                        if(node != null) node.AutoCurve(segmentCurve);
                    }
                }
            }

            UpdateArea(bounds);
            UpdateArea(GetTotalBounds(false));
        }

        public override void Undo()
        {
            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                state.instance.SetState(state);
            }

            UpdateArea(bounds);
            UpdateArea(GetTotalBounds(false));
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("TransformAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }

        public HashSet<InstanceState> CalculateStates(Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * 57.29578f, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    InstanceState newState = new InstanceState();
                    newState.instance = state.instance;
                    newState.info = state.info;

                    newState.position = matrix4x.MultiplyPoint(state.position - center) ;
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
    }

    public class AlignHeightAction : Action
    {
        public float height;

        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public AlignHeightAction()
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
            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    state.instance.SetHeight(height);
                }
            }

            UpdateArea(GetTotalBounds(false));
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
                    DebugUtils.Log("AlignHeightAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }

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
                if(instance.id.Type == InstanceType.NetNode)
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

                    if(!found)
                    {
                        toRemove.Add(instance);
                    }
                }
            }

            newSelection.ExceptWith(toRemove);

            if (newSelection.Count == 0) return;

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

            // Save states
            foreach (Instance instance in newSelection)
            {
                if (instance.isValid)
                {
                    savedStates.Add(instance.GetState());
                }
            }
        }

        public CloneAction(InstanceState[] states)
        {
            m_oldSelection = selection;

            foreach (InstanceState state in states)
            {
                if (state.instance != null)
                {
                    savedStates.Add(state);
                }
            }
        }

        public override void Do()
        {
            m_clones = new HashSet<Instance>();

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis(angleDelta * 57.29578f, Vector3.down), Vector3.one);

            Dictionary<Instance, Instance> clonedOrigin = new Dictionary<Instance, Instance>();
            // Clone nodes first
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();
            
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

            if(m_clonedOrigin != null)
            {
                Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();

                foreach(Instance key in m_clonedOrigin.Keys)
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

            if(m_clonedOrigin != null)
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
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * 57.29578f, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in savedStates)
            {
                if (state.instance.isValid)
                {
                    InstanceState newState = new InstanceState();
                    newState.instance = state.instance;
                    newState.info = state.info;

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
                foreach(InstanceState state in savedStates)
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

        /*public BulldozeAction(InstanceState [] states)
        {
            foreach (InstanceState state in states)
            {
                if (state.instance != null)
                {
                    m_states.Add(state);
                }
            }
        }*/

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
            
            Dictionary<Instance, Instance> toReplace = new Dictionary<Instance,Instance>();
            Dictionary<ushort, ushort> clonedNodes = new Dictionary<ushort, ushort>();

            foreach(InstanceState state in m_states)
            {
                if(state.instance.id.Type == InstanceType.NetNode)
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

            foreach(Instance instance in toReplace.Keys)
            {
                if(m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("BulldozeAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }
            }
        }
    }
}
