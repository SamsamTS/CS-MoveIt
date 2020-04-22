using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    internal class MoveToPanel : MonoBehaviour
    {
        internal UIPanel Panel;
        private UISlicedSprite TitleContainer;
        private UIDragHandle TitleDrag;
        private UILabel TitleCaption, XLabel, ZLabel, ALabel, YLabel;
        private UIButton TitleClose, Submit;
        private UISprite TitleIcon;
        private UITextField XInput, ZInput, AInput, YInput;

        internal MoveToPanel()
        {
            _initialise();
        }

        internal void Visible()
        {
            Visible(!Panel.isVisible);
        }

        internal void Visible(bool show)
        {
            Panel.isVisible = show;
            if (show)
            {
                UpdateValues();
            }
        }

        internal void UpdateValues()
        {
            if (!Panel.isVisible) return;

            Vector3 center = Action.GetCenter();
            XInput.text = center.x.ToString();
            YInput.text = center.y.ToString();
            ZInput.text = center.z.ToString();
            AInput.text = (Action.GetAngle() * Mathf.Rad2Deg).ToString();
            if (AInput.text == "NaN")
            {
                AInput.text = "";
                AInput.enabled = false;
            }
            else
            {
                AInput.enabled = true;
            }
        }

        private void _initialise()
        {
            Panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Panel.name = "MoveIt_MoveToPanel";
            Panel.atlas = ResourceLoader.GetAtlas("Ingame");
            Panel.backgroundSprite = "MenuPanel2";
            Panel.size = new Vector2(190, 185);
            Panel.absolutePosition = new Vector3(Panel.GetUIView().GetScreenResolution().x - 220, Panel.GetUIView().GetScreenResolution().y - 450);
            Panel.clipChildren = true;
            Panel.isVisible = false;
            Panel.anchor = UIAnchorStyle.None;

            TitleContainer = Panel.AddUIComponent<UISlicedSprite>();
            TitleContainer.position = new Vector3(0, 0);
            TitleContainer.size = new Vector2(Panel.width, 40);

            TitleIcon = TitleContainer.AddUIComponent(typeof(UISprite)) as UISprite;
            TitleIcon.relativePosition = new Vector3(4, 4);
            TitleIcon.size = new Vector2(32, 32);
            TitleIcon.atlas = UIMoveItButton.GetAtlas(new string[] { "MoveIt" });
            TitleIcon.spriteName = "MoveIt";

            TitleCaption = TitleContainer.AddUIComponent<UILabel>();
            TitleCaption.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
            TitleCaption.size = new Vector2(Panel.width, 40);
            TitleCaption.text = "Position";

            TitleDrag = TitleContainer.AddUIComponent(typeof(UIDragHandle)) as UIDragHandle;
            TitleDrag.relativePosition = new Vector3(0, 0);
            TitleDrag.size = new Vector2(Panel.width, 40);
            TitleDrag.target = Panel;

            TitleClose = TitleContainer.AddUIComponent<UIButton>();
            TitleClose.atlas = ResourceLoader.GetAtlas("Ingame");
            TitleClose.normalBgSprite = "buttonclose";
            TitleClose.pressedBgSprite = "buttonclosepressed";
            TitleClose.hoveredBgSprite = "buttonclosehover";
            TitleClose.size = new Vector2(32, 32);
            TitleClose.relativePosition = new Vector3(Panel.width - 36, 4);
            TitleClose.eventClicked += (UIComponent c, UIMouseEventParameter p) =>
            {
                Visible(false);
            };

            XLabel = Panel.AddUIComponent<UILabel>();
            XLabel.relativePosition = new Vector3(8, 52);
            XLabel.text = "X:";
            XInput = UIUtils.CreateTextField(Panel);
            XInput.relativePosition = new Vector3(30, 48);
            XInput.size = new Vector2(148, 24);
            XInput.horizontalAlignment = UIHorizontalAlignment.Left;
            XInput.tabIndex = 0;
            XInput.eventTextSubmitted += (UIComponent component, string value) =>
            {
                Debug.Log($"{XInput.text}");
            };

            ZLabel = Panel.AddUIComponent<UILabel>();
            ZLabel.relativePosition = new Vector3(8, 86);
            ZLabel.text = "Y:";
            ZInput = UIUtils.CreateTextField(Panel);
            ZInput.relativePosition = new Vector3(30, 82);
            ZInput.size = new Vector2(148, 24);
            ZInput.horizontalAlignment = UIHorizontalAlignment.Left;
            ZInput.tabIndex = 1;
            //ZInput.eventTextSubmitted += (UIComponent component, string value) =>
            //{
            //    Debug.Log($"{ZInput.text}");
            //};

            YLabel = Panel.AddUIComponent<UILabel>();
            YLabel.relativePosition = new Vector3(8, 120);
            YLabel.text = "H:";
            YLabel.tooltip = "Height";
            YInput = UIUtils.CreateTextField(Panel);
            YInput.relativePosition = new Vector3(30, 116);
            YInput.size = new Vector2(148, 24);
            YInput.horizontalAlignment = UIHorizontalAlignment.Left;
            YInput.tabIndex = 2;
            //YInput.eventTextSubmitted += (UIComponent component, string value) =>
            //{
            //    Debug.Log($"{YInput.text}");
            //};

            ALabel = Panel.AddUIComponent<UILabel>();
            ALabel.relativePosition = new Vector3(8, 154);
            ALabel.text = "A:";
            ALabel.tooltip = "Angle";
            AInput = UIUtils.CreateTextField(Panel);
            AInput.relativePosition = new Vector3(30, 150);
            AInput.size = new Vector2(100, 24);
            AInput.horizontalAlignment = UIHorizontalAlignment.Left;
            AInput.tabIndex = 3;
            //AInput.eventTextSubmitted += (UIComponent component, string value) =>
            //{
            //    Debug.Log($"{AInput.text}");
            //};

            Submit = UIUtils.CreateButton(Panel);
            Submit.relativePosition = new Vector3(138, 146);
            Submit.size = new Vector2(40, 30);
            Submit.text = "Go";
            Submit.tabIndex = 4;
            Submit.eventClicked += (UIComponent c, UIMouseEventParameter p) =>
            {
                Debug.Log($"{XInput.text},{ZInput.text},{YInput.text},{AInput.text}");
            };
        }
    }
}
