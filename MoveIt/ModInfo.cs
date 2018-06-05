using ICities;

using System;

using ColossalFramework;
using ColossalFramework.UI;

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

        public const string version = "2.1.2";

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

                group.AddSpace(10);

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
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

    }
}
