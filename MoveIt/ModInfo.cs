using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using System;
using System.IO;
using UnityEngine;

namespace MoveIt
{
    public class ModInfo : IUserMod
    {
        public ModInfo()
        {
            try
            {
                // Creating setting file
                if (GameSettings.FindSettingsFileByName(MoveItTool.settingsFileName) == null)
                {
                    GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = MoveItTool.settingsFileName } });
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("Could load/create the setting file.");
                DebugUtils.LogException(e);
            }
        }

        public string Name
        {
            get { return "Move It! " + version; }
        }

        public string Description
        {
            get { return "[Alpha Prodecural Objects Option]"; }
        }

        public const string version = "2.4.0 [APOO]";

        private static bool debugInitialised = false;
        public static readonly string debugPath = Path.Combine(DataLocation.localApplicationData, "MoveIt.log");

        public static void DebugLine(string line, bool newLine = true)
        {
            if (!debugInitialised)
            {
                File.WriteAllText(debugPath, $"Move It debug log:\n");
                debugInitialised = true;
            }

            Debug.Log(line);
            File.AppendAllText(debugPath, line);
            if (newLine)
            {
                File.AppendAllText(debugPath, "\n");
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                UIHelper group = helper.AddGroup(Name) as UIHelper;
                UIPanel panel = group.self as UIPanel;

                UICheckBox checkBox = (UICheckBox)group.AddCheckbox("Disable debug messages logging", DebugUtils.hideDebugMessages.value, (b) =>
                {
                    DebugUtils.hideDebugMessages.value = b;
                });
                checkBox.tooltip = "If checked, debug messages won't be logged.";
                checkBox = (UICheckBox)group.AddCheckbox("Hide tips", MoveItTool.hideTips.value, (b) =>
                {
                    MoveItTool.hideTips.value = b;
                    if (UITipsWindow.instance != null)
                    {
                        UITipsWindow.instance.isVisible = false;
                    }
                });
                checkBox.tooltip = "Check this if you don't want to see the tips.";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Auto-close Align Tools menu", MoveItTool.autoCloseAlignTools.value, (b) =>
                {
                    MoveItTool.autoCloseAlignTools.value = b;
                    if (UIAlignTools.AlignToolsPanel != null)
                    {
                        UIAlignTools.AlignToolsPanel.isVisible = false;
                    }
                });
                checkBox.tooltip = "Check this to close the Align Tools menu after choosing a tool.";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Only see PO objects that are selected in PO", MoveItTool.POOnlySelectedAreVisible.value, (b) =>
                {
                    MoveItTool.POOnlySelectedAreVisible.value = b;
                    MoveItTool.PO.ToolEnabled();
                });
                checkBox.tooltip = "If you have a lot (250 or more) of PO objects, this is recommended.";

                checkBox = (UICheckBox)group.AddCheckbox("Highlight unselected visible PO objects", MoveItTool.POHighlightUnselected.value, (b) =>
                {
                    MoveItTool.POHighlightUnselected.value = b;
                    MoveItTool.PO.ToolEnabled();
                });
                checkBox.tooltip = "Show a faded purple circle around PO objects that aren't selected.";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Select pylons and pillars by holding Alt only", MoveItTool.altSelectNodeBuildings.value, (b) =>
                {
                    MoveItTool.altSelectNodeBuildings.value = b;
                });
                //checkBox = (UICheckBox)group.AddCheckbox("Alt+Click on segment to select nodes", MoveItTool.altSelectSegmentNodes.value, (b) =>
                //{
                //    MoveItTool.altSelectSegmentNodes.value = b;
                //});

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Filter as surface: [RWB] FxUK's Brushes", MoveItTool.brushesAsSurfaces.value, (b) =>
                {
                    MoveItTool.brushesAsSurfaces.value = b;
                });
                checkBox = (UICheckBox)group.AddCheckbox("Filter as surface: Extras", MoveItTool.extraAsSurfaces.value, (b) =>
                {
                    MoveItTool.extraAsSurfaces.value = b;
                });
                checkBox.tooltip = "Ploppable Asphalt Decals, Ronyx69's Docks, Deczaah's Surfaces";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Use cardinal movements", MoveItTool.useCardinalMoves.value, (b) =>
                {
                    MoveItTool.useCardinalMoves.value = b;
                });
                checkBox.tooltip = "If checked, Up will move in the North direction, Down is South, Left is West, Right is East.";

                checkBox = (UICheckBox)group.AddCheckbox("Right click cancels cloning", MoveItTool.rmbCancelsCloning.value, (b) =>
                {
                    MoveItTool.rmbCancelsCloning.value = b;
                });
                checkBox.tooltip = "If checked, Right click will cancel cloning instead of rotating 45°.";

                group.AddSpace(10);

                panel.gameObject.AddComponent<OptionsKeymapping>();

                group.AddSpace(10);

                UIButton button = (UIButton)group.AddButton("Remove Ghost Nodes", _cleanGhostNodes);
                button.tooltip = "Use this button when in-game to remove ghost nodes (nodes with no segments attached). Note: this will clear Move It's undo history!";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Show Move It debug panel\n(Affects performance, do not enable unless you have a specific reason)", MoveItTool.showDebugPanel.value, (b) =>
                {
                    MoveItTool.showDebugPanel.value = b;
                    if (MoveItTool.debugPanel != null)
                    {
                        MoveItTool.debugPanel.Visible(b);
                    }
                });
                checkBox.name = "MoveIt_DebugPanel";

                group.AddSpace(10);
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        private void _cleanGhostNodes()
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

            for (ushort nodeId = 0; nodeId < NetManager.instance.m_nodes.m_buffer.Length; nodeId++)
            {
                NetNode node = NetManager.instance.m_nodes.m_buffer[nodeId];
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
                    Debug.Log($"#{nodeId}: {node.Info.GetAI()} {node.m_position}\n{node.Info.m_class} ({node.Info.m_class.m_service}.{node.Info.m_class.m_subService})");
                    //NetManager.instance.ReleaseNode(nodeId);
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
    }
}
