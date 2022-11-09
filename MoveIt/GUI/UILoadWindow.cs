﻿using ColossalFramework;
using ColossalFramework.UI;
using MoveIt.Localization;
using System.IO;
using UIUtils = SamsamTS.UIUtils;
using UnityEngine;
using MoveIt.GUI;

namespace MoveIt
{
    public class UILoadWindow : UISaveLoadWindow
    {
        public static readonly SavedInt loadWindowX = new SavedInt("loadWindowX", MoveItTool.settingsFileName, -1000, true);
        public static readonly SavedInt loadWindowY = new SavedInt("loadWindowY", MoveItTool.settingsFileName, -1000, true);

        public override void Start()
        {
            name = "MoveIt_SaveWindow";
            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "SubcategoriesPanel";
            size = new Vector2(645, 272);
            canFocus = true;

            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.target = parent;
            dragHandle.relativePosition = Vector3.zero;

            AddCloseButton();

            UILabel label = AddUIComponent<UILabel>();
            label.textScale = 0.9f;
            label.text = Str.xml_Import;
            label.relativePosition = new Vector2(8, 8);
            label.SendToBack();

            AddSortingPanel(28f);

            // FastList
            fastList = AddUIComponent<UIFastList>();
            fastList.backgroundSprite = "UnlockingPanel";
            fastList.width = width - 16;
            fastList.height = 46 * 7;
            fastList.canSelect = true;
            fastList.relativePosition = new Vector3(8, sortPanel.relativePosition.y + sortPanel.height + 8);

            fastList.rowHeight = 46f;

            height = fastList.relativePosition.y + fastList.height + 8;
            dragHandle.size = size;
            absolutePosition = new Vector3(loadWindowX.value, loadWindowY.value);
            MakePixelPerfect();

            RefreshFileList();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;
            if (modalEffect != null && !modalEffect.isVisible)
            {
                modalEffect.Show(false);
                ValueAnimator.Animate("ModalEffect", delegate (float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
            }

            BringToFront();
            Focus();
        }

        public static void Open()
        {
            if (instance == null)
            {
                instance = UIView.GetAView().AddUIComponent(typeof(UILoadWindow)) as UILoadWindow;
                UIView.PushModal(instance);
            }
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged(loadWindowY, loadWindowY);
        }

        public override void RefreshFileList()
        {
            base.RefreshFileList();

            Focus();
        }
    }
}
