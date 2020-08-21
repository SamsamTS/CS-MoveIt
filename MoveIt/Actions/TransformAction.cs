using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;

namespace MoveIt
{
    public class TransformAction : BaseTransformAction
    {
    }

    public class MoveToAction : BaseTransformAction
    {
        internal Vector3 Original, Position;
        internal float AngleOriginal, Angle;
        internal bool AngleActive, HeightActive;

        public override void Undo()
        {
            MoveItTool.instance.DeactivateTool();
            
            base.Undo();
        }
    }

    public abstract class BaseTransformAction : Action
    {
        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public float snapAngle;
        public bool followTerrain;

        public bool autoCurve;
        public NetSegment segmentCurve;

        protected readonly bool containsNetwork = false;
        protected Dictionary<BuildingState, BuildingState> pillarsCloneToOriginal = new Dictionary<BuildingState, BuildingState>();

        private bool PillarsProcessed;

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        internal bool _virtual = false;
        public bool Virtual
        {
            get => _virtual;
            set
            {
                if (value == true)
                {
                    if (_virtual == false && selection.Count < MoveItTool.Fastmove_Max)
                    {
                        _virtual = true;
                        foreach (Instance i in selection)
                        {
                            i.Virtual = true;
                        }
                    }
                }
                else
                {
                    if (_virtual == true)
                    {
                        _virtual = false;
                        foreach (Instance i in selection)
                        {
                            i.Virtual = false;
                        }
                        Do();
                        UpdateArea(GetTotalBounds(), true);
                    }
                }
            }
        }

        public BaseTransformAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.SaveToState());

                    if (instance is MoveableNode || instance is MoveableSegment)
                    {
                        containsNetwork = true;
                    }
                }
            }

            ProcessPillars();
            center = GetCenter();
        }

        private void ProcessPillars()
        {
            HashSet<ushort> nodesWithAttachments = new HashSet<ushort>();

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            foreach (InstanceState instanceState in m_states)
            {
                if (instanceState is NodeState ns && ((NetNode)(ns.instance.data)).m_building > 0 && 
                    ((buildingBuffer[((NetNode)(ns.instance.data)).m_building].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden))
                {
                    nodesWithAttachments.Add(ns.instance.id.NetNode);
                    Debug.Log($"Node {ns.instance.id.NetNode} found");
                }
            }
            HashSet<InstanceState> newStates = new HashSet<InstanceState>(m_states);
            foreach (InstanceState instanceState in m_states)
            {
                ushort buildingId = instanceState.instance.id.Building;
                if (instanceState is BuildingState originalState && MoveItTool.m_pillarMap.ContainsKey(buildingId) && MoveItTool.m_pillarMap[buildingId] > 0)
                {
                    ushort nodeId = MoveItTool.m_pillarMap[buildingId];
                    if (nodesWithAttachments.Contains(nodeId)) // The node is also selected
                    {
                        Debug.Log($"Pillar {buildingId} for selected node {nodeId}");
                        continue;
                    }
                    MoveableBuilding original = (MoveableBuilding)instanceState.instance;
                    buildingBuffer[buildingId].m_flags |= Building.Flags.Hidden;
                    MoveableBuilding clone = original.Duplicate();
                    BuildingState cloneState = (BuildingState)clone.SaveToState();
                    pillarsCloneToOriginal.Add(cloneState, originalState);
                    Debug.Log($"Pillar {buildingId} for node {nodeId} duplicated to {clone.id.Building}");
                    selection.Remove(original);
                    selection.Add(clone);
                    newStates.Remove(originalState);
                    newStates.Add(cloneState);
                    original.isHidden = true;
                }
            }
            if (pillarsCloneToOriginal.Count > 0)
            {
                MoveItTool.UpdatePillarMap();
            }
            m_states = newStates;
            watch.Stop();
            Debug.Log($"Pillars handled in {watch.ElapsedMilliseconds} ms\nSelected nodes:{nodesWithAttachments.Count}, total selection:{m_states.Count}, dups mapped:{pillarsCloneToOriginal.Count}");
            PillarsProcessed = true;
        }

        public override void Do()
        {
            if (!PillarsProcessed) ProcessPillars();

            Bounds originalBounds = GetTotalBounds(false);

            Matrix4x4 matrix4x = default;
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis((angleDelta + snapAngle) * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid && !(state is SegmentState))
                {
                    state.instance.Transform(state, ref matrix4x, moveDelta.y, angleDelta + snapAngle, center, followTerrain);

                    if (autoCurve && state.instance is MoveableNode node)
                    {
                        node.AutoCurve(segmentCurve);
                    }
                }
            }

            // Move segments after the nodes have moved
            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid && state is SegmentState)
                {
                    state.instance.Transform(state, ref matrix4x, moveDelta.y, angleDelta + snapAngle, center, followTerrain);
                }
            }

            bool full = !(MoveItTool.fastMove != Event.current.shift);
            if (!full)
            {
                full = selection.Count > MoveItTool.Fastmove_Max;
            }
            UpdateArea(originalBounds, full);
            Bounds fullbounds = GetTotalBounds(false);
            UpdateArea(fullbounds, full);
        }

        public override void Undo()
        {
            PillarsProcessed = false;

            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                if (!(state is SegmentState))
                {
                    state.instance.LoadFromState(state);
                }
            }

            foreach (InstanceState state in m_states)
            {
                if (state is SegmentState)
                {
                    state.instance.LoadFromState(state);
                }
            }

            foreach (KeyValuePair<BuildingState, BuildingState> pillarClone in pillarsCloneToOriginal)
            {
                BuildingState cloneState = pillarClone.Key;
                BuildingState originalState = pillarClone.Value;
                cloneState.instance.Delete();
                originalState.instance.isHidden = false;
                buildingBuffer[originalState.instance.id.Building].m_flags &= ~Building.Flags.Hidden;
                selection.Remove(cloneState.instance);
                selection.Add(originalState.instance);
                m_states.Remove(cloneState);
                m_states.Add(originalState);
            }
            if (pillarsCloneToOriginal.Count > 0)
            {
                MoveItTool.UpdatePillarMap();
            }

            UpdateArea(bounds, true);
            UpdateArea(GetTotalBounds(false), true);
        }

        public void InitialiseDrag()
        {
            MoveItTool.dragging = true;
            Virtual = false;

            foreach (InstanceState instanceState in m_states)
            {
                if (instanceState.instance is MoveableBuilding mb)
                {
                    mb.InitialiseDrag();
                }
            }
        }

        public void FinaliseDrag()
        {
            MoveItTool.dragging = false;
            Virtual = false;

            foreach (InstanceState instanceState in m_states)
            {
                if (instanceState.instance is MoveableBuilding mb)
                {
                    mb.FinaliseDrag();
                }
            }
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
}
