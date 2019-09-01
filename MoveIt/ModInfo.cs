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
                DebugUtils.Log("Could not load/create the setting file.");
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

        public const string version = "2.6.0";

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

                UICheckBox checkBox = (UICheckBox)group.AddCheckbox("Hide tips", MoveItTool.hideTips.value, (b) =>
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

                checkBox = (UICheckBox)group.AddCheckbox("Prefer fast, low-detail moving (hold Shift to temporarily switch)", MoveItTool.fastMove.value, (b) =>
                {
                    MoveItTool.fastMove.value = b;
                });
                checkBox.tooltip = "Helps you position objects when your frame-rate is poor.";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Select pylons and pillars by holding Alt only", MoveItTool.altSelectNodeBuildings.value, (b) =>
                {
                    MoveItTool.altSelectNodeBuildings.value = b;
                });

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Use cardinal movements", MoveItTool.useCardinalMoves.value, (b) =>
                {
                    MoveItTool.useCardinalMoves.value = b;
                });
                checkBox.tooltip = "If checked, Up will move in the North direction, Down is South, Left is West, Right is East.";

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox("Right click cancels cloning", MoveItTool.rmbCancelsCloning.value, (b) =>
                {
                    MoveItTool.rmbCancelsCloning.value = b;
                });
                checkBox.tooltip = "If checked, Right click will cancel cloning instead of rotating 45°.";

                group.AddSpace(15);

                ((UIPanel)((UIHelper)group).self).gameObject.AddComponent<OptionsKeymappingMain>();

                group.AddSpace(15);

                UIButton button = (UIButton)group.AddButton("Remove Ghost Nodes", MoveItTool.CleanGhostNodes);
                button.tooltip = "Use this button when in-game to remove ghost nodes (nodes with no segments attached). Note: this will clear Move It's undo history!";

                group.AddSpace(20);

                checkBox = (UICheckBox)group.AddCheckbox("Disable debug messages logging", DebugUtils.hideDebugMessages.value, (b) =>
                {
                    DebugUtils.hideDebugMessages.value = b;
                });
                checkBox.tooltip = "If checked, debug messages won't be logged.";

                checkBox = (UICheckBox)group.AddCheckbox("Show Move It debug panel\n", MoveItTool.showDebugPanel.value, (b) =>
                {
                    MoveItTool.showDebugPanel.value = b;
                    if (MoveItTool.m_debugPanel != null)
                    {
                        MoveItTool.m_debugPanel.Visible(b);
                    }
                });
                checkBox.name = "MoveIt_DebugPanel";

                UILabel debugLabel = panel.AddUIComponent<UILabel>();
                debugLabel.name = "debugLabel";
                debugLabel.text = "Shows information about the last highlighted object. Slightly decreases\nperformance, do not enable unless you have a specific reason.\n ";

                group.AddSpace(5);

                if (!MoveItTool.HidePO)
                {
                    group = helper.AddGroup("Procedural Objects");
                    panel = ((UIPanel)((UIHelper)group).self) as UIPanel;

                    UILabel poLabel = panel.AddUIComponent<UILabel>();
                    poLabel.name = "poLabel";
                    poLabel.text = PO_Manager.getVersionText();

                    UILabel poWarning = panel.AddUIComponent<UILabel>();
                    poWarning.name = "poWarning";
                    poWarning.text = "      Please note, you can not redo Convert-to-PO actions or undo Bulldoze \n" +
                        "      actions. This means if you delete PO objects with Move It, they are \n" +
                        "      immediately PERMANENTLY gone.\n ";

                    checkBox = (UICheckBox)group.AddCheckbox("Hide the PO deletion warning", !MoveItTool.POShowDeleteWarning.value, (b) =>
                    {
                        MoveItTool.POShowDeleteWarning.value = !b;
                    });

                    //checkBox = (UICheckBox)group.AddCheckbox("Limit Move It to only PO objects selected in PO", MoveItTool.POOnlySelectedAreVisible.value, (b) =>
                    //{
                    //    MoveItTool.POOnlySelectedAreVisible.value = b;
                    //    if (MoveItTool.PO != null)
                    //    {
                    //        MoveItTool.PO.ToolEnabled();
                    //    }
                    //});
                    //checkBox.tooltip = "If you have a lot of PO objects (250 or more), this is recommended.";

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
    }
}
