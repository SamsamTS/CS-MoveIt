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

        public static UIPopupPanel instance;

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

        public static void Open(Type type)
        {
            if (instance == null)
            {
                instance = UIView.GetAView().AddUIComponent(type) as UIPopupPanel;
                UIView.PushModal(instance);
            }
        }

        public static void Close()
        {
            if (instance != null)
            {
                UIView.PopModal();

                UIComponent modalEffect = instance.GetUIView().panelsLibraryModalEffect;
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

                instance.isVisible = false;
                Destroy(instance.gameObject);
                this = null;
            }
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


    public class UIChangesWindow : UIPopupPanel
    {
        public override void Start()
        {
            base.Start();

            blurb.text = Str.whatsNew;

            CloseButton();
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
                Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=1619685021");
            };
        }
    }





    //public class UIPopupPanel : UIPanel
    //{
    //    public static UIPopupPanel instance;
    //    private string blurb = "";
    //    private float extraPadding = 5f;
    //    private UIButton closeBtn;

    //    public override void Start()
    //    {
    //        name = "FindIt_UpdateNoticePopUp";
    //        atlas = UIUtils.GetAtlas("Ingame");
    //        backgroundSprite = "GenericPanelWhite";
    //        size = new Vector2(500, 240);

    //        UILabel title = AddUIComponent<UILabel>();
    //        title.text = "Move It";
    //        title.textColor = new Color32(0, 0, 0, 255);
    //        title.relativePosition = new Vector3(extraPadding * 2, extraPadding * 2);

    //        UIButton close = AddUIComponent<UIButton>();
    //        close.size = new Vector2(30f, 30f);
    //        close.text = "X";
    //        close.textScale = 0.9f;
    //        close.textColor = new Color32(0, 0, 0, 255);
    //        close.focusedTextColor = new Color32(0, 0, 0, 255);
    //        close.hoveredTextColor = new Color32(109, 109, 109, 255);
    //        close.pressedTextColor = new Color32(128, 128, 128, 102);
    //        close.textPadding = new RectOffset(8, 8, 8, 8);
    //        close.canFocus = false;
    //        close.playAudioEvents = true;
    //        close.relativePosition = new Vector3(width - close.width, 0);
    //        close.eventClicked += (c, p) => Close();

    //        UILabel message = AddUIComponent<UILabel>();
    //        message.text = "\n" + blurb;
    //        message.textColor = new Color32(0, 0, 0, 255);
    //        message.relativePosition = new Vector3(extraPadding * 4, extraPadding + title.height + extraPadding);

    //        closeBtn = SamsamTS.UIUtils.CreateButton(this);
    //        closeBtn.size = new Vector2(100, 40);
    //        closeBtn.text = "Close";
    //        closeBtn.relativePosition = new Vector3(extraPadding * 2, message.relativePosition.y + message.height + extraPadding * 2);
    //        closeBtn.eventClick += (c, p) =>
    //        {
    //            Close();
    //        };

    //        height = closeBtn.relativePosition.y + closeBtn.height + 10;
    //        width = message.width + 40;
    //        close.relativePosition = new Vector3(width - close.width, 0);
    //        closeBtn.Focus();
    //    }

    //    private static void Close()
    //    {
    //        if (instance != null)
    //        {
    //            // UIView.PopModal();
    //            instance.isVisible = false;
    //            Destroy(instance.gameObject);
    //            instance = null;
    //        }
    //    }

    //    protected override void OnKeyDown(UIKeyEventParameter p)
    //    {
    //        if (Input.GetKey(KeyCode.Escape))
    //        {
    //            p.Use();
    //            Close();
    //        }

    //        base.OnKeyDown(p);
    //    }

    //    public static void ShowAt()
    //    {
    //        if (instance == null)
    //        {
    //            instance = UIView.GetAView().AddUIComponent(typeof(UIPopupPanel)) as UIPopupPanel;
    //            instance.Show(true);
    //        }
    //        else
    //        {
    //            instance.Show(true);
    //        }
    //    }

    //    private Vector3 deltaPosition;
    //    protected override void OnMouseDown(UIMouseEventParameter p)
    //    {
    //        if (p.buttons.IsFlagSet(UIMouseButton.Right))
    //        {
    //            Vector3 mousePosition = Input.mousePosition;
    //            mousePosition.y = m_OwnerView.fixedHeight - mousePosition.y;
    //            deltaPosition = absolutePosition - mousePosition;

    //            BringToFront();
    //        }
    //    }

    //    protected override void OnMouseMove(UIMouseEventParameter p)
    //    {
    //        if (p.buttons.IsFlagSet(UIMouseButton.Right))
    //        {
    //            Vector3 mousePosition = Input.mousePosition;
    //            mousePosition.y = m_OwnerView.fixedHeight - mousePosition.y;
    //            absolutePosition = mousePosition + deltaPosition;
    //        }
    //    }
    //}






    //public class UIPopupPanel : UILabel
    //{
    //    public static UIPopupPanel instance;

    //    internal string m_blurb = "";

    //    public override void Start()
    //    {
    //        atlas = UIUtils.GetAtlas("Ingame");
    //        backgroundSprite = "GenericPanelWhite";

    //        size = new Vector2(500, 300);
    //        padding = new RectOffset(10, 10, 10, 10);
    //        textColor = new Color32(0, 0, 0, 255);
    //        textScale = 0.9f;

    //        wordWrap = true;
    //        autoHeight = true;

    //        instance = this;
    //        text = m_blurb;

    //        UIButton close = new UIButton
    //        {
    //            name = "close",
    //            width = 100,
    //            height = 30,
    //            absolutePosition = new Vector2(instance.width / 2 + 50, instance.height - 50)
    //        };

    //        close.eventClicked += (c, p) =>
    //        {
    //            instance.isVisible = false;
    //        };
    //    }

    //    protected override void OnMouseEnter(UIMouseEventParameter p)
    //    {
    //        textColor = new Color32(99, 99, 99, 255);
    //    }

    //    protected override void OnMouseLeave(UIMouseEventParameter p)
    //    {
    //        textColor = new Color32(0, 0, 0, 255);
    //    }

    //    protected override void OnVisibilityChanged()
    //    {
    //        if (isVisible)
    //        {
    //            RefreshPosition();
    //        }
    //        base.OnVisibilityChanged();
    //    }

    //    protected override void OnSizeChanged()
    //    {
    //        RefreshPosition();
    //    }

    //    public void RefreshPosition()
    //    {
    //        float x = (GetUIView().GetScreenResolution().x / 2) - (width / 2);
    //        float y = (GetUIView().GetScreenResolution().y / 2) - (height / 2) - 50;

    //        absolutePosition = new Vector3(x, y);
    //    }

    //    protected override void OnClick(UIMouseEventParameter p)
    //    {
    //        //isVisible = false;
    //        //MoveItTool.hideChangesWindow.value = true;
    //        // Close and disable
    //    }
    //}

    //public class UIChangesWindow : UIPopupPanel
    //{
    //    //"New in Move It 2.8.0:\n\n" +
    //    //"- Slope Align now automatically uses the 2 furthest apart select objects (Shift+Click on the tool icon to manually select the 2 points).\n\n" +
    //    //"- Toolbox (More Tools) menu redesigned, and some icons changed.\n\n" + 
    //    //"- Line Tool - Evenly space out objects in a straight line.\n\n" +
    //    //"- Set Position Tool - Change a selection's coordinates.\n\n" +
    //    //"- Move and delete paths for ship, aircraft, ferries, helicopters, etc.\n\n" +
    //    //"- Options page redesigned.\n\n" +
    //    //"- Many other tweaks and optimisations!\n\n" +
    //    //"Read the workshop Move It Guide for more information.\n\n" +
    //    //"Click anywhere on this box to close it.";

    //    public override void Start()
    //    {
    //        m_blurb = Str.whatsNew;

    //        base.Start();
    //    }
    //}
}
