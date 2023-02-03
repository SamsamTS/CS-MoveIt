using ColossalFramework;
using ColossalFramework.UI;
using MoveIt.GUI;
using MoveIt.Lang;
using System;
using System.IO;
using UnityEngine;
using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UISaveWindow : XMLWindow
    {
        public static readonly SavedInt saveWindowX = new SavedInt("saveWindowX", Settings.settingsFileName, -1000, true);
        public static readonly SavedInt saveWindowY = new SavedInt("saveWindowY", Settings.settingsFileName, -1000, true);

        public UIButton saveButton;
        public UIPanel savePanel;

        public override void Start()
        {
            name = "MoveIt_SaveWindow";
            atlas = UIUtils.GetAtlas("Ingame");
            backgroundSprite = "SubcategoriesPanel";
            size = new Vector2(690, 372); // 180
            canFocus = true;

            UIDragHandle dragHandle = AddDragHandle();

            AddCloseButton();

            AddLabel(Str.xml_Export);

            // Save Panel
            UIPanel savePanel = AddUIComponent<UIPanel>();
            savePanel.atlas = atlas;
            savePanel.backgroundSprite = "GenericPanel";
            savePanel.color = new Color32(206, 206, 206, 255);
            savePanel.size = new Vector2(width - 16, 46);
            savePanel.relativePosition = new Vector2(8, 28);

            // Input
            fileNameInput = UIUtils.CreateTextField(savePanel);
            fileNameInput.padding.top = 7;
            fileNameInput.horizontalAlignment = UIHorizontalAlignment.Left;
            fileNameInput.relativePosition = new Vector3(8, 8);

            fileNameInput.eventKeyDown += (c, p) =>
            {
                if (p.keycode == KeyCode.Return)
                {
                    saveButton.SimulateClick();
                }
            };

            // Save
            saveButton = UIUtils.CreateButton(savePanel);
            saveButton.name = "MoveIt_SaveButton";
            saveButton.text = Str.xml_Export;
            saveButton.size = new Vector2(100f, 30f);
            saveButton.relativePosition = new Vector3(savePanel.width - saveButton.width - 8, 8);

            fileNameInput.size = new Vector2(saveButton.relativePosition.x - 16f, 30f);

            AddSortingPanel(savePanel.relativePosition.y + savePanel.height + 8);

            // FastList
            fastList = AddUIComponent<UIFastList>();
            fastList.rowHeight = 46f;
            fastList.atlas = atlas;
            fastList.backgroundSprite = "UnlockingPanel";
            fastList.width = width - 16;
            fastList.height = fastList.rowHeight * 9;
            fastList.canSelect = true;
            fastList.relativePosition = new Vector3(8, sortPanel.relativePosition.y + sortPanel.height + 8);

            saveButton.eventClicked += (c, p) =>
            {
                string filename = fileNameInput.text.Trim();
                filename = String.Concat(filename.Split(Path.GetInvalidFileNameChars()));

                if (!filename.IsNullOrWhiteSpace())
                {
                    Export(filename);
                }
            };

            height = fastList.relativePosition.y + fastList.height + 8;
            dragHandle.size = size;
            absolutePosition = new Vector3(saveWindowX.value, saveWindowY.value);
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

            fastList.listPosition = scrollPos;
            BringToFront();
            fileNameInput.Focus();
        }

        public static void Export(string filename)
        {
            string file = Path.Combine(MoveItTool.saveFolder, filename + ".xml");

            if (File.Exists(file))
            {
                ConfirmPanel.ShowModal(Str.xml_OverwriteTitle, String.Format(Str.xml_OverwriteMessage, filename), (comp, ret) =>
                {
                    if (ret == 1)
                    {
                        MoveItTool.instance.Export(filename);
                        instance.RefreshFileList();
                        instance.fileNameInput.Focus();
                        instance.fileNameInput.SelectAll();
                    }
                });
            }
            else
            {
                MoveItTool.instance.Export(filename);
                instance.RefreshFileList();
                instance.fileNameInput.Focus();
                instance.fileNameInput.SelectAll();
            }
        }

        public static void Open()
        {
            if (instance == null)
            {
                instance = UIView.GetAView().AddUIComponent(typeof(UISaveWindow)) as UISaveWindow;
                UIView.PushModal(instance);
            }
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged(saveWindowY, saveWindowY);
        }

        public override void RefreshFileList()
        {
            base.RefreshFileList();

            fileNameInput.Focus();
            fileNameInput.SelectAll();
        }
    }
}
