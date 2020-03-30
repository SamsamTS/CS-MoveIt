using ColossalFramework.UI;
using MoveIt;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class UIMoreTools
    {
        public static UIButton MoreToolsBtn;
        public static UIPanel MoreToolsPanel;
        public static Dictionary<string, UIButton> MoreButtons = new Dictionary<string, UIButton>();
        private static MoveItTool MIT = MoveItTool.instance;

        public static void Initialise()
        {
            MoreToolsBtn = null;
            MoreToolsPanel = null;
            MoreButtons = new Dictionary<string, UIButton>();
            MIT = MoveItTool.instance;
        }

        public static void MoreToolsClicked(UIComponent c, UIMouseEventParameter p)
        {
            switch (c.name)
            {
                case "MoveIt_MoreToolsBtn":
                    if (MoreToolsPanel.isVisible)
                    {
                        MoreToolsPanel.isVisible = false;
                    }
                    else
                    {
                        MoreToolsPanel.isVisible = true;
                    }
                    UpdateMoreTools();
                    break;

                case "MoveIt_AlignHeightBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Height);
                    break;

                case "MoveIt_AlignMirrorBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Mirror);
                    break;

                case "MoveIt_AlignTerrainHeightBtn":
                    MIT.AlignMode = MoveItTool.AlignModes.TerrainHeight;

                    if (MIT.ToolState == MoveItTool.ToolStates.Cloning || MIT.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignTerrainHeightAction atha = new AlignTerrainHeightAction();
                    ActionQueue.instance.Push(atha);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MoveItTool.instance.DeactivateTool();
                    break;

                case "MoveIt_AlignSlopeBtn":
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        MIT.ProcessAligning(MoveItTool.AlignModes.Slope);
                        break;
                    }

                    MIT.AlignMode = MoveItTool.AlignModes.SlopeNode;

                    if (MIT.ToolState == MoveItTool.ToolStates.Cloning || MIT.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignSlopeAction asa = new AlignSlopeAction
                    {
                        followTerrain = MoveItTool.followTerrain,
                        mode = AlignSlopeAction.Modes.Auto
                    };
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        asa.mode = AlignSlopeAction.Modes.Quick;
                    }
                    ActionQueue.instance.Push(asa);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_AlignIndividualBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Inplace);
                    break;

                case "MoveIt_AlignGroupBtn":
                    MIT.ProcessAligning(MoveItTool.AlignModes.Group);
                    break;

                case "MoveIt_AlignRandomBtn":
                    MIT.AlignMode = MoveItTool.AlignModes.Random;

                    if (MIT.ToolState == MoveItTool.ToolStates.Cloning || MIT.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignRandomAction ara = new AlignRandomAction();
                    ara.followTerrain = MoveItTool.followTerrain;
                    ActionQueue.instance.Push(ara);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_ConvertToPOBtn":
                    if (MoveItTool.PO.Enabled && MIT.ToolState == MoveItTool.ToolStates.Default)
                    {
                        MoveItTool.PO.StartConvertAction();
                    }
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_ResetObjectBtn":
                    MoveItTool.instance.StartReset();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                default:
                    Debug.Log($"Invalid Tool call ({c.name})");
                    break;
            }
        }


        // Updates the UI based on the current state
        public static void UpdateMoreTools()
        {
            if (MoreToolsBtn == null)
            { // Button isn't created yet
                return;
            }

            MoreToolsBtn.normalFgSprite = "MoreTools";
            MoreButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlope";

            foreach (UIButton btn in MoreButtons.Values)
            {
                btn.normalBgSprite = "OptionBase";
            }

            switch (MIT.AlignMode)
            {
                case MoveItTool.AlignModes.Height:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignHeight";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignHeightBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.TerrainHeight:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignHeight";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignHeightBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Slope:
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignSlopeBtn"].normalBgSprite = "OptionBaseFocused";

                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlope";

                    switch (MoveItTool.instance.AlignToolPhase)
                    {
                        case 1:
                            MoreButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlopeA";
                            if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlopeA";
                            break;

                        case 2:
                            MoreButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlopeB";
                            if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlopeB";
                            break;
                    }
                    break;

                case MoveItTool.AlignModes.Inplace:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignIndividual";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignIndividualBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Group:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignGroup";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignGroupBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Mirror:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignGroup";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    MoreButtons["MoveIt_AlignMirrorBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                // TerrainHeight and Random modes are instant, their buttons aren't relevant

                default:
                    if (MoreToolsPanel.isVisible)
                    {
                        MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    }
                    else
                    {
                        MoreToolsBtn.normalBgSprite = "OptionBase";
                    }
                    break;
            }
        }
    }
}
