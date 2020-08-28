using UnityEngine;
using ColossalFramework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MoveIt
{
    public abstract class Action
    {
        public static HashSet<Instance> selection = new HashSet<Instance>();
        public static bool affectsSegments = true;

        public abstract void Do();
        public abstract void Undo();
        public abstract void ReplaceInstances(Dictionary<Instance, Instance> toReplace);

        protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
        protected static PropInstance[] propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;
        protected static TreeInstance[] treeBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;

        protected Dictionary<BuildingState, BuildingState> pillarsOriginalToClone = new Dictionary<BuildingState, BuildingState>();
        protected bool PillarsProcessed;

        internal virtual void OnHover() { }

        internal virtual void Overlays(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor) { }

        internal virtual void UpdateNodeIdInSegmentState(ushort oldId, ushort newId) { }

        public static bool IsSegmentSelected(ushort segment)
        {
            if (affectsSegments) return false;

            InstanceID id = InstanceID.Empty;
            id.NetSegment = segment;

            return selection.Contains(id);
        }

        public static Vector3 GetCenter()
        {
            return GetTotalBounds().center;
        }

        public static float GetAngle()
        {
            if (selection.Count() == 0)
            {
                return 0f;
            }
            else if (selection.Count() == 1)
            {
                return selection.First().angle;
            }
            List<float> angles = new List<float>();
            foreach (Instance i in selection.Where(i => i is MoveableBuilding || i is MoveableProc || i is MoveableProp))
            {
                angles.Add((i.angle % (Mathf.PI * 2)) * Mathf.Rad2Deg);
            }
            if (angles.Count() == 0)
            {
                GetExtremeObjects(out Instance a, out Instance b);
                return (Mathf.PI / 2) - (float)GetAngleBetweenPointsRads(a.position, b.position);
            }

            return ModeAngle(angles.ToArray());
        }

        protected static double GetAngleBetweenPointsRads(Vector3 a, Vector3 b)
        {
            return (Math.Atan2(b.x - a.x, b.z - a.z) + (Mathf.PI * 2)) % (Mathf.PI * 2);
        }

        //private static float MeanAngle(float[] angles)
        //{
        //    var x = angles.Sum(a => Mathf.Cos(a * Mathf.PI / 180)) / angles.Length;
        //    var y = angles.Sum(a => Mathf.Sin(a * Mathf.PI / 180)) / angles.Length;
        //    return (Mathf.Atan2(y, x) * 180 / Mathf.PI) * Mathf.Deg2Rad;
        //}

        private static float ModeAngle(float[] angles)
        {
            Dictionary<float, uint> angleCount = new Dictionary<float, uint>();

            foreach (float a in angles)
            {
                if (!angleCount.ContainsKey(a))
                {
                    angleCount[a] = 1;
                }
                else
                {
                    angleCount[a]++;
                }
            }

            float angle = 0f;
            uint max = 0;
            foreach (KeyValuePair<float, uint> pair in angleCount)
            {
                if (pair.Value > max)
                {
                    angle = pair.Key;
                    max = pair.Value;
                }
            }

            return angle * Mathf.Deg2Rad;
        }

        public static void ClearPOFromSelection()
        {
            if (!MoveItTool.PO.Enabled) return;

            HashSet<Instance> toRemove = new HashSet<Instance>();
            foreach (Instance i in selection)
            {
                if (i is MoveableProc)
                {
                    toRemove.Add(i);
                }
            }
            foreach (Instance i in toRemove)
            {
                selection.Remove(i);
            }

            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public static Bounds GetTotalBounds(bool ignoreSegments = true, bool excludeNetworks = false)
        {
            Bounds totalBounds = default;

            bool init = false;

            foreach (Instance instance in selection)
            {
                if (!init)
                {
                    totalBounds = instance.GetBounds(ignoreSegments);
                    init = true;
                }
                else
                {
                    totalBounds.Encapsulate(instance.GetBounds(ignoreSegments));
                }
            }

            return totalBounds;
        }

        public static void UpdateArea(Bounds bounds, bool full = false)
        {
            try
            {
                if (full)
                {
                    TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
                }

                bounds.Expand(32f);
                MoveItTool.instance.areasToUpdate.Add(bounds);
                MoveItTool.instance.areaUpdateCountdown = 60;

                if (full)
                {
                    Singleton<BuildingManager>.instance.ZonesUpdated(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    Singleton<PropManager>.instance.UpdateProps(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    Singleton<TreeManager>.instance.UpdateTrees(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    bounds.Expand(64f);
                    Singleton<ElectricityManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    Singleton<WaterManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    UpdateRender(bounds);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.Log($"EXCEPTION\n{bounds}\n{e}");
            }
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

            RenderManager renderManager = Singleton<RenderManager>.instance;
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

        internal static void GetExtremeObjects(out Instance A, out Instance B)
        {
            List<Instance> inst = new List<Instance>();
            foreach (Instance i in selection)
            {
                if (i is MoveableSegment)
                {
                    continue;
                }
                inst.Add(i);
            }

            if (inst.Count() < 2)
            {
                throw new IndexOutOfRangeException("Less than 2 objects selected");
            }
            A = inst[0];
            B = inst[1];

            float longest = 0;

            for (int i = 0; i < (inst.Count() - 1); i++)
            {
                for (int j = i + 1; j < inst.Count(); j++)
                {
                    float distance = Math.Abs((inst[i].position - inst[j].position).sqrMagnitude);

                    if (distance > longest)
                    {
                        A = inst[i];
                        B = inst[j];
                        longest = distance;
                    }
                }
            }
        }

        protected HashSet<InstanceState> ProcessPillars(HashSet<InstanceState> states, bool makeClone)
        {
            if (!MoveItTool.advancedPillarControl) return states;

            HashSet<ushort> nodesWithAttachments = new HashSet<ushort>();

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            foreach (InstanceState instanceState in states)
            {
                if (instanceState is NodeState ns && ((NetNode)(ns.instance.data)).m_building > 0 && 
                    ((buildingBuffer[((NetNode)(ns.instance.data)).m_building].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden))
                {
                    nodesWithAttachments.Add(ns.instance.id.NetNode);
                    //Debug.Log($"Node {ns.instance.id.NetNode} found");
                }
            }
            HashSet<InstanceState> newStates = new HashSet<InstanceState>(states);
            foreach (InstanceState instanceState in states)
            {
                ushort buildingId = instanceState.instance.id.Building;
                if (instanceState is BuildingState originalState && MoveItTool.m_pillarMap.ContainsKey(buildingId) && MoveItTool.m_pillarMap[buildingId] > 0)
                {
                    ushort nodeId = MoveItTool.m_pillarMap[buildingId];
                    if (nodesWithAttachments.Contains(nodeId)) // The node is also selected
                    {
                        //Debug.Log($"Pillar {buildingId} for selected node {nodeId}");
                        continue;
                    }
                    MoveableBuilding original = (MoveableBuilding)instanceState.instance;
                    buildingBuffer[buildingId].m_flags |= Building.Flags.Hidden;
                    selection.Remove(original);
                    newStates.Remove(originalState);
                    BuildingState cloneState = null;
                    if (makeClone)
                    {
                        MoveableBuilding clone = original.Duplicate();
                        selection.Add(clone);
                        cloneState = (BuildingState)clone.SaveToState();
                        newStates.Add(cloneState);
                        Debug.Log($"Pillar {buildingId} for node {nodeId} duplicated to {clone.id.Building}");
                    }
                    else
                    {
                        Debug.Log($"Pillar {buildingId} for node {nodeId} hidden");
                    }
                    pillarsOriginalToClone.Add(originalState, cloneState);
                    original.isHidden = true;
                }
            }
            if (pillarsOriginalToClone.Count > 0)
            {
                MoveItTool.UpdatePillarMap();
            }
            states = newStates;
            watch.Stop();
            Debug.Log($"Pillars handled in {watch.ElapsedMilliseconds} ms\nSelected nodes:{nodesWithAttachments.Count}, total selection:{states.Count}, dups mapped:{pillarsOriginalToClone.Count}");
            PillarsProcessed = true;

            return states;
        }
    }
}
