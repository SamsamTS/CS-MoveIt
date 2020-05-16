using UnityEngine;
using ColossalFramework.UI;
using System;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UIToolOptionPanel : UIPanel
    {
        public static UIToolOptionPanel instance;

        private UIMultiStateButton m_followTerrain;
        private UIMultiStateButton m_snapping;

        private UITabstrip m_tabStrip;
        private UIButton m_single;
        private UIButton m_marquee;
        private UIButton m_copy;
        private UIButton m_bulldoze;
        private UIButton m_moreTools;
        public UIButton m_picker;

        public UIMultiStateButton PO_button;
        public UIMultiStateButton grid;
        public UIMultiStateButton underground;

        public UIPanel m_filtersPanel, m_filtersPanelList;
        public UIPanel m_moreToolsPanel;
        public UIPanel m_viewOptions;

        public override void Start()
        {
            UIMoreTools.Initialise();

            instance = this;

            UICheckBox checkBox = null;

            atlas = UIUtils.GetAtlas("Ingame");
            size = new Vector2(41, 41);
            relativePosition = new Vector2(GetUIView().GetScreenResolution().x - 340, -41);
            name = "MoveIt_ToolOptionPanel";

            DebugUtils.Log("ToolOptionPanel position: " + absolutePosition);

            #region Follow Terrain
            m_followTerrain = AddUIComponent<UIMultiStateButton>();
            m_followTerrain.atlas = GetFollowTerrainAtlas();
            m_followTerrain.name = "MoveIt_FollowTerrain";
            m_followTerrain.tooltip = "Follow Terrain";
            m_followTerrain.playAudioEvents = true;

            m_followTerrain.size = new Vector2(36, 36);
            m_followTerrain.spritePadding = new RectOffset();

            m_followTerrain.backgroundSprites[0].disabled = "ToggleBaseDisabled";
            m_followTerrain.backgroundSprites[0].hovered = "ToggleBaseHovered";
            m_followTerrain.backgroundSprites[0].normal = "ToggleBase";
            m_followTerrain.backgroundSprites[0].pressed = "ToggleBasePressed";

            m_followTerrain.backgroundSprites.AddState();
            m_followTerrain.backgroundSprites[1].disabled = "ToggleBaseDisabled";
            m_followTerrain.backgroundSprites[1].hovered = "";
            m_followTerrain.backgroundSprites[1].normal = "ToggleBaseFocused";
            m_followTerrain.backgroundSprites[1].pressed = "ToggleBasePressed";

            m_followTerrain.foregroundSprites[0].normal = "FollowTerrain_disabled";

            m_followTerrain.foregroundSprites.AddState();
            m_followTerrain.foregroundSprites[1].normal = "FollowTerrain";

            m_followTerrain.relativePosition = Vector2.zero;
            
            m_followTerrain.activeStateIndex = MoveItTool.followTerrain ? 1 : 0;

            m_followTerrain.eventClicked += (c, p) =>
            {
                MoveItTool.followTerrain = (m_followTerrain.activeStateIndex == 1);
                MoveItTool.followTerrainModeEnabled.value = (m_followTerrain.activeStateIndex == 1);
            };
            #endregion

            #region Snapping
            m_snapping = AddUIComponent<UIMultiStateButton>();
            m_snapping.atlas = UIUtils.GetAtlas("Ingame");
            m_snapping.name = "MoveIt_Snapping";
            m_snapping.tooltip = "Toggle Snapping";
            m_snapping.playAudioEvents = true;

            m_snapping.size = new Vector2(36, 36);
            m_snapping.spritePadding = new RectOffset();

            m_snapping.backgroundSprites[0].disabled = "ToggleBaseDisabled";
            m_snapping.backgroundSprites[0].hovered = "ToggleBaseHovered";
            m_snapping.backgroundSprites[0].normal = "ToggleBase";
            m_snapping.backgroundSprites[0].pressed = "ToggleBasePressed";

            m_snapping.backgroundSprites.AddState();
            m_snapping.backgroundSprites[1].disabled = "ToggleBaseDisabled";
            m_snapping.backgroundSprites[1].hovered = "";
            m_snapping.backgroundSprites[1].normal = "ToggleBaseFocused";
            m_snapping.backgroundSprites[1].pressed = "ToggleBasePressed";

            m_snapping.foregroundSprites[0].disabled = "SnappingDisabled";
            m_snapping.foregroundSprites[0].hovered = "SnappingHovered";
            m_snapping.foregroundSprites[0].normal = "Snapping";
            m_snapping.foregroundSprites[0].pressed = "SnappingPressed";

            m_snapping.foregroundSprites.AddState();
            m_snapping.foregroundSprites[1].disabled = "SnappingDisabled";
            m_snapping.foregroundSprites[1].hovered = "";
            m_snapping.foregroundSprites[1].normal = "SnappingFocused";
            m_snapping.foregroundSprites[1].pressed = "SnappingPressed";

            m_snapping.relativePosition = m_followTerrain.relativePosition + new Vector3(m_followTerrain.width, 0);

            m_snapping.activeStateIndex = (MoveItTool.instance != null && MoveItTool.instance.snapping) ? 1 : 0;

            m_snapping.eventClicked += (c, p) =>
            {
                if (MoveItTool.instance != null)
                {
                    MoveItTool.instance.snapping = (m_snapping.activeStateIndex == 1);
                }
            };
            #endregion

            m_tabStrip = AddUIComponent<UITabstrip>();
            m_tabStrip.size = new Vector2(36, 72);

            m_tabStrip.relativePosition = m_snapping.relativePosition + new Vector3(m_snapping.width, 0);

            #region Single Select
            m_single = m_tabStrip.AddTab("MoveIt_Single", null, false);
            m_single.group = m_tabStrip;
            m_single.atlas = UIUtils.GetAtlas("Ingame");
            m_single.tooltip = "Single Selection";
            m_single.playAudioEvents = true;

            m_single.size = new Vector2(36, 36);

            m_single.normalBgSprite = "OptionBase";
            m_single.focusedBgSprite = "OptionBaseFocused";
            m_single.hoveredBgSprite = "OptionBaseHovered";
            m_single.pressedBgSprite = "OptionBasePressed";
            m_single.disabledBgSprite = "OptionBaseDisabled";
            m_single.text = "•";
            m_single.textScale = 1.5f;
            m_single.textPadding = new RectOffset(0, 1, 4, 0);
            m_single.textColor = new Color32(119, 124, 126, 255);
            m_single.hoveredTextColor = new Color32(110, 113, 114, 255);
            m_single.pressedTextColor = new Color32(172, 175, 176, 255);
            m_single.focusedTextColor = new Color32(187, 224, 235, 255);
            m_single.disabledTextColor = new Color32(66, 69, 70, 255);
            #endregion

            #region Marquee Select
            m_marquee = m_tabStrip.AddTab("MoveIt_Marquee", null, false);
            m_marquee.group = m_tabStrip;
            m_marquee.atlas = UIUtils.GetAtlas("Ingame");
            m_marquee.tooltip = "Marquee Selection";
            m_marquee.playAudioEvents = true;

            m_marquee.size = new Vector2(36, 36);

            m_marquee.normalBgSprite = "OptionBase";
            m_marquee.focusedBgSprite = "OptionBaseFocused";
            m_marquee.hoveredBgSprite = "OptionBaseHovered";
            m_marquee.pressedBgSprite = "OptionBasePressed";
            m_marquee.disabledBgSprite = "OptionBaseDisabled";

            m_marquee.normalFgSprite = "ZoningOptionMarquee";
            m_marquee.relativePosition = m_single.relativePosition + new Vector3(m_single.width, 0);

            #region filtersPanel
            m_filtersPanel = AddUIComponent(typeof(UIPanel)) as UIPanel;
            m_filtersPanel.atlas = UIUtils.GetAtlas("Ingame");
            m_filtersPanel.backgroundSprite = "SubcategoriesPanel";
            m_filtersPanel.clipChildren = true;
            m_filtersPanel.size = new Vector2(150, 235);
            m_filtersPanel.isVisible = false;
            UIFilters.FilterPanel = m_filtersPanel;

            void OnDoubleClick(UIComponent c, UIMouseEventParameter p)
            {
                foreach (UICheckBox cb in UIFilters.FilterCBs)
                {
                    cb.isChecked = false;
                    Filters.SetFilter(cb.name, false);
                }
                ((UICheckBox)c).isChecked = true;
                Filters.SetFilter(c.name, true);

                UIFilters.RefreshFilters();
            }

            void OnPickerClick(UIComponent c, UIMouseEventParameter p)
            {
                MoveItTool.SetToolState(MoveItTool.ToolStates.Picking);
                UIFilters.UpdatePickerButton(2);
            }

            void OnPickerDoubleClick(UIComponent c, UIMouseEventParameter p)
            {
                Filters.Picker = new PickerFilter();

                Filters.SetFilter("Picker", false);
                MoveItTool.SetToolState();
                UIFilters.UpdatePickerButton(1);
            }

            #region Standard Filters
            m_filtersPanelList = m_filtersPanel.AddUIComponent(typeof(UIPanel)) as UIPanel;
            m_filtersPanelList.name = "m_filtersPanelList";

            m_picker = UIUtils.CreateButton(m_filtersPanel);
            m_picker.relativePosition = new Vector3(122, 9);
            m_picker.size = new Vector2(20, 20);
            m_picker.atlas = GetIconsAtlas();
            m_picker.normalFgSprite = "EyeDropper";
            m_picker.normalBgSprite = "OptionsDropboxListbox";
            m_picker.hoveredBgSprite = "OptionsDropboxListboxFocused";
            m_picker.pressedBgSprite = "OptionsDropboxListboxHovered";
            m_picker.eventClick += OnPickerClick;
            m_picker.eventDoubleClick += OnPickerDoubleClick;
            m_picker.name = "mit_pickerButton";

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Picker", null, false);
            checkBox.width -= 21;
            UIFilters.UpdatePickerLabel("Picker", "Pick an object to filter for objects of the same type", UIFilters.InactiveLabelColor, false);
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Buildings");
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Props");
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Decals");
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Surfaces");
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Trees");
            checkBox.eventDoubleClick += OnDoubleClick;

            if (MoveItTool.PO.Enabled)
            {
                if (MoveItTool.PO.Active)
                {
                    m_filtersPanel.height += 20f;
                }
                checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "PO");
                checkBox.eventDoubleClick += OnDoubleClick;
                checkBox.isVisible = MoveItTool.PO.Active;
            }

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Nodes");
            checkBox.eventDoubleClick += OnDoubleClick;

            checkBox = UIFilters.CreateFilterCB(m_filtersPanelList, "Segments");
            checkBox.eventDoubleClick += OnDoubleClick;
            #endregion

            #region Network Filters
            UIButton btnNetworks = UIFilters.CreateToggleNFBtn();
            void OnDoubleClickNetworkFilter(UIComponent c, UIMouseEventParameter p)
            {
                foreach (UICheckBox cb in UIFilters.NetworkCBs)
                {
                    cb.isChecked = false;
                    Filters.SetNetworkFilter(cb.name, false);
                }
                ((UICheckBox)c).isChecked = true;
                Filters.SetNetworkFilter(c.name, true);

                UIFilters.RefreshFilters();
            }

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Roads");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Tracks");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Paths");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Fences");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Powerlines");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            checkBox = UIFilters.CreateNetworkFilterCB(m_filtersPanelList, "Others");
            checkBox.eventDoubleClick += OnDoubleClickNetworkFilter;

            UIFilters.RefreshFilters();
            #endregion

            m_filtersPanelList.padding = new RectOffset(10, 10, 10, 10);
            m_filtersPanelList.autoLayoutDirection = LayoutDirection.Vertical;
            m_filtersPanelList.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            m_filtersPanelList.autoLayout = true;
            m_filtersPanelList.relativePosition = new Vector3(0, 0, 0);
            m_filtersPanel.autoLayout = false;
            m_filtersPanel.absolutePosition = m_marquee.absolutePosition + new Vector3(-47, -5 - m_filtersPanel.height);
            #endregion

            m_marquee.eventButtonStateChanged += (c, p) =>
            {
                MoveItTool.marqueeSelection = p == UIButton.ButtonState.Focused;
                m_filtersPanel.isVisible = MoveItTool.marqueeSelection;

                if (UIChangesWindow.instance != null)
                {
                    UIChangesWindow.instance.RefreshPosition();
                }
            };
            m_marquee.eventDoubleClick += (UIComponent c, UIMouseEventParameter p) =>
            {
                bool newChecked = false;
                foreach (UICheckBox cb in m_filtersPanel.GetComponentsInChildren<UICheckBox>())
                {
                    if (cb.name == "Picker")
                    {
                        continue;
                    }
                    if (!cb.isChecked)
                    {
                        newChecked = true;
                        break;
                    }
                }

                foreach (UICheckBox cb in m_filtersPanel.GetComponentsInChildren<UICheckBox>())
                {
                    if (cb.name == "Picker")
                    {
                        cb.isChecked = false;
                        Filters.SetAnyFilter(cb.name, false);
                    }
                    else
                    {
                        cb.isChecked = newChecked;
                        Filters.SetAnyFilter(cb.name, newChecked);
                    }
                }

                UIFilters.RefreshFilters();
            };
            #endregion

            #region Copy
            m_copy = AddUIComponent<UIButton>();
            m_copy.name = "MoveIt_Copy";
            m_copy.group = m_tabStrip;
            m_copy.atlas = GetIconsAtlas();
            m_copy.tooltip = "Copy (Alt+Click to duplicate in-place)";
            m_copy.playAudioEvents = true;

            m_copy.size = new Vector2(36, 36);

            m_copy.normalBgSprite = "OptionBase";
            m_copy.hoveredBgSprite = "OptionBaseHovered";
            m_copy.pressedBgSprite = "OptionBasePressed";
            m_copy.disabledBgSprite = "OptionBaseDisabled";

            m_copy.normalFgSprite = "Copy";

            m_copy.relativePosition = m_tabStrip.relativePosition + new Vector3(m_single.width + m_marquee.width, 0);

            m_copy.eventClicked += (c, p) =>
            {
                if (MoveItTool.instance != null)
                {
                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning)
                    {
                        MoveItTool.instance.StopCloning();
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                        {
                            if (Action.selection.Count > 0)
                            {
                                DuplicateAction action = new DuplicateAction();
                                if (action.Count > 0)
                                {
                                    ActionQueue.instance.Push(action);
                                    ActionQueue.instance.Do();
                                }
                            }
                        }
                        else
                        {
                            MoveItTool.instance.StartCloning();
                        }
                    }
                }
            };
            #endregion

            #region Bulldoze
            m_bulldoze = AddUIComponent<UIButton>();
            m_bulldoze.name = "MoveIt_Bulldoze";
            m_bulldoze.group = m_tabStrip;
            m_bulldoze.atlas = GetIconsAtlas();
            m_bulldoze.tooltip = "Bulldoze";
            m_bulldoze.playAudioEvents = true;

            m_bulldoze.size = new Vector2(36, 36);

            m_bulldoze.normalBgSprite = "OptionBase";
            m_bulldoze.hoveredBgSprite = "OptionBaseHovered";
            m_bulldoze.pressedBgSprite = "OptionBasePressed";
            m_bulldoze.disabledBgSprite = "OptionBaseDisabled";

            m_bulldoze.normalFgSprite = "Bulldoze";

            m_bulldoze.relativePosition = m_copy.relativePosition + new Vector3(m_copy.width, 0);

            m_bulldoze.eventClicked += (c, p) =>
            {
                if (MoveItTool.instance != null)
                {
                    MoveItTool.instance.StartBulldoze();
                }
            };
            #endregion

            #region More Tools
            #region More Tools Container
            m_moreTools = AddUIComponent<UIButton>();
            UIMoreTools.MoreToolsBtn = m_moreTools;
            m_moreTools.name = "MoveIt_MoreToolsBtn";
            m_moreTools.group = m_tabStrip;
            m_moreTools.atlas = GetIconsAtlas();
            m_moreTools.tooltip = "More Tools";
            m_moreTools.playAudioEvents = true;
            m_moreTools.size = new Vector2(36, 36);
            m_moreTools.normalBgSprite = "OptionBase";
            m_moreTools.hoveredBgSprite = "OptionBaseHovered";
            m_moreTools.pressedBgSprite = "OptionBasePressed";
            m_moreTools.disabledBgSprite = "OptionBaseDisabled";
            m_moreTools.normalFgSprite = "MoreTools";
            m_moreTools.relativePosition = m_bulldoze.relativePosition + new Vector3(m_bulldoze.width, 0);
            m_moreTools.eventClicked += (UIComponent c, UIMouseEventParameter p) =>
            {
                UIMoreTools.MoreToolsClicked(m_moreTools.name);
            };

            m_moreToolsPanel = AddUIComponent<UIPanel>();
            UIMoreTools.MoreToolsPanel = m_moreToolsPanel;
            m_moreToolsPanel.name = "mtPanel";
            m_moreToolsPanel.autoLayout = false;
            m_moreToolsPanel.clipChildren = true;
            m_moreToolsPanel.size = new Vector2(36, 132);
            m_moreToolsPanel.isVisible = false;
            m_moreToolsPanel.absolutePosition = m_moreTools.absolutePosition + new Vector3(0, 10 - m_moreToolsPanel.height);
            m_moreTools.zOrder = m_moreToolsPanel.zOrder + 10;

            // The vertical shade
            UIPanel mtpBackground = m_moreToolsPanel.AddUIComponent<UIPanel>();
            mtpBackground.name = "mtpBackground";
            mtpBackground.clipChildren = true;
            mtpBackground.relativePosition = new Vector3(5, 10);
            mtpBackground.atlas = UIUtils.GetAtlas("Ingame");
            mtpBackground.backgroundSprite = "InfoPanelBack";
            mtpBackground.size = m_moreToolsPanel.size - new Vector2(10, 10);

            UIPanel mtpContainer = m_moreToolsPanel.AddUIComponent<UIPanel>();
            mtpContainer.name = "mtpContainer";
            mtpContainer.autoLayoutDirection = LayoutDirection.Vertical;
            mtpContainer.autoLayoutPadding = new RectOffset(0, 0, 0, 4);
            mtpContainer.autoLayout = true;
            mtpContainer.relativePosition = Vector3.zero;
            mtpContainer.size = m_moreToolsPanel.size;
            mtpContainer.eventMouseEnter += (UIComponent c, UIMouseEventParameter p) => { if (MoveItTool.marqueeSelection) m_filtersPanel.isVisible = false; };
            mtpContainer.eventMouseLeave += (UIComponent c, UIMouseEventParameter p) => { if (MoveItTool.marqueeSelection) m_filtersPanel.isVisible = true; };

            UIMoreTools.MoreButtons.Clear();
            UIMoreTools.MoreSubButtons.Clear();
            #endregion

            #region More Tools buttons
            UIMoreToolsBtn othersBtn = new UIMoreToolsBtn(this, "MoveIt_OthersBtn", "Other Tools", "MenuOthers", mtpContainer, "m_mtOthersList", (MoveItTool.PO.Enabled ? 7.25f : 6.25f));
            if (MoveItTool.PO.Enabled)
                othersBtn.CreateSubButton("MoveIt_ConvertToPOBtn", "Convert To PO", "ConvertToPO");
            othersBtn.CreateSubButton("MoveIt_AlignLineBtn", "Line Up Objects", "AlignLine");
            othersBtn.CreateSubButton("MoveIt_AlignMirrorBtn", "Mirror Objects", "AlignMirror");
            othersBtn.CreateSubButton("MoveIt_ResetObjectBtn", "Reset Objects", "ResetObject");
            othersBtn.CreateSubButton("MoveIt_MoveToBtn", "Set Position", "MoveTo");
            othersBtn.CreateSubSeparator("MoveIt_FileSeparator");
            othersBtn.CreateSubButton("MoveIt_LoadBtn", "Import Selection", "Load");
            othersBtn.CreateSubButton("MoveIt_SaveBtn", "Export Selection", "Save");

            UIMoreToolsBtn rotateBtn = new UIMoreToolsBtn(this, "MoveIt_RotateBtn", "Rotation Tools", "MenuRotate", mtpContainer, "m_mtRotateList", 3f);
            rotateBtn.CreateSubButton("MoveIt_AlignRandomBtn", "Rotate Randomly", "AlignRandom");
            rotateBtn.CreateSubButton("MoveIt_AlignGroupBtn", "Rotate At Centre", "AlignGroup");
            rotateBtn.CreateSubButton("MoveIt_AlignIndividualBtn", "Rotate In-Place", "AlignIndividual");

            UIMoreToolsBtn heightBtn = new UIMoreToolsBtn(this, "MoveIt_HeightBtn", "Height Tools", "MenuHeight", mtpContainer, "m_mtHeightList", 3f);
            //heightBtn.CreateSubButton("MoveIt_SlopeNetworkBtn", "Slope Network", "SlopeNetwork");
            heightBtn.CreateSubButton("MoveIt_AlignSlopeBtn", "Slope Objects", "AlignSlope");
            heightBtn.CreateSubButton("MoveIt_AlignTerrainHeightBtn", "To Terrain Height", "AlignTerrainHeight");
            heightBtn.CreateSubButton("MoveIt_AlignHeightBtn", "To Object Height", "AlignHeight");
            #endregion
            #endregion

            #region View Options
            m_viewOptions = AddUIComponent<UIPanel>();
            m_viewOptions.atlas = UIUtils.GetAtlas("Ingame");
            m_viewOptions.backgroundSprite = "InfoPanelBack";
            m_viewOptions.size = new Vector2(44f, 80f);

            m_viewOptions.absolutePosition = new Vector3(GetUIView().GetScreenResolution().x - m_viewOptions.width, absolutePosition.y - m_viewOptions.height - 8f);


            grid = m_viewOptions.AddUIComponent<UIMultiStateButton>();
            grid.atlas = GetIconsAtlas();
            grid.name = "MoveIt_GridView";
            grid.tooltip = "Toggle Grid";
            grid.playAudioEvents = true;

            grid.size = new Vector2(36, 36);
            grid.spritePadding = new RectOffset();

            grid.backgroundSprites[0].disabled = "OptionBaseDisabled";
            grid.backgroundSprites[0].hovered = "OptionBaseHovered";
            grid.backgroundSprites[0].normal = "OptionBase";
            grid.backgroundSprites[0].pressed = "OptionBasePressed";

            grid.backgroundSprites.AddState();
            grid.backgroundSprites[1].disabled = "OptionBaseDisabled";
            grid.backgroundSprites[1].hovered = "";
            grid.backgroundSprites[1].normal = "OptionBaseFocused";
            grid.backgroundSprites[1].pressed = "OptionBasePressed";

            grid.foregroundSprites[0].normal = "Grid";

            grid.foregroundSprites.AddState();
            grid.foregroundSprites[1].normal = "GridFocused";

            grid.relativePosition = new Vector3(4f, 4f);

            grid.activeStateIndex = 0;

            grid.eventClicked += (c, p) =>
            {
                MoveItTool.gridVisible = (grid.activeStateIndex == 1);
            };


            underground = m_viewOptions.AddUIComponent<UIMultiStateButton>();
            underground.atlas = UIUtils.GetAtlas("Ingame");
            underground.name = "MoveIt_UndergroundView";
            underground.tooltip = "Toggle Underground View";
            underground.playAudioEvents = true;

            underground.size = new Vector2(36, 36);
            underground.spritePadding = new RectOffset();

            underground.backgroundSprites[0].disabled = "OptionBaseDisabled";
            underground.backgroundSprites[0].hovered = "OptionBaseHovered";
            underground.backgroundSprites[0].normal = "OptionBase";
            underground.backgroundSprites[0].pressed = "OptionBasePressed";

            underground.backgroundSprites.AddState();
            underground.backgroundSprites[1].disabled = "OptionBaseDisabled";
            underground.backgroundSprites[1].hovered = "";
            underground.backgroundSprites[1].normal = "OptionBaseFocused";
            underground.backgroundSprites[1].pressed = "OptionBasePressed";

            underground.foregroundSprites[0].normal = "BulldozerOptionPipes";

            underground.foregroundSprites.AddState();
            underground.foregroundSprites[1].normal = "BulldozerOptionPipesFocused";

            underground.relativePosition = new Vector3(4f, 40f);

            underground.activeStateIndex = 0;

            underground.eventClicked += (c, p) =>
            {
                MoveItTool.tunnelVisible = (underground.activeStateIndex == 1);
            };


            if (MoveItTool.PO.Enabled)
            {
                PO_button = m_viewOptions.AddUIComponent<UIMultiStateButton>();
                PO_button.atlas = GetIconsAtlas();
                PO_button.name = "MoveIt_PO_button";
                PO_button.tooltip = "Toggle Procedural Objects";
                PO_button.playAudioEvents = true;

                PO_button.size = new Vector2(36, 36);
                PO_button.spritePadding = new RectOffset();

                PO_button.backgroundSprites[0].disabled = "OptionBaseDisabled";
                PO_button.backgroundSprites[0].hovered = "OptionBaseHovered";
                PO_button.backgroundSprites[0].normal = "OptionBase";
                PO_button.backgroundSprites[0].pressed = "OptionBasePressed";

                PO_button.backgroundSprites.AddState();
                PO_button.backgroundSprites[1].disabled = "OptionBaseDisabled";
                PO_button.backgroundSprites[1].hovered = "";
                PO_button.backgroundSprites[1].normal = "OptionBaseFocused";
                PO_button.backgroundSprites[1].pressed = "OptionBasePressed";

                PO_button.foregroundSprites[0].normal = "PO";

                PO_button.foregroundSprites.AddState();
                PO_button.foregroundSprites[1].normal = "POFocused";

                PO_button.relativePosition = new Vector3(4f, 76f);

                PO_button.activeStateIndex = 0;

                PO_button.eventClicked += (c, p) =>
                {
                    MoveItTool.PO.InitialiseTool();
                };

                m_viewOptions.height += 36;
                m_viewOptions.absolutePosition += new Vector3(0, -36);
            }

            #endregion
        }

        protected override void OnVisibilityChanged()
        {
            if (isVisible)
            {
                relativePosition = new Vector2(GetUIView().GetScreenResolution().x - 340, -41);
            }
            base.OnVisibilityChanged();
        }

        public static void RefreshSnapButton()
        {
            if (instance != null && instance.m_snapping != null && MoveItTool.instance != null)
            {
                instance.m_snapping.activeStateIndex = MoveItTool.instance.snapping ? 1 : 0;
            }
        }

        public static void RefreshAlignHeightButton()
        {
            if (instance != null && instance.m_moreTools != null && MoveItTool.instance != null)
            {
                if (MoveItTool.ToolState == MoveItTool.ToolStates.Aligning)
                {
                    instance.m_moreTools.normalBgSprite = "OptionBaseFocused";
                }
                else
                {
                    instance.m_moreTools.normalBgSprite = "OptionBase";
                }
            }
        }

        public static void RefreshCloneButton()
        {
            if (instance != null && instance.m_copy != null && MoveItTool.instance != null)
            {
                if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning || MoveItTool.ToolState == MoveItTool.ToolStates.RightDraggingClone)
                {
                    instance.m_copy.normalBgSprite = "OptionBaseFocused";
                }
                else
                {
                    instance.m_copy.normalBgSprite = "OptionBase";
                }
            }
        }

        private UITextureAtlas GetFollowTerrainAtlas()
        {

            Texture2D[] textures =
            {
                atlas["ToggleBaseDisabled"].texture,
                atlas["ToggleBaseHovered"].texture,
                atlas["ToggleBase"].texture,
                atlas["ToggleBasePressed"].texture,
                atlas["ToggleBaseFocused"].texture

            };

            string[] spriteNames = new string[]
            {
                "FollowTerrain",
                "FollowTerrain_disabled"
            };

            UITextureAtlas loadedAtlas = ResourceLoader.CreateTextureAtlas("MoveIt_FollowTerrain", spriteNames, "MoveIt.Icons.");
            ResourceLoader.AddTexturesInAtlas(loadedAtlas, textures);

            return loadedAtlas;
        }

        internal UITextureAtlas GetIconsAtlas()
        {
            Texture2D[] textures =
            {
                atlas["OptionBase"].texture,
                atlas["OptionBaseHovered"].texture,
                atlas["OptionBasePressed"].texture,
                atlas["OptionBaseDisabled"].texture,
                atlas["OptionBaseFocused"].texture,
                atlas["OptionsDropboxListbox"].texture,
                atlas["OptionsDropboxListboxHovered"].texture,
                atlas["OptionsDropboxListboxFocused"].texture
            };

            string[] spriteNames = new string[]
            {
                "MoreTools",
                "AlignIndividual",
                "AlignIndividualActive",
                "AlignGroup",
                "AlignGroupActive",
                "AlignRandom",
                "AlignSlope",
                "AlignSlopeA",
                "AlignSlopeB",
                "AlignHeight",
                "AlignLine",
                "AlignMirror",
                "AlignTerrainHeight",
                //"SlopeNetwork",
                "MoveTo",
                "MoveToActive",
                "ConvertToPO",
                "EyeDropper",
                "Copy",
                "Bulldoze",
                "ResetObject",
                "MenuHeight",
                "MenuOthers",
                "MenuRotate",
                "Save",
                "Save_disabled",
                "Load",
                "Grid",
                "GridFocused",
                "PO",
                "POFocused",
                "SubmenuBG",
                "SubmenuSeparator"
            };

            UITextureAtlas loadedAtlas = ResourceLoader.CreateTextureAtlas("MoveIt_Icons", spriteNames, "MoveIt.Icons.");
            ResourceLoader.AddTexturesInAtlas(loadedAtlas, textures);

            return loadedAtlas;
        }
    }
}
