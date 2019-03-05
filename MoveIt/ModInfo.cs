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
            get { return "Move It " + version; }
        }

        public string Description
        {
            get { return "Move things"; }
        }

        public const string version = "2.4.0";

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
                UIHelperBase group = helper.AddGroup(Name);
                UIPanel panel = ((UIPanel)((UIHelper)group).self) as UIPanel;

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

                group.AddSpace(15);

                checkBox = (UICheckBox)group.AddCheckbox("Auto-close Align Tools menu", MoveItTool.autoCloseAlignTools.value, (b) =>
                {
                    MoveItTool.autoCloseAlignTools.value = b;
                    if (UIAlignTools.AlignToolsPanel != null)
                    {
                        UIAlignTools.AlignToolsPanel.isVisible = false;
                    }
                });
                checkBox.tooltip = "Check this to close the Align Tools menu after choosing a tool.";

                group.AddSpace(15);

                checkBox = (UICheckBox)group.AddCheckbox("Prefer fast, low-detail moving (hold Shift to temporarily switch)", MoveItTool.fastMove.value, (b) =>
                {
                    MoveItTool.fastMove.value = b;
                });
                checkBox.tooltip = "Helps you position objects when your frame-rate is poor.";

                group.AddSpace(15);

                checkBox = (UICheckBox)group.AddCheckbox("Select pylons and pillars by holding Alt only", MoveItTool.altSelectNodeBuildings.value, (b) =>
                {
                    MoveItTool.altSelectNodeBuildings.value = b;
                });

                group.AddSpace(15);

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

                group.AddSpace(15);

                ((UIPanel)((UIHelper)group).self).gameObject.AddComponent<OptionsKeymappingMain>();

<<<<<<< HEAD
                group.AddSpace(20);
=======
                group.AddSpace(15);
>>>>>>> PO

                UIButton button = (UIButton)group.AddButton("Remove Ghost Nodes", _cleanGhostNodes);
                button.tooltip = "Use this button when in-game to remove ghost nodes (nodes with no segments attached). Note: this will clear Move It's undo history!";

                group.AddSpace(20);

                checkBox = (UICheckBox)group.AddCheckbox("Show Move It debug panel\n", MoveItTool.showDebugPanel.value, (b) =>
                {
                    MoveItTool.showDebugPanel.value = b;
                    if (MoveItTool.debugPanel != null)
                    {
                        MoveItTool.debugPanel.Visible(b);
                    }
                });
                checkBox.name = "MoveIt_DebugPanel";

                UILabel debugLabel = panel.AddUIComponent<UILabel>();
                debugLabel.name = "debugLabel";
                debugLabel.text = "Shows information about the last highlighted object. Slightly decreases\nperformance, do not enable unless you have a specific reason.\n ";
                debugLabel.eventDoubleClick += DebugLabel_eventClick;

                group.AddSpace(5);

                if (!MoveItTool.HidePO)
                {
                    group = helper.AddGroup("Procedural Objects");
                    panel = ((UIPanel)((UIHelper)group).self) as UIPanel;

                    UILabel poLabel = panel.AddUIComponent<UILabel>();
                    poLabel.name = "poLabel";
                    poLabel.text = PO_Manager.getVersion();

                    UILabel poWarning = panel.AddUIComponent<UILabel>();
                    poWarning.name = "poWarning";
                    poWarning.text = "Procedural Objects (PO) support is in beta. At present you can not clone PO objects, \n" +
                        "redo Convert-to-PO actions or undo Bulldoze actions. This means if you delete PO objects \n" +
                        "with Move It, they are immediately PERMANENTLY gone.\n ";

                    checkBox = (UICheckBox)group.AddCheckbox("Limit Move It to only PO objects selected in PO", MoveItTool.POOnlySelectedAreVisible.value, (b) =>
                    {
                        MoveItTool.POOnlySelectedAreVisible.value = b;
                        if (MoveItTool.PO != null)
                        {
                            MoveItTool.PO.ToolEnabled();
                        }
                    });
                    checkBox.tooltip = "If you have a lot of PO objects (250 or more), this is recommended.";

                    checkBox = (UICheckBox)group.AddCheckbox("Highlight unselected visible PO objects", MoveItTool.POHighlightUnselected.value, (b) =>
                    {
                        MoveItTool.POHighlightUnselected.value = b;
                        if (MoveItTool.PO != null)
                        {
                            MoveItTool.PO.ToolEnabled();
                        }
                    });
                    checkBox.tooltip = "Show a faded purple circle around PO objects that aren't selected.";

                    group.AddSpace(15);

                    panel.gameObject.AddComponent<OptionsKeymappingPO>();

                    group.AddSpace(15);
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        private void DebugLabel_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            MoveItTool.HidePO.value = !MoveItTool.HidePO;
            if (MoveItTool.HidePO)
            {
                ((UILabel)component).text = "Shows information about the last highlighted object. Slightly decreases\nperformance, do not enable unless you have a specific reason.\n \n" +
                    "PO Mode is no longer enabled.";
            }
            else
            {
                ((UILabel)component).text = "Shows information about the last highlighted object. Slightly decreases\nperformance, do not enable unless you have a specific reason.\n \n" +
                    "PO Mode enabled! Restart the game to view PO options.";
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
                    //Debug.Log($"#{nodeId}: {node.Info.GetAI()} {node.m_position}\n{node.Info.m_class} ({node.Info.m_class.m_service}.{node.Info.m_class.m_subService})");
                    NetManager.instance.ReleaseNode(nodeId);
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
