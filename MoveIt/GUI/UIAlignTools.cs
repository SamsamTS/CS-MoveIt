using ColossalFramework.UI;
using MoveIt;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class UIAlignTools
    {
        public static UIButton AlignToolsBtn;
        public static UIPanel AlignToolsPanel;
        public static Dictionary<string, UIButton> AlignButtons = new Dictionary<string, UIButton>();
        private static MoveItTool MIT = MoveItTool.instance;


        public static void AlignToolsClicked(UIComponent c, UIMouseEventParameter p)
        {

            switch (c.name)
            {
                case "MoveIt_AlignToolsBtn":
                    if (AlignToolsPanel.isVisible)
                    {
                        AlignToolsPanel.isVisible = false;
                    }
                    else
                    {
                        AlignToolsPanel.isVisible = true;
                    }
                    UpdateAlignTools();
                    break;

                case "MoveIt_AlignHeightBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Height);
                    break;

                case "MoveIt_AlignIndividualBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Inplace);
                    break;

                case "MoveIt_AlignGroupBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Group);
                    break;

                case "MoveIt_AlignRandomBtn":
                    MIT.alignMode = MoveItTool.AlignModes.Random;

                    if (MIT.toolState == MoveItTool.ToolState.Cloning || MIT.toolState == MoveItTool.ToolState.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignRandomAction action = new AlignRandomAction();
                    action.followTerrain = MoveItTool.followTerrain;
                    ActionQueue.instance.Push(action);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    MoveItTool.instance.DeactivateAlignTool();
                    break;

                default:
                    Debug.Log($"Invalid Align Tools call ({c.name})");
                    break;
            }
            //Debug.Log($"{c.name} clicked, mode is {MITE.AlignMode}");
        }


        // Updates the UI based on the current state
        public static void UpdateAlignTools()
        {
            AlignToolsBtn.normalFgSprite = "AlignTools";
            foreach (UIButton btn in AlignButtons.Values)
            {
                btn.normalBgSprite = "OptionBase";
            }

            switch (MIT.alignMode)
            {
                case MoveItTool.AlignModes.Height:
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignHeight";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignHeightBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Inplace:
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignIndividual";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignIndividualBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Group:
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignGroup";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignGroupBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                // Random mode is instant, button isn't relevant
                default:
                    if (AlignToolsPanel.isVisible)
                    {
                        AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    }
                    else
                    {
                        AlignToolsBtn.normalBgSprite = "OptionBase";
                    }
                    break;
            }
        }

    }
}
