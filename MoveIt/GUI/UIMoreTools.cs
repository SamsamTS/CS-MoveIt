using ColossalFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    class UIMoreToolsBtn
    {
        internal UIPanel m_panel;
        internal UIPanel m_subpanel;
        internal UIToolOptionPanel m_toolbar;
        internal UIButton m_button;
        internal UIPanel m_container;
        internal string m_fgSprite;

        public UIMoreToolsBtn(UIToolOptionPanel parent, string btnName, string tooltip, string fgSprite, UIPanel container, string subName, float entries)
        {
            m_panel = parent.AddUIComponent(typeof(UIPanel)) as UIPanel;
            m_subpanel = m_panel.AddUIComponent(typeof(UIPanel)) as UIPanel;
            m_toolbar = parent;
            m_container = container;
            m_fgSprite = fgSprite;

            UIMoreTools.MoreButtons.Add(btnName, this);
            UIMoreTools.MoreSubButtons.Add(this, new Dictionary<string, UIButton>());

            m_button = container.AddUIComponent<UIButton>();
            m_button.name = btnName;
            m_button.atlas = m_toolbar.GetIconsAtlas();
            m_button.tooltip = tooltip;
            m_button.playAudioEvents = true;
            m_button.size = new Vector2(36, 36);
            m_button.normalBgSprite = "OptionBase";
            m_button.hoveredBgSprite = "OptionBaseHovered";
            m_button.pressedBgSprite = "OptionBasePressed";
            m_button.disabledBgSprite = "OptionBaseDisabled";
            m_button.normalFgSprite = fgSprite;
            m_button.eventMouseEnter += (UIComponent c, UIMouseEventParameter p) =>
            {
                MouseOverToggle(this, true);
            };
            m_button.eventMouseLeave += (UIComponent c, UIMouseEventParameter p) =>
            {
                MouseOverToggle(this, false);
            };

            CreateSubPanel(subName, entries);
        }

        private static void MouseOverToggle(UIMoreToolsBtn button, bool visible)
        {
            button.m_panel.isVisible = visible;
        }

        internal void CreateSubPanel(string subName, float entries)
        {
            m_panel.atlas = UIUtils.GetAtlas("Ingame");
            m_panel.backgroundSprite = "SubcategoriesPanel";
            m_panel.clipChildren = true;
            m_panel.size = new Vector2(160, 12 + (entries * 32f));
            m_panel.isVisible = false;
            m_subpanel.name = subName;
            m_subpanel.padding = new RectOffset(1, 1, 6, 6);
            m_subpanel.size = m_panel.size;
            m_subpanel.autoLayoutDirection = LayoutDirection.Vertical;
            m_subpanel.autoLayout = true;
            m_subpanel.relativePosition = new Vector3(0, 0, 0);
            m_subpanel.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            m_panel.autoLayout = false;
            m_panel.absolutePosition = UIMoreTools.MoreToolsBtn.absolutePosition + new Vector3(-m_panel.width, -m_panel.height - 10);
            m_panel.eventMouseEnter += (UIComponent c, UIMouseEventParameter p) =>
            {
                UIMoreTools.m_activeDisplayMenu = this;
                if (MoveItTool.marqueeSelection)
                {
                    UIToolOptionPanel.instance.m_filtersPanel.isVisible = false;
                }
                c.isVisible = true;
                UIMoreTools.UpdateMoreTools();
            };
            m_panel.eventMouseLeave += (UIComponent c, UIMouseEventParameter p) =>
            {
                UIMoreTools.m_activeDisplayMenu = null;
                if (MoveItTool.marqueeSelection)
                {
                    UIToolOptionPanel.instance.m_filtersPanel.isVisible = true;
                }
                c.isVisible = false;
                UIMoreTools.UpdateMoreTools();
            };
        }

        internal UIButton CreateSubButton(string name, string text, string fgSprite)
        {
            UIMoreTools.MoreSubButtons[this].Add(name, m_subpanel.AddUIComponent<UIButton>());
            UIButton subButton = UIMoreTools.MoreSubButtons[this][name];
            subButton.name = name;
            subButton.atlas = m_toolbar.GetIconsAtlas();
            subButton.playAudioEvents = true;
            subButton.size = new Vector2(158, 30);
            subButton.text = text;
            subButton.textHorizontalAlignment = UIHorizontalAlignment.Left;
            subButton.textScale = 0.8f;
            subButton.textPadding = new RectOffset(29, 0, 5, 0);
            subButton.textColor = new Color32(230, 230, 230, 255);
            subButton.hoveredTextColor = new Color32(255, 255, 255, 255);
            subButton.normalFgSprite = fgSprite;
            subButton.hoveredBgSprite = "SubmenuBG";
            subButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            subButton.spritePadding = new RectOffset(0, 64, 0, 0);
            subButton.eventClicked += (UIComponent c, UIMouseEventParameter p) =>
            {
                UIMoreTools.MoreToolsClicked(subButton.name, Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            };

            return subButton;
        }

        internal UIPanel CreateSubSeparator(string name)
        {
            UIPanel separator = m_subpanel.AddUIComponent<UIPanel>();
            separator.name = name;
            separator.atlas = m_toolbar.GetIconsAtlas();
            separator.size = new Vector2(158, 7);
            separator.backgroundSprite = "SubmenuSeparator";

            return separator;
        }
    }

    class UIMoreTools
    {
        public static UIButton MoreToolsBtn;
        public static UIMoreToolsBtn m_activeDisplayMenu; // Currently displayed submenu's menu button
        public static UIMoreToolsBtn m_activeToolMenu; // Currently selected tool's menu button
        public static UIPanel MoreToolsPanel;
        public static Dictionary<string, UIMoreToolsBtn> MoreButtons = new Dictionary<string, UIMoreToolsBtn>();
        public static Dictionary<UIMoreToolsBtn, Dictionary<string, UIButton>> MoreSubButtons = new Dictionary<UIMoreToolsBtn, Dictionary<string, UIButton>>();
        private static MoveItTool MIT = MoveItTool.instance;

        public static void Initialise()
        {
            MoreToolsBtn = null;
            MoreToolsPanel = null;
            MoreButtons = new Dictionary<string, UIMoreToolsBtn>();
            MoreSubButtons = new Dictionary<UIMoreToolsBtn, Dictionary<string, UIButton>>();
            MIT = MoveItTool.instance;
        }

        public static void MoreToolsClicked(string name, bool simAlt = false, bool simShift = false)
        {
            MoveItTool.instance.DeactivateTool();

            switch (name)
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

                case "MoveIt_LoadBtn":
                    UILoadWindow.Open();
                    break;

                case "MoveIt_SaveBtn":
                    if (MoveItTool.IsExportSelectionValid())
                    {
                        UISaveWindow.Open();
                    }
                    else
                    {
                        UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Selection invalid", "The selection is empty or invalid.", false);
                    }
                    break;

                case "MoveIt_AlignHeightBtn":
                    m_activeToolMenu = MoreButtons["MoveIt_HeightBtn"];
                    MIT.ProcessAligning(MoveItTool.MT_Tools.Height);
                    break;

                case "MoveIt_AlignMirrorBtn":
                    m_activeToolMenu = MoreButtons["MoveIt_OthersBtn"];
                    MIT.ProcessAligning(MoveItTool.MT_Tools.Mirror);
                    break;

                case "MoveIt_AlignTerrainHeightBtn":
                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning || MoveItTool.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignTerrainHeightAction atha = new AlignTerrainHeightAction();
                    ActionQueue.instance.Push(atha);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_AlignSlopeBtn":
                    if (simShift)
                    {
                        m_activeToolMenu = MoreButtons["MoveIt_HeightBtn"];
                        MIT.ProcessAligning(MoveItTool.MT_Tools.Slope);
                        break;
                    }

                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning || MoveItTool.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignSlopeAction asa = new AlignSlopeAction
                    {
                        followTerrain = MoveItTool.followTerrain,
                        mode = AlignSlopeAction.Modes.Auto
                    };
                    if (simAlt)
                    {
                        asa.mode = AlignSlopeAction.Modes.Quick;
                    }
                    ActionQueue.instance.Push(asa);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_AlignLineBtn":
                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning || MoveItTool.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    LineAction la = new LineAction
                    {
                        followTerrain = MoveItTool.followTerrain,
                    };
                    ActionQueue.instance.Push(la);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_AlignIndividualBtn":
                    m_activeToolMenu = MoreButtons["MoveIt_RotateBtn"];
                    MIT.ProcessAligning(MoveItTool.MT_Tools.Inplace);
                    break;

                case "MoveIt_AlignGroupBtn":
                    m_activeToolMenu = MoreButtons["MoveIt_RotateBtn"];
                    MIT.ProcessAligning(MoveItTool.MT_Tools.Group);
                    break;

                case "MoveIt_AlignRandomBtn":
                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning || MoveItTool.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                    {
                        MIT.StopCloning();
                    }

                    AlignRandomAction ara = new AlignRandomAction
                    {
                        followTerrain = MoveItTool.followTerrain
                    };
                    ActionQueue.instance.Push(ara);
                    ActionQueue.instance.Do();
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    MIT.DeactivateTool();
                    break;

                case "MoveIt_ConvertToPOBtn":
                    if (MoveItTool.PO.Enabled && MoveItTool.ToolState == MoveItTool.ToolStates.Default)
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

                case "MoveIt_MoveToBtn":
                    m_activeToolMenu = MoreButtons["MoveIt_OthersBtn"];
                    if (!MoveItTool.instance.StartTool(MoveItTool.ToolStates.ToolActive, MoveItTool.MT_Tools.MoveTo))
                    {
                        m_activeToolMenu = null;
                        break;
                    }

                    MoveToAction mta = new MoveToAction
                    {
                        followTerrain = MoveItTool.followTerrain
                    };
                    ActionQueue.instance.Push(mta);

                    MoveItTool.m_moveToPanel.Visible(true);
                    if (MoveItTool.autoCloseAlignTools) MoreToolsPanel.isVisible = false;
                    break;

                default:
                    Debug.Log($"Invalid Tool clicked ({name})");
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

            foreach (UIMoreToolsBtn btn in MoreSubButtons.Keys)
            {
                btn.m_button.normalBgSprite = "OptionBase";
                btn.m_button.normalFgSprite = btn.m_fgSprite;
            }
            if (m_activeDisplayMenu != null)
            {
                m_activeDisplayMenu.m_button.normalBgSprite = "OptionBaseFocused";
            }
            else if (m_activeToolMenu != null)
            {
                m_activeToolMenu.m_button.normalBgSprite = "OptionBaseFocused";
            }

            switch (MoveItTool.MT_Tool)
            { // Cases only apply to tools that require further interaction
                case MoveItTool.MT_Tools.Height:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignHeight";
                    else m_activeToolMenu.m_button.normalFgSprite = "AlignHeight";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.MT_Tools.Slope:
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlope";
                    else m_activeToolMenu.m_button.normalFgSprite = "AlignSlope";

                    switch (MoveItTool.AlignToolPhase)
                    {
                        case 1:
                            if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlopeA";
                            else m_activeToolMenu.m_button.normalFgSprite = "AlignSlopeA";
                            break;

                        case 2:
                            if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignSlopeB";
                            else m_activeToolMenu.m_button.normalFgSprite = "AlignSlopeB";
                            break;
                    }
                    break;

                case MoveItTool.MT_Tools.Inplace:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignIndividualActive";
                    else m_activeToolMenu.m_button.normalFgSprite = "AlignIndividualActive";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.MT_Tools.Group:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignGroupActive";
                    else m_activeToolMenu.m_button.normalFgSprite = "AlignGroupActive";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.MT_Tools.Mirror:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "AlignMirror";
                    else m_activeToolMenu.m_button.normalFgSprite = "AlignMirror";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    break;

                case MoveItTool.MT_Tools.MoveTo:
                    if (!MoreToolsPanel.isVisible) MoreToolsBtn.normalFgSprite = "MoveToActive";
                    else m_activeToolMenu.m_button.normalFgSprite = "MoveToActive";
                    MoreToolsBtn.normalBgSprite = "OptionBaseFocused";
                    break;

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
