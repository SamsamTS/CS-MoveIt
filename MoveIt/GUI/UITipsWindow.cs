using UnityEngine;

using ColossalFramework.UI;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UITipsWindow : UILabel
    {
        public static UITipsWindow instance;

        private string[] m_tips =
        {
            "New in 2.4.0: Hold Shift to move objects in a fast, low detail mode",
            "Tip: Hold Alt to select a node or segment owned by a building",
            "Tip: Hold Alt to deselect objects using the marquee selection",
            "Tip: A building with an orange highlight will despawn when the simulation is running",
            "Tip: Hold Ctrl while rotating to snap to 45° angles",
            "Tip: Hold Shift to select multiple objects to move at once",
            "Tip: Use Left Click to drag objects around",
            "Tip: Hold Alt while dragging objects to reverse the Snapping option",
            "Tip: While holding Right Click, move the mouse left and right to rotate objects",
            "Tip: Use Alt for finer movements with the keyboard",
            "Tip: Use Shift for faster movements with the keyboard",
            "Tip: When Follow Terrain is disabled, objects will keep their height when moved",
            "Tip: Right Click to clear the selection",
            "Tip: Buildings, Trees, Props, Decals, Surfaces and Networks can all be moved",
            "Tip: Movable objects are highlighted when hovered",
            "Tip: Hover various things to discover what can be moved",
            "Tip: Look for the tiny green circle\nThat's the center of rotation",
            "Tip: Disable tips in the mod options\nEsc > Options > Move It! > Hide tips"
        };
        private int m_currentTip = -1;

        public override void Start()
        {
            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "GenericPanelWhite";

            size = new Vector2(300, 100);
            padding = new RectOffset(10, 10, 10, 10);
            textColor = new Color32(109, 109, 109, 255);
            textScale = 0.9f;

            wordWrap = true;
            autoHeight = true;

            instance = this;

            NextTip();
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            textColor = new Color32(0, 0, 0, 255);
        }

        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            textColor = new Color32(109, 109, 109, 255);
        }

        public void NextTip()
        {
            m_currentTip = (m_currentTip + 1) % m_tips.Length;
            text = m_tips[m_currentTip];
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            NextTip();
        }

        protected override void OnVisibilityChanged()
        {
            if (isVisible)
            {
                RefreshPosition();
            }
            base.OnVisibilityChanged();
        }

        protected override void OnSizeChanged()
        {
            RefreshPosition();
        }

        public void RefreshPosition()
        {
            float x = GetUIView().GetScreenResolution().x - width - 270f;
            float y;

            UIComponent thumbnailBar = GetUIView().FindUIComponent<UIComponent>("ThumbnailBar");
            if (thumbnailBar != null)
            {
                y = thumbnailBar.absolutePosition.y - height - 25f;
            }
            else
            {
                y = Screen.height - height - 365;
            }

            absolutePosition = new Vector3(x, y);
        }
    }
}
