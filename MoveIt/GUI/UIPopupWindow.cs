using ColossalFramework;
using ColossalFramework.UI;
using MoveIt.Localization;
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public abstract class UIPopupPanel : UIPanel
    {
        public UIButton closeTop, closeBottom;
        public UILabel blurb;
        public float sizeX = 465f, sizeY = 292f;
        public string title = "Move It";

        public override void Start()
        {
            name = "MoveIt_PopupWindow";
            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "SubcategoriesPanel";
            size = new Vector2(sizeX, sizeY);
            canFocus = true;
            absolutePosition = new Vector3(-1000f, -1000f);
            autoLayout = false;

            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.target = parent;
            dragHandle.relativePosition = Vector3.zero;

            closeTop = AddUIComponent<UIButton>();
            closeTop.size = new Vector2(30f, 30f);
            closeTop.text = "X";
            closeTop.textScale = 0.9f;
            closeTop.textColor = new Color32(118, 123, 123, 255);
            closeTop.focusedTextColor = new Color32(118, 123, 123, 255);
            closeTop.hoveredTextColor = new Color32(140, 142, 142, 255);
            closeTop.pressedTextColor = new Color32(99, 102, 102, 102);
            closeTop.textPadding = new RectOffset(8, 8, 8, 8);
            closeTop.canFocus = false;
            closeTop.playAudioEvents = true;
            closeTop.relativePosition = new Vector3(width - closeTop.width, 0);

            UILabel label = AddUIComponent<UILabel>();
            label.textScale = 0.9f;
            label.text = title;
            label.name = "MoveItPopupTitle";
            label.relativePosition = new Vector2(8, 8);
            label.SendToBack();

            blurb = AddUIComponent<UILabel>();
            blurb.autoSize = false;
            blurb.name = "MoveItPopupBlurb";
            blurb.text = "";
            blurb.wordWrap = true;
            blurb.backgroundSprite = "UnlockingPanel";
            blurb.color = new Color32(206, 206, 206, 255);
            blurb.size = new Vector2(width - 10, 214);
            blurb.relativePosition = new Vector2(5, 28);
            blurb.padding = new RectOffset(6, 6, 8, 8);
            blurb.atlas = atlas;
            blurb.SendToBack();

            closeTop.eventClicked += (c, p) =>
            {
                Close();
            };

            BringToFront();
        }

        internal void CloseButton()
        {
            closeBottom = UIUtils.CreateButton(this);
            closeBottom.text = "Close";
            closeBottom.playAudioEvents = true;
            closeBottom.relativePosition = new Vector3(width / 2 - closeBottom.width / 2, height - 40);

            closeBottom.eventClicked += (c, p) =>
            {
                Close();
            };
        }

        public static UIPopupPanel Open(Type type)
        {
            UIPopupPanel instance = UIView.GetAView().AddUIComponent(type) as UIPopupPanel;
            UIView.PushModal(instance);
            return instance;
        }

        public void Close()
        {
            UIView.PopModal();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;
            if (modalEffect != null && modalEffect.isVisible)
            {
                ValueAnimator.Animate("ModalEffect", delegate (float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate
                {
                    modalEffect.Hide();
                });
            }

            isVisible = false;
            Destroy(this.gameObject);
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                p.Use();
                Close();
            }

            base.OnKeyDown(p);
        }

        protected override void OnPositionChanged()
        {
            Vector2 resolution = GetUIView().GetScreenResolution();

            if (absolutePosition.x == -1000)
            {
                absolutePosition = new Vector2((resolution.x - width) / 2, (resolution.y - height) / 2);
                MakePixelPerfect();
            }

            absolutePosition = new Vector2(
                (int)Mathf.Clamp(absolutePosition.x, 0, resolution.x - width),
                (int)Mathf.Clamp(absolutePosition.y, 0, resolution.y - height));

            base.OnPositionChanged();
        }

        public void RefreshPosition()
        {
            float x = (GetUIView().GetScreenResolution().x / 2) - (width / 2);
            float y = (GetUIView().GetScreenResolution().y / 2) - (height / 2) - 50;

            absolutePosition = new Vector3(x, y);
        }
    }


    public class UIPopupWindow : UIPopupPanel
    {
        public override void Start()
        {
            base.Start();

            height += 70;
            blurb.height += 70;
            blurb.text = Str.whatsNew;

            CloseButton();
            MoveItTool.hideChangesWindow.value = true;
        }
    }

    public class UILSMWarning : UIPopupPanel
    {
        public override void Start()
        {
            base.Start();

            blurb.text = Str.lsmWarning;

            UIButton button = UIUtils.CreateButton(this);
            button.autoSize = false;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.size = new Vector2(250, 30);
            button.text = Str.lsmWorkshopBtn;
            button.tooltip = Str.lsmWorkshopBtn_Tooltip;
            button.relativePosition = new Vector3(width / 2 - button.width / 2, height - 40);
            button.eventClicked += (c, p) =>
            {
                Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=2858591409");
            };
        }
    }
}
