using ColossalFramework;
using ColossalFramework.UI;
using MoveIt.Localization;
using UnityEngine;

namespace MoveIt
{
    public class UIMoveItButton : UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", MoveItTool.settingsFileName, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", MoveItTool.settingsFileName, -1000, true);

        private UIPopupPanel m_changesWindow, m_lsmWarningWindow;

        private UIComponent BulldoserButton
        {
            get
            {
                UIComponent bulldoserButton = GetUIView().FindUIComponent<UIComponent>("MarqueeBulldozer");

                if (bulldoserButton == null)
                {
                    bulldoserButton = GetUIView().FindUIComponent<UIComponent>("BulldozerButton");
                }
                return bulldoserButton;
            }
        }

        public override void Start()
        {
            LoadResources();

            m_changesWindow = GetUIView().AddUIComponent(typeof(UIPopupWindow)) as UIPopupWindow;
            m_changesWindow.isVisible = false;

            m_lsmWarningWindow = GetUIView().AddUIComponent(typeof(UILSMWarning)) as UILSMWarning;
            m_lsmWarningWindow.isVisible = false;

            name = "MoveIt";
            tooltip = Str.baseUI_MoveItButton_Tooltip + " " + ModInfo.version;

            normalFgSprite = "MoveIt";
            hoveredFgSprite = "MoveIt_hover";

            playAudioEvents = true;

            size = new Vector2(43, 49);

            if (savedX.value == -1000)
            {
                absolutePosition = new Vector2(BulldoserButton.absolutePosition.x - width - 5, BulldoserButton.parent.absolutePosition.y);
            }
            else
            {
                absolutePosition = new Vector2(savedX.value, savedY.value);
            }
        }

        public void ResetPosition()
        {
            absolutePosition = new Vector2(BulldoserButton.absolutePosition.x - width - 5, BulldoserButton.parent.absolutePosition.y);
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Left) && MoveItTool.instance != null)
            {
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
            if (MoveItTool.instance != null && MoveItTool.instance.enabled)
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

            atlas = GetAtlas(spriteNames);
        }

        internal static UITextureAtlas GetAtlas(string[] spriteNames)
        {
            return ResourceLoader.CreateTextureAtlas("MoveIt", spriteNames, "MoveIt.Icons.");
        }
    }
}
