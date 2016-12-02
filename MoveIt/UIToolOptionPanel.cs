using UnityEngine;

using ColossalFramework.UI;

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

        public UIPanel filtersPanel;

        public override void Start()
        {
            instance = this;

            atlas = UIUtils.GetAtlas("Ingame");
            size = new Vector2(41, 41);
            relativePosition = new Vector2(GetUIView().GetScreenResolution().x - 300, -41);
            name = "MoveIt_ToolOptionPanel";

            DebugUtils.Log("ToolOptionPanel position: " + absolutePosition);

            m_followTerrain = AddUIComponent<UIMultiStateButton>();
            m_followTerrain.atlas = GetFollowTerrainAtlas();
            m_followTerrain.name = "MoveIt_Snapping";
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
            };

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

            filtersPanel.size = new Vector2(150, 140);
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

            checkBox = UIUtils.CreateCheckBox(filtersPanel);
            checkBox.label.text = "Segments";
            checkBox.isChecked = true;
            checkBox.eventCheckChanged += (c, p) =>
            {
                MoveItTool.filterSegments = p;
            };

            filtersPanel.padding = new RectOffset(10, 10, 10, 10);
            filtersPanel.autoLayoutDirection = LayoutDirection.Vertical;
            filtersPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            filtersPanel.autoLayout = true;

            filtersPanel.height = 140;

            filtersPanel.absolutePosition = m_marquee.absolutePosition - new Vector3(57, filtersPanel.height + 5);

            m_marquee.eventButtonStateChanged += (c, p) =>
            {
                MoveItTool.marqueeSelection = p == UIButton.ButtonState.Focused;
                filtersPanel.isVisible = MoveItTool.marqueeSelection;

                if (UITipsWindow.instance != null)
                {
                    UITipsWindow.instance.RefreshPosition();
                }
            };

            m_copy = AddUIComponent<UIButton>();
            m_copy.name = "MoveIt_Copy";
            m_copy.group = m_tabStrip;
            m_copy.atlas = GetIconsAtlas();
            m_copy.tooltip = "Copy";
            m_copy.playAudioEvents = true;

            m_copy.size = new Vector2(36, 36);

            m_copy.normalBgSprite =  "OptionBase";
            m_copy.hoveredBgSprite = "OptionBaseHovered";
            m_copy.pressedBgSprite = "OptionBasePressed";
            m_copy.disabledBgSprite = "OptionBaseDisabled";

            m_copy.normalFgSprite = "Copy";

            m_copy.relativePosition = m_tabStrip.relativePosition + new Vector3(m_single.width + m_marquee.width, 0);

            m_copy.eventClicked += (c, p) =>
            {
                if (MoveItTool.instance != null)
                {
                    MoveItTool.instance.StartCloning();
                }
            };

            m_bulldoze = AddUIComponent<UIButton>();
            m_bulldoze.name = "MoveIt_Bulldoze";
            m_bulldoze.group = m_tabStrip;
            m_bulldoze.atlas = GetIconsAtlas();
            m_bulldoze.tooltip = "Bulldoze\nWARNING: NO UNDO!";
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
        }

        protected override void OnVisibilityChanged()
        {
            if(isVisible)
            {
                relativePosition = new Vector2(GetUIView().GetScreenResolution().x - 300, -41);
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

        private UITextureAtlas GetIconsAtlas()
        {

            Texture2D[] textures = 
            {
                atlas["OptionBase"].texture,
                atlas["OptionBaseHovered"].texture,
                atlas["OptionBasePressed"].texture,
                atlas["OptionBaseDisabled"].texture
                
            };

            string[] spriteNames = new string[]
			{
				"Copy",
                "Bulldoze"
			};

            UITextureAtlas loadedAtlas = ResourceLoader.CreateTextureAtlas("MoveIt_Icons", spriteNames, "MoveIt.Icons.");
            ResourceLoader.AddTexturesInAtlas(loadedAtlas, textures);

            return loadedAtlas;
        }
    }
}
