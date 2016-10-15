using UnityEngine;

using ColossalFramework.UI;

using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UIToolOptionPanel : UIPanel
    {
        public static UIToolOptionPanel instance;

        private UIMultiStateButton m_snapping;

        private UITabstrip m_tabStrip;
        private UIButton m_single;
        private UIButton m_marquee;

        public UIPanel filtersPanel;

        public override void Start()
        {
            instance = this;

            size = new Vector2(41, 41);
            relativePosition = new Vector2(Screen.width - 300, -41);
            name = "MoveIt_ToolOptionPanel";

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

            m_snapping.relativePosition = Vector2.zero;

            m_snapping.activeStateIndex = MoveItTool.snapping ? 1 : 0;

            m_snapping.eventActiveStateIndexChanged += (c, p) =>
            {
                MoveItTool.snapping = (p == 1);
            };

            m_tabStrip = AddUIComponent<UITabstrip>();
            m_tabStrip.size = new Vector2(36, 72);

            m_tabStrip.relativePosition = m_snapping.relativePosition + new Vector3(m_snapping.width, 0);

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

            filtersPanel = AddUIComponent(typeof(UIPanel)) as UIPanel;
            filtersPanel.atlas = UIUtils.GetAtlas("Ingame");
            filtersPanel.backgroundSprite = "SubcategoriesPanel";
            filtersPanel.clipChildren = true;

            filtersPanel.size = new Vector2(150, 110);
            filtersPanel.isVisible = false;
            
            UICheckBox checkBox = UIUtils.CreateCheckBox(filtersPanel);
            checkBox.label.text = "Buildings";
            checkBox.isChecked = true;
            checkBox.eventCheckChanged += (c, p) =>
            {
                MoveItTool.filterBuildings = p;
            };

            checkBox = UIUtils.CreateCheckBox(filtersPanel);
            checkBox.label.text = "Props";
            checkBox.isChecked = true;
            checkBox.eventCheckChanged += (c, p) =>
            {
                MoveItTool.filterProps = p;
            };

            checkBox = UIUtils.CreateCheckBox(filtersPanel);
            checkBox.label.text = "Trees";
            checkBox.isChecked = true;
            checkBox.eventCheckChanged += (c, p) =>
            {
                MoveItTool.filterTrees = p;
            };

            checkBox = UIUtils.CreateCheckBox(filtersPanel);
            checkBox.label.text = "Nodes";
            checkBox.isChecked = true;
            checkBox.eventCheckChanged += (c, p) =>
            {
                MoveItTool.filterNodes = p;
            };

            filtersPanel.padding = new RectOffset(10, 10, 10, 10);
            filtersPanel.autoLayoutDirection = LayoutDirection.Vertical;
            filtersPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            filtersPanel.autoLayout = true;

            filtersPanel.height = 110;

            filtersPanel.absolutePosition = m_marquee.absolutePosition - new Vector3(57, 115);

            m_marquee.eventButtonStateChanged += (c, p) =>
            {
                MoveItTool.marqueeSelection = p == UIButton.ButtonState.Focused;
                filtersPanel.isVisible = MoveItTool.marqueeSelection;

                if (UITipsWindow.instance != null)
                {
                    UITipsWindow.instance.RefreshPosition();
                }
            };
        }
    }
}
