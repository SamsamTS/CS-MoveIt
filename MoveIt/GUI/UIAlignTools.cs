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


        public static void AlignToolsClicked(UIComponent c, UIMouseEventParameter p)
        {
            MoveItTool MIT = MoveItTool.instance;

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
                    if (MIT.alignMode == MoveItTool.AlignModes.Height)
                    {
                        MIT.alignMode = MoveItTool.AlignModes.Off;
                        MIT.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        MIT.StartAligning(MoveItTool.AlignModes.Height);
                        if (MIT.toolState == MoveItTool.ToolState.Aligning)
                        { // Change MITE's mode only if MoveIt changed to AligningHeights
                            MIT.alignMode = MoveItTool.AlignModes.Height;
                        }
                    }
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "MoveIt_AlignIndividualBtn":
                    if (MIT.alignMode == MoveItTool.AlignModes.Individual)
                    {
                        MIT.alignMode = MoveItTool.AlignModes.Off;
                        MIT.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        if (MIT.toolState == MoveItTool.ToolState.Cloning || MIT.toolState == MoveItTool.ToolState.RightDraggingClone)
                        {
                            MIT.StopCloning();
                        }

                        if (Action.selection.Count > 0)
                        {
                            MIT.toolState = MoveItTool.ToolState.Aligning;
                            MIT.alignMode = MoveItTool.AlignModes.Individual;
                        }
                    }
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "MoveIt_AlignGroupBtn":
                    if (MIT.alignMode == MoveItTool.AlignModes.Group)
                    {
                        MIT.alignMode = MoveItTool.AlignModes.Off;
                        MIT.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        if (MIT.toolState == MoveItTool.ToolState.Cloning || MIT.toolState == MoveItTool.ToolState.RightDraggingClone)
                        {
                            MIT.StopCloning();
                        }
                        MIT.toolState = MoveItTool.ToolState.Aligning;

                        if (Action.selection.Count > 0)
                        {
                            MIT.toolState = MoveItTool.ToolState.Aligning;
                            MIT.alignMode = MoveItTool.AlignModes.Group;
                        }
                    }
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "MoveIt_AlignRandomBtn":
                    MIT.alignMode = MoveItTool.AlignModes.Random;

                    if (MIT.toolState == MoveItTool.ToolState.Cloning || MIT.toolState == MoveItTool.ToolState.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignRotationAction action = new AlignRandomAction();
                    action.followTerrain = MoveItTool.followTerrain;
                    ActionQueue.instance.Push(action);
                    ActionQueue.instance.Do();
                    MoveItTool.instance.DeactivateAlignTool();
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                default:
                    Debug.Log($"Invalid Align Tools call ({c.name})");
                    break;
            }
            //Debug.Log($"{c.name} clicked, mode is {MITE.AlignMode}");
        }


        public static void UpdateAlignTools()
        {
            AlignToolsBtn.atlas = AlignButtons["AlignGroup"].atlas;
            AlignToolsBtn.normalFgSprite = "AlignTools";
            foreach (UIButton btn in AlignButtons.Values)
            {
                btn.normalBgSprite = "OptionBase";
            }

            switch (MoveItTool.instance.alignMode)
            {
                case MoveItTool.AlignModes.Height:
                    if (!AlignToolsPanel.isVisible)
                    {
                        AlignToolsBtn.atlas = AlignButtons["AlignHeight"].atlas;
                        AlignToolsBtn.normalFgSprite = "AlignHeight";
                    }
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["AlignHeight"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Individual:
                    AlignToolsBtn.atlas = AlignButtons["AlignIndividual"].atlas;
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignIndividual";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["AlignIndividual"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Group:
                    AlignToolsBtn.atlas = AlignButtons["AlignGroup"].atlas;
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignGroup";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["AlignGroup"].normalBgSprite = "OptionBaseFocused";
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
