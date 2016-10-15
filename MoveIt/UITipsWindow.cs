using UnityEngine;

using ColossalFramework.UI;

namespace MoveIt
{
    public class UITipsWindow : UILabel
    {
        public static UITipsWindow instance;

        private string[] m_tips =
        {
            "New in 1.3.0: Marquee Selection!\nClick the button below",
            "Tip: Hold Alt to deselect using the marquee selection",
            "Tip: Hold Shift to select multiple objects to move at once",
            "Tip: Use Left Click to drag objects around",
            "Tip: While holding Right Click, move the mouse left and right to rotate objects",
            "Tip: Use Alt for finer movements with the keyboard",
            "Tip: Use Shift for faster movements with the keyboard",
            "Tip: The object under the mouse will move if nothing else is selected",
            "Tip: Right Click to clear the selection",
            "Tip: Buildings, Trees, Props and Nodes can all be moved",
            "Tip: Movable objects are highlighted when hovered",
            "Tip: Hover various things to discover what can be moved",
            "Tip: Look for the tiny green circle\nThat's the center of rotation",
            "Tip: Disable tips in the mod options\nEsc > Options > Move It! > Hide tips"
        };
        private int m_currentTip = -1;

        public override void Start()
        {
            backgroundSprite = "GenericPanelWhite";

            size = new Vector2(300, 100);
            padding = new RectOffset(10, 10, 10, 10);
            textColor = new Color32(109, 109, 109, 255);

            wordWrap = true;
            autoHeight = true;

            instance = this;
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

        protected override void OnSizeChanged()
        {
            RefreshPosition();
        }

        public void RefreshPosition()
        {

            float x = Screen.width - width - 10f;
            float y;

            if (UIToolOptionPanel.instance != null && MoveItTool.marqueeSelection)
            {
                y = UIToolOptionPanel.instance.filtersPanel.absolutePosition.y - height - 10f;
            }
            else
            {
                y = GetUIView().FindUIComponent<UIComponent>("ThumbnailBar").absolutePosition.y - height - 30f;
            }

            absolutePosition = new Vector3(x, y);
        }
    }
}
