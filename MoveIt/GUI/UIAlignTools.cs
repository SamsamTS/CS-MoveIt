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

                case "MoveIt_AlignTerrainHeightBtn":
                    MIT.AlignMode = MoveItTool.AlignModes.TerrainHeight;

                    if (MIT.ToolState == MoveItTool.ToolStates.Cloning || MIT.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignTerrainHeightAction atha = new AlignTerrainHeightAction();
                    ActionQueue.instance.Push(atha);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    MoveItTool.instance.DeactivateAlignTool();
                    break;

                case "MoveIt_AlignSlopeBtn":
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        MIT.AlignMode = MoveItTool.AlignModes.SlopeNode;

                        if (MIT.ToolState == MoveItTool.ToolStates.Cloning || MIT.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                        {
                            MIT.StopCloning();
                        }

                        AlignSlopeAction asa = new AlignSlopeAction();
                        asa.followTerrain = MoveItTool.followTerrain;
                        asa.IsQuick = true;
                        ActionQueue.instance.Push(asa);
                        ActionQueue.instance.Do();
                        if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                        MIT.DeactivateAlignTool();
                        break;
                    }
                    MIT.ProcessAligning(MoveItTool.AlignModes.Slope);
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
                    if (MoveItTool.autoCloseAlignTools) AlignToolsPanel.isVisible = false;
                    MoveItTool.instance.DeactivateAlignTool();
                    break;

                default:
                    Debug.Log($"Invalid Align Tools call ({c.name})");
                    break;
            }
            //Debug.Log($"{c.name} clicked, mode is {MoveItTool.alignMode}");
        }


        // Updates the UI based on the current state
        public static void UpdateAlignTools()
        {
            AlignToolsBtn.normalFgSprite = "AlignTools";
            AlignButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlope";

            foreach (UIButton btn in AlignButtons.Values)
            {
                btn.normalBgSprite = "OptionBase";
            }

            switch (MIT.AlignMode)
            {
                case MoveItTool.AlignModes.Height:
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignHeight";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignHeightBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.TerrainHeight:
                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignHeight";
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignHeightBtn"].normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.AlignModes.Slope:
                    AlignToolsBtn.normalBgSprite = "OptionBaseFocused";
                    AlignButtons["MoveIt_AlignSlopeBtn"].normalBgSprite = "OptionBaseFocused";

                    if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignSlope";

                    switch (MoveItTool.instance.AlignToolPhase)
                    {
                        case 1:
                            AlignButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlopeA";
                            if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignSlopeA";
                            break;

                        case 2:
                            AlignButtons["MoveIt_AlignSlopeBtn"].normalFgSprite = "AlignSlopeB";
                            if (!AlignToolsPanel.isVisible) AlignToolsBtn.normalFgSprite = "AlignSlopeB";
                            break;
                    }
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

                // TerrainHeight and Random modes are instant, their buttons aren't relevant

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
