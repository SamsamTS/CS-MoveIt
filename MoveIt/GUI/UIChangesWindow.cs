using UnityEngine;
using ColossalFramework.UI;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UIChangesWindow : UILabel
    {
        public static UIChangesWindow instance;

        private const string m_blurb =
            "New in Move It 2.8.0:\n\n" +
            "- Slope Align now automatically uses the 2 furthest apart select objects (Shift+Click on the tool icon to manually select the 2 points).\n\n" +
            "- Toolbox (More Tools) menu redesigned, and some icons changed.\n\n" + 
            "- Line Tool - Evenly space out objects in a straight line.\n\n" +
            "- Set Position Tool - Change a selection's coordinates.\n\n" +
            "- Options page redesigned.\n\n" +
            "- Many other tweaks and optimisations!\n\n" +
            "Read the workshop Move It Guide for more information.\n\n" +
            "Click anywhere on this box to close it.";

        public override void Start()
        {
            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "GenericPanelWhite";

            size = new Vector2(500, 300);
            padding = new RectOffset(10, 10, 10, 10);
            textColor = new Color32(0, 0, 0, 255);
            textScale = 0.9f;

            wordWrap = true;
            autoHeight = true;

            instance = this;
            text = m_blurb;
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            textColor = new Color32(99, 99, 99, 255);
        }

        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            textColor = new Color32(0, 0, 0, 255);
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            isVisible = false;
            MoveItTool.hideChangesWindow.value = true;
            // Close and disable
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
            float x = (GetUIView().GetScreenResolution().x / 2) - (width / 2);
            float y = (GetUIView().GetScreenResolution().y / 2) - (height / 2) - 50;

            absolutePosition = new Vector3(x, y);
        }
    }
}
