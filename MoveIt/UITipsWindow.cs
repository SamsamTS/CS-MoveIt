using UnityEngine;

using ColossalFramework;
using ColossalFramework.UI;

namespace MoveIt
{
    public class UITipsWindow: UILabel
    {
        public static UITipsWindow instance;

        private string[] m_tips =
        {
            "Hold Shift to select multiple objects to move at once",
            "Use Left Click to drag objects around",
            "While holding Right Click, move the mouse left and right to rotate objects",
            "Use Alt for finer movements with the keyboard",
            "Use Shift for faster movements with the keyboard",
            "Right Click to clear the selection",
            "Buildings, Trees, Props and Nodes can all be moved",
            "Movable objects are highlighted when hovered",
            "Hover various things to discover what can be moved",
            "Look for the tiny green circle\nThat's the center of rotation",
            "Disable tips in the mod options\nEsc > Options > Move It! > Hide tips"
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
            text = "Tip: " + m_tips[m_currentTip] + "\n";
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            NextTip();
        }

        protected override void OnSizeChanged()
        {
            float x = Screen.width - width - 10f;
            float y =  GetUIView().FindUIComponent<UIComponent>("ThumbnailBar").absolutePosition.y - height - 10f;
            absolutePosition = new Vector3(x, y);
        }
    }
}
