using System;
using System.Reflection;

using UnityEngine;

using ColossalFramework;
using ColossalFramework.UI;

namespace MoveIt
{
    public class UIMoveItButton: UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", MoveItTool.settingsFileName, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", MoveItTool.settingsFileName, -1000, true);

        public override void Start()
        {
            LoadResources();

            UIComponent bulldoserButton = UIView.GetAView().FindUIComponent<UIComponent>("BulldozerButton");

            name = "MoveIt";
            tooltip = "Move It! " + ModInfo.version;

            normalFgSprite = "MoveIt";
            hoveredFgSprite = "MoveIt_hover";

            size = bulldoserButton.size;

            if (savedX.value == -1000)
            {
                absolutePosition = new Vector2(bulldoserButton.absolutePosition.x - bulldoserButton.width, bulldoserButton.absolutePosition.y);
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
                MoveItTool.instance.enabled = !MoveItTool.instance.enabled;
            }

            base.OnClick(p);
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
            base.OnMouseDown(p);
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
            base.OnMouseDown(p);
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

            base.Update();
        }

        public void OnGUI()
        {
            if (OptionsKeymapping.toggleTool.IsPressed(Event.current))
            {
                MoveItTool.instance.enabled = !MoveItTool.instance.enabled;
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
