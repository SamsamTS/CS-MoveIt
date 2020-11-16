using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using MoveIt.Localization;
using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            get { return Str.mod_description; }
        }

        internal static CultureInfo Culture => new CultureInfo(SingletonLite<LocaleManager>.instance.language == "zh" ? "zh-cn" : SingletonLite<LocaleManager>.instance.language);

        public const string version = "2.9.1 Unstable";

        private static bool debugInitialised = false;
        public static readonly string debugPath = Path.Combine(DataLocation.localApplicationData, "MoveIt.log");

        public static void DebugLine(string line, bool newLine = true)
        {
            if (!debugInitialised)
            {
                File.WriteAllText(debugPath, $"Move It debug log:\n");
                debugInitialised = true;
            }

            Log.Debug(line);
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
                LocaleManager.eventLocaleChanged -= MoveItLoader.LocaleChanged;
                MoveItLoader.LocaleChanged();
                LocaleManager.eventLocaleChanged += MoveItLoader.LocaleChanged;

                UIHelperBase group = helper.AddGroup(Name);
                UIPanel panel = ((UIHelper)group).self as UIPanel;

                UICheckBox checkBox = (UICheckBox)group.AddCheckbox(Str.options_AutoCloseToolbox, MoveItTool.autoCloseAlignTools.value, (b) =>
                {
                    MoveItTool.autoCloseAlignTools.value = b;
                    if (UIMoreTools.MoreToolsPanel != null)
                    {
                        UIMoreTools.CloseMenu();
                    }
                });
                checkBox.tooltip = Str.options_AutoCloseToolbox_Tooltip;

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_PreferFastmove, MoveItTool.fastMove.value, (b) =>
                {
                    MoveItTool.fastMove.value = b;
                });
                checkBox.tooltip = Str.options_PreferFastmove_Tooltip;

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_UseCompass, MoveItTool.useCardinalMoves.value, (b) =>
                {
                    MoveItTool.useCardinalMoves.value = b;
                });
                checkBox.tooltip = Str.options_UseCompass_Tooltip;

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_RightClickCancel, MoveItTool.rmbCancelsCloning.value, (b) =>
                {
                    MoveItTool.rmbCancelsCloning.value = b;
                });
                checkBox.tooltip = Str.options_RightClickCancel_Tooltip;

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_AdvancedPillarControl, MoveItTool.advancedPillarControl.value, (b) =>
                {
                    MoveItTool.advancedPillarControl.value = b;
                });
                checkBox.tooltip = Str.options_AdvancedPillarControl_Tooltip;

                group.AddSpace(10);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_AltForPillars, MoveItTool.altSelectNodeBuildings.value, (b) =>
                {
                    MoveItTool.altSelectNodeBuildings.value = b;
                });

                group.AddSpace(10);
                group = helper.AddGroup(Str.options_ShortcutsGeneral);
                panel = ((UIHelper)group).self as UIPanel;
                group.AddSpace(10);

                ((UIPanel)((UIHelper)group).self).gameObject.AddComponent<OptionsKeymappingMain>();

                group.AddSpace(10);
                group = helper.AddGroup(Str.options_ShortcutsToolbox);
                panel = ((UIHelper)group).self as UIPanel;
                group.AddSpace(10);

                ((UIPanel)((UIHelper)group).self).gameObject.AddComponent<OptionsKeymappingToolbox>();

                group.AddSpace(10);
                group = helper.AddGroup(Str.options_ExtraOptions);
                panel = ((UIHelper)group).self as UIPanel;
                group.AddSpace(10);

                UIButton button = (UIButton)group.AddButton(Str.options_RemoveGhostNodes, MoveItTool.CleanGhostNodes);
                button.tooltip = Str.options_RemoveGhostNodes_Tooltip;

                group.AddSpace(10);

                button = (UIButton)group.AddButton(Str.options_ResetButtonPosition, () =>
                {
                    UIMoveItButton.savedX.value = -1000;
                    UIMoveItButton.savedY.value = -1000;
                    MoveItTool.instance?.m_button?.ResetPosition();
                });

                group.AddSpace(20);

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_DisableDebugLogging, DebugUtils.hideDebugMessages.value, (b) =>
                {
                    DebugUtils.hideDebugMessages.value = b;
                });
                checkBox.tooltip = Str.options_DisableDebugLogging_Tooltip;

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_ShowDebugPanel, MoveItTool.showDebugPanel.value, (b) =>
                {
                    MoveItTool.showDebugPanel.value = b;
                    if (MoveItTool.m_debugPanel != null)
                    {
                        MoveItTool.m_debugPanel.Visible(b);
                    }
                });
                checkBox.name = "MoveIt_DebugPanel";

                group.AddSpace(5);
                UILabel nsLabel = panel.AddUIComponent<UILabel>();
                nsLabel.name = "nsLabel";
                nsLabel.text = NS_Manager.getVersionText();

                group = helper.AddGroup(Str.options_ProceduralObjects);
                panel = ((UIHelper)group).self as UIPanel;

                UILabel poLabel = panel.AddUIComponent<UILabel>();
                poLabel.name = "poLabel";
                poLabel.text = PO_Manager.getVersionText();

                // TODO add users of MoveITIntegration.dll here by name/description

                UILabel poWarning = panel.AddUIComponent<UILabel>();
                poWarning.name = "poWarning";
                poWarning.padding = new RectOffset(25, 0, 0, 15);
                poWarning.text = Str.options_PODeleteWarning;

                checkBox = (UICheckBox)group.AddCheckbox(Str.options_HidePODeletionWarning, !MoveItTool.POShowDeleteWarning.value, (b) =>
                {
                    MoveItTool.POShowDeleteWarning.value = !b;
                });

                group.AddSpace(15);

                panel.gameObject.AddComponent<OptionsKeymappingPO>();

                group.AddSpace(15);
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        internal static bool InGame() => SceneManager.GetActiveScene().name == "Game";

        public void OnEnabled()
        {
            if (InGame())
            {
                // basic ingame hot reload
                MoveItLoader.loadMode = LoadMode.NewGame;
                MoveItLoader.InstallMod();
            }
        }

        public void OnDisabled()
        {
            if (InGame())
            {
                // basic in game hot unload
                MoveItLoader.UninstallMod();
            }
        }
    }

}
