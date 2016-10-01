using UnityEngine;

using ColossalFramework;
using ColossalFramework.UI;

namespace MoveIt
{
    public class UIMoveItButton : UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", MoveItTool.settingsFileName, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", MoveItTool.settingsFileName, -1000, true);

        private UITipsWindow m_tipsWindow;

        public override void Start()
        {
            LoadResources();

            m_tipsWindow = GetUIView().AddUIComponent(typeof(UITipsWindow)) as UITipsWindow;
            m_tipsWindow.isVisible = false;

            UIComponent bulldoserButton = GetUIView().FindUIComponent<UIComponent>("MarqueeBulldozer");

            if (bulldoserButton == null)
            {
                bulldoserButton = GetUIView().FindUIComponent<UIComponent>("BulldozerButton");
            }

            name = "MoveIt";
            tooltip = "Move It! " + ModInfo.version;

            normalFgSprite = "MoveIt";
            hoveredFgSprite = "MoveIt_hover";

            playAudioEvents = true;

            size = new Vector2(43, 49);

            if (savedX.value == -1000)
            {
                absolutePosition = new Vector2(bulldoserButton.absolutePosition.x - width - 5, bulldoserButton.parent.absolutePosition.y);
            }
            else
            {
                absolutePosition = new Vector2(savedX.value, savedY.value);
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Left))
            {
                MoveItTool.infoMode = InfoManager.instance.CurrentMode;
                MoveItTool.subInfoMode = InfoManager.instance.CurrentSubMode;

                MoveItTool.instance.enabled = !MoveItTool.instance.enabled;
            }
        }

        private Vector3 m_deltaPos;
        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                m_deltaPos = absolutePosition - mousePos;
                BringToFront();
            }
        }


        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                absolutePosition = mousePos + m_deltaPos;
                savedX.value = (int)absolutePosition.x;
                savedY.value = (int)absolutePosition.y;
            }
        }

        public override void Update()
        {
            if (MoveItTool.instance.enabled)
            {
                normalFgSprite = "MoveIt_focused";
            }
            else
            {
                normalFgSprite = "MoveIt";
            }
        }

        public void OnGUI()
        {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && OptionsKeymapping.toggleTool.IsPressed(Event.current))
            {
                SimulateClick();
            }
        }

        private void LoadResources()
        {
            string[] spriteNames = new string[]
			{
				"MoveIt",
				"MoveIt_focused",
				"MoveIt_hover"
			};

            atlas = ResourceLoader.CreateTextureAtlas("MoveIt", spriteNames, "MoveIt.Icons.");
        }
    }
}
