using UnityEngine;

using ColossalFramework.UI;

using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UIToolOptionPanel : UIPanel
    {
        public static UIToolOptionPanel instance;

        private UIMultiStateButton m_snapping;

        public override void Start()
        {
            instance = this;
            isVisible = false;

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

        }
    }
}
