using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Text.RegularExpressions;
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
            Initialise();
        }

        internal void Visible(bool show)
        {
            Panel.isVisible = show;
            if (show)
            {
                MoveToAction action = ActionQueue.instance.current as MoveToAction;
                action.Position = action.Original = Action.GetCenter();
                action.Angle = action.AngleOriginal = Action.GetAngle();

                action.HeightActive = false;
                foreach (Instance i in Action.selection)
                {
                    if (i is MoveableBuilding || i is MoveableProc || i is MoveableProp || i is MoveableNode || i is MoveableTree)
                    {
                        action.HeightActive = true;
                        break;
                    }
                }
                YLabel.isVisible = action.HeightActive;
                YInput.enabled = action.HeightActive;

                action.AngleActive = false;
                foreach (Instance i in Action.selection)
                {
                    if (i is MoveableBuilding || i is MoveableProc || i is MoveableProp)
                    {
                        action.AngleActive = true;
                        break;
                    }
                }
                ALabel.isVisible = action.AngleActive;
                AInput.enabled = action.AngleActive;

                UpdateValues();
            }
        }

        internal void UpdateValues()
        {
            if (!Panel.isVisible) return;

            MoveToAction action = ActionQueue.instance.current as MoveToAction;

            XInput.text = action.Position.x.ToString();
            YInput.text = action.Position.y.ToString();
            ZInput.text = action.Position.z.ToString();
            AInput.text = (action.Angle * Mathf.Rad2Deg).ToString();
        }

        internal void Go()
        {
            MoveToAction action = ActionQueue.instance.current as MoveToAction;

            float x = Mathf.Clamp(float.Parse(XInput.text), -8600, 8600);
            float y = action.HeightActive ? Mathf.Clamp(float.Parse(YInput.text), 0, 1024) : action.Original.y;
            float z = Mathf.Clamp(float.Parse(ZInput.text), -8600, 8600);
            float a = (float.Parse(AInput.text) * Mathf.Deg2Rad) % (Mathf.PI * 2);

            action.Position = new Vector3(x, y, z);
            action.Angle = a;
            
            action.moveDelta = action.Position - action.Original;
            if (action.AngleActive)
            {
                action.angleDelta = action.Angle - action.AngleOriginal;
            }

            ActionQueue.instance.Do();
            UpdateValues();
        }

        private void Initialise()
        {
            Panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Panel.name = "MoveIt_MoveToPanel";
            Panel.atlas = ResourceLoader.GetAtlas("Ingame");
            Panel.backgroundSprite = "MenuPanel2";
            Panel.size = new Vector2(190, 185);
            Panel.absolutePosition = new Vector3(Panel.GetUIView().GetScreenResolution().x - 220, Panel.GetUIView().GetScreenResolution().y - 600);
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
                MoveItTool.instance.DeactivateTool();
            };

            XLabel = Panel.AddUIComponent<UILabel>();
            XLabel.relativePosition = new Vector3(8, 52);
            XLabel.text = "X:";
            XInput = UIUtils.CreateTextField(Panel);
            XInput.relativePosition = new Vector3(30, 48);
            XInput.size = new Vector2(148, 24);
            XInput.horizontalAlignment = UIHorizontalAlignment.Left;
            XInput.tabIndex = 0;
            XInput.eventLostFocus += (UIComponent c, UIFocusEventParameter f) => { Validate(XInput); };

            ZLabel = Panel.AddUIComponent<UILabel>();
            ZLabel.relativePosition = new Vector3(8, 86);
            ZLabel.text = "Y:";
            ZInput = UIUtils.CreateTextField(Panel);
            ZInput.relativePosition = new Vector3(30, 82);
            ZInput.size = new Vector2(148, 24);
            ZInput.horizontalAlignment = UIHorizontalAlignment.Left;
            ZInput.tabIndex = 1;
            ZInput.eventLostFocus += (UIComponent c, UIFocusEventParameter f) => { Validate(ZInput); };

            YLabel = Panel.AddUIComponent<UILabel>();
            YLabel.relativePosition = new Vector3(8, 120);
            YLabel.text = "H:";
            YLabel.tooltip = "Height";
            YInput = UIUtils.CreateTextField(Panel);
            YInput.relativePosition = new Vector3(30, 116);
            YInput.size = new Vector2(148, 24);
            YInput.horizontalAlignment = UIHorizontalAlignment.Left;
            YInput.tabIndex = 2;
            YInput.eventLostFocus += (UIComponent c, UIFocusEventParameter f) => { Validate(YInput); };

            ALabel = Panel.AddUIComponent<UILabel>();
            ALabel.relativePosition = new Vector3(8, 154);
            ALabel.text = "A:";
            ALabel.tooltip = "Angle";
            AInput = UIUtils.CreateTextField(Panel);
            AInput.relativePosition = new Vector3(30, 150);
            AInput.size = new Vector2(100, 24);
            AInput.horizontalAlignment = UIHorizontalAlignment.Left;
            AInput.tabIndex = 3;
            AInput.eventLostFocus += (UIComponent c, UIFocusEventParameter f) => { Validate(AInput); };

            Submit = UIUtils.CreateButton(Panel);
            Submit.relativePosition = new Vector3(138, 146);
            Submit.size = new Vector2(40, 30);
            Submit.text = "Go";
            Submit.tabIndex = 4;
            Submit.eventClicked += (UIComponent c, UIMouseEventParameter p) =>
            {
                Go();
            };
        }

        private void Validate(UITextField textField)
        {
            string text = textField.text;
            text = Regex.Replace(text, @"[^0-9\-\.]", @"", RegexOptions.ECMAScript);
            text = text.Substring(0, 1) + Regex.Replace(text.Substring(1), @"[^0-9\.]", @"", RegexOptions.ECMAScript);
            if (text.IndexOf('.') > 0)
            {
                text = text.Substring(0, text.IndexOf('.') + 1) + Regex.Replace(text.Substring(text.IndexOf('.') + 1), @"[^0-9\-]", @"", RegexOptions.ECMAScript);
            }
            textField.text = text;
        }
    }
}
