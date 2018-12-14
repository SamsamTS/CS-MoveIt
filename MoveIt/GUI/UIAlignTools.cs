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
            switch (c.name)
            {
                case "AlignToolsBtn":
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

                case "AlignHeight":
                    if (MITE.AlignMode == MITE.AlignModes.Height)
                    {
                        MITE.AlignMode = MITE.AlignModes.Off;
                        MoveItTool.instance.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        MoveItTool.instance.StartAligningHeights();
                        if (MoveItTool.instance.toolState == MoveItTool.ToolState.AligningHeights)
                        { // Change MITE's mode only if MoveIt changed to AligningHeights
                            MITE.AlignMode = MITE.AlignModes.Height;
                        }
                    }
                    if (MITE.Settings.AutoCollapseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "AlignIndividual":
                    if (MITE.AlignMode == MITE.AlignModes.Individual)
                    {
                        MITE.AlignMode = MITE.AlignModes.Off;
                        MoveItTool.instance.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        if (MoveItTool.instance.toolState == MoveItTool.ToolState.Cloning || MoveItTool.instance.toolState == MoveItTool.ToolState.RightDraggingClone)
                        {
                            MoveItTool.instance.StopCloning();
                        }
                        MoveItTool.instance.toolState = MoveItTool.ToolState.AligningHeights;

                        if (Action.selection.Count > 0)
                        {
                            MITE.AlignMode = MITE.AlignModes.Individual;
                        }
                    }
                    if (MITE.Settings.AutoCollapseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "AlignGroup":
                    if (MITE.AlignMode == MITE.AlignModes.Group)
                    {
                        MITE.AlignMode = MITE.AlignModes.Off;
                        MoveItTool.instance.toolState = MoveItTool.ToolState.Default;
                    }
                    else
                    {
                        if (MoveItTool.instance.toolState == MoveItTool.ToolState.Cloning || MoveItTool.instance.toolState == MoveItTool.ToolState.RightDraggingClone)
                        {
                            MoveItTool.instance.StopCloning();
                        }
                        MoveItTool.instance.toolState = MoveItTool.ToolState.AligningHeights;

                        if (Action.selection.Count > 0)
                        {
                            MITE.AlignMode = MITE.AlignModes.Group;
                        }
                    }
                    if (MITE.Settings.AutoCollapseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;

                case "AlignRandom":
                    MITE.AlignMode = MITE.AlignModes.Random;

                    if (MoveItTool.instance.toolState == MoveItTool.ToolState.Cloning || MoveItTool.instance.toolState == MoveItTool.ToolState.RightDraggingClone)
                    {
                        MoveItTool.instance.StopCloning();
                    }

                    AlignRotationAction action = new AlignRandomAction();
                    action.followTerrain = MoveItTool.followTerrain;
                    ActionQueue.instance.Push(action);
                    ActionQueue.instance.Do();
                    MITE.DeactivateAlignTool();
                    if (MITE.Settings.AutoCollapseAlignTools) AlignToolsPanel.isVisible = false;
                    UpdateAlignTools();
                    break;
            }
            //Debug.Log($"{c.name} clicked, mode is {MITE.AlignMode}");
        }


        public static void UpdateAlignTools()
        {
            AlignToolsBtn.atlas = AlignButtons.GetValueSafe("AlignGroup").atlas;
            AlignToolsBtn.normalFgSprite = "AlignTools";
            foreach (UIButton btn in AlignButtons.Values)
            {
                btn.normalBgSprite = "OptionBase";
            }

            switch (MITE.AlignMode)
            {
                case MITE.AlignModes.Height:
                    if (!AlignToolsPanel.isVisible)
                    {
                        AlignToolsBtn.atlas = AlignButtons.GetValueSafe("AlignHeight").atlas;
                        AlignToolsBtn.normalFgSprite = "AlignHeight";
                    }
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons.GetValueSafe("AlignHeight").normalBgSprite = "OptionBaseFocused";
                    break;

                case MITE.AlignModes.Individual:
                    AlignToolsBtn.atlas = AlignButtons.GetValueSafe("AlignIndividual").atlas;
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignIndividual";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons.GetValueSafe("AlignIndividual").normalBgSprite = "OptionBaseFocused";
                    break;

                case MITE.AlignModes.Group:
                    AlignToolsBtn.atlas = AlignButtons.GetValueSafe("AlignGroup").atlas;
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignGroup";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons.GetValueSafe("AlignGroup").normalBgSprite = "OptionBaseFocused";
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
