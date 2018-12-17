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
            get { return "Move things"; }
        }

        public const string version = "2.3.0 [Alpha]";

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

    }
}
