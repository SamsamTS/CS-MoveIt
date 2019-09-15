using UnityEngine;
using ColossalFramework;
using System.Collections.Generic;

namespace MoveIt
{
    public abstract class Action
    {
        public static HashSet<Instance> selection = new HashSet<Instance>();

        public abstract void Do();
        public abstract void Undo();
        public abstract void ReplaceInstances(Dictionary<Instance, Instance> toReplace);

        public virtual void UpdateNodeIdInSegmentState(ushort oldId, ushort newId) { }

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

            //MoveItTool.PO.SelectionClear();
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public static Bounds GetTotalBounds(bool ignoreSegments = true, bool excludeNetworks = false)
        {
            Bounds totalBounds = default(Bounds);

            bool init = false;

            foreach (Instance instance in selection)
            {
                if (!excludeNetworks || (instance.id.Building > 0 || instance.id.Prop > 0 || instance.id.NetLane > 0 || instance.id.Tree > 0))
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
            }

            return totalBounds;
        }

        public static void UpdateArea(Bounds bounds, bool updateTerrain = false)
        {
            if (updateTerrain)
            {
                TerrainModify.UpdateArea(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, true, true, false);
            }

            bounds.Expand(64f);
            MoveItTool.instance.areasToUpdate.Add(bounds);
            MoveItTool.instance.areaUpdateCountdown = 40;

            Singleton<BuildingManager>.instance.ZonesUpdated(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            Singleton<PropManager>.instance.UpdateProps(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            Singleton<TreeManager>.instance.UpdateTrees(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

            bounds.Expand(64f);
            Singleton<ElectricityManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            Singleton<WaterManager>.instance.UpdateGrid(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
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
    }
}
