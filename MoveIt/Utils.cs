using ColossalFramework.UI;
using ColossalFramework;
using System;

namespace MoveIt
{
    internal class Utils
    {
        //protected static NetManager netManager = Singleton<NetManager>.instance;
        //protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
        //protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
        //protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
        //protected static NetLane[] laneBuffer = Singleton<NetManager>.instance.m_lanes.m_buffer;

        internal static bool isTreeAnarchyEnabled()
        {
            if (!QCommonLib.QCommon.CheckAssembly("tamod", "treeanarchy"))
            {
                Log.Debug($"TreeAnarchy not found");
                return false;
            }

            Log.Debug($"TreeAnarchy found");
            return true;
        }

        internal static bool isTreeSnappingEnabled()
        {
            if (!QCommonLib.QCommon.CheckAssembly("mod", "treesnapping"))
            {
                Log.Debug($"TreeSnapping not found");
                return false;
            }

            Log.Debug($"TreeSnapping found");
            return true;
        }

        internal static void CleanGhostNodes()
        {
            if (!MoveItLoader.IsGameLoaded)
            {
                ExceptionPanel notLoaded = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                notLoaded.SetMessage("Not In-Game", "Use this button when in-game to remove ghost nodes (nodes with no segments attached, which were previously created by Move It)", false);
                return;
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            string message;
            int count = 0;

            for (ushort nodeId = 0; nodeId < Singleton<NetManager>.instance.m_nodes.m_buffer.Length; nodeId++)
            {
                NetNode node = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None) continue;
                if ((node.m_flags & NetNode.Flags.Untouchable) != NetNode.Flags.None) continue;
                bool hasSegments = false;

                for (int i = 0; i < 8; i++)
                {
                    if (node.GetSegment(i) > 0)
                    {
                        hasSegments = true;
                        break;
                    }
                }

                if (!hasSegments)
                {
                    count++;
                    Singleton<NetManager>.instance.ReleaseNode(nodeId);
                }
            }
            if (count > 0)
            {
                ActionQueue.instance.Clear();
                message = $"Removed {count} ghost node{(count == 1 ? "" : "s")}!";
            }
            else
            {
                message = "No ghost nodes found, nothing has been changed.";
            }
            panel.SetMessage("Removing Ghost Nodes", message, false);
        }

        internal static void FixTreeFixedHeightFlag()
        {
            if (!MoveItLoader.IsGameLoaded)
            {
                ExceptionPanel notLoaded = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                notLoaded.SetMessage("Not In-Game", "If Tree Snapping is causing tree heights to be off, click this straight after loading your city to fix the issue.", false);
                return;
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            string message;
            int count = 0, total = 0;

            for (uint treeId = 0; treeId < Singleton<TreeManager>.instance.m_trees.m_buffer.Length; treeId++)
            {
                TreeInstance tree = Singleton<TreeManager>.instance.m_trees.m_buffer[treeId];
                if ((tree.m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.None) continue;
                total++;

                if (tree.FixedHeight) continue;

                // check if it should have FixedHeight flag
                float terrainHeight = TerrainManager.instance.SampleDetailHeight(tree.Position);
                if (tree.Position.y < terrainHeight - 0.075f || tree.Position.y > terrainHeight + 0.075f)
                {
                    Singleton<TreeManager>.instance.m_trees.m_buffer[treeId].FixedHeight = true;
                    count++;
                }

                if (treeId > 100_000_000)
                {
                    throw new Exception("Scanning too many trees, aborting");
                }
            }
            if (count > 0)
            {
                ActionQueue.instance.Clear();
                message = $"Adjusted {count} tree{(count == 1 ? "" : "s")} out of {total}!";
            }
            else
            {
                message = $"No unflagged floating trees out of {total}, nothing has been changed.";
            }
            panel.SetMessage("Flagging Trees", message, false);
        }
    }
}
