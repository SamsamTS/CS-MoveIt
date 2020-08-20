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
    }
}
