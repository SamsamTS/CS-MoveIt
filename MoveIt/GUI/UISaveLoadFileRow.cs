using UnityEngine;
using ColossalFramework.UI;
using MoveIt.Localization;
using System;

using UIUtils = SamsamTS.UIUtils;
using MoveIt.GUI;

namespace MoveIt
{
    public class UISaveLoadFileRow : UIPanel, IUIFastListRow<FileData>
    {
        public UILabel fileNameLabel, fileDateLabel, fileSizeLabel;

        public UIButton loadToPosition;
        public UIButton saveLoadButton;
        public UIButton deleteButton;

        private UIPanel m_background;
        private Color activeColor = Color.white;
        private Color inactiveColor = new Color(0.8f, 0.8f, 0.8f);

        private bool IsExport => UISaveLoadWindow.instance is UISaveWindow;

        public UIPanel background
        {
            get
            {
                if (m_background == null)
                {
                    m_background = AddUIComponent<UIPanel>();
                    m_background.atlas = UIUtils.GetAtlas("Ingame");
                    m_background.width = width;
                    m_background.height = 48;
                    m_background.relativePosition = Vector2.zero;

                    m_background.zOrder = 0;
                }

                return m_background;
            }
        }

        public override void Awake()
        {
            height = 46;

            fileNameLabel = AddUIComponent<UILabel>();
            fileNameLabel.textScale = 0.9f;
            fileNameLabel.autoSize = false;
            fileNameLabel.height = 30;
            fileNameLabel.verticalAlignment = UIVerticalAlignment.Middle;
            fileNameLabel.relativePosition = new Vector3(46, 10);
            fileNameLabel.textColor = inactiveColor;

            fileDateLabel = AddUIComponent<UILabel>();
            fileDateLabel.textScale = 0.8f;
            fileDateLabel.autoSize = false;
            fileDateLabel.size = new Vector2(80f, 30f);
            fileDateLabel.height = 30;
            fileDateLabel.verticalAlignment = UIVerticalAlignment.Middle;
            fileDateLabel.textAlignment = UIHorizontalAlignment.Right;
            fileDateLabel.relativePosition = new Vector3(56, 10);
            fileDateLabel.textColor = activeColor;

            fileSizeLabel = AddUIComponent<UILabel>();
            fileSizeLabel.textScale = 0.8f;
            fileSizeLabel.autoSize = false;
            fileSizeLabel.size = new Vector2(70f, 30f);
            fileSizeLabel.height = 30;
            fileSizeLabel.verticalAlignment = UIVerticalAlignment.Middle;
            fileSizeLabel.textAlignment = UIHorizontalAlignment.Right;
            fileSizeLabel.relativePosition = new Vector3(56, 10);
            fileSizeLabel.textColor = inactiveColor;

            deleteButton = UIUtils.CreateButton(this);
            deleteButton.name = "MoveIt_DeleteFileButton";
            deleteButton.text = "X";
            deleteButton.size = new Vector2(30f, 30f);
            deleteButton.relativePosition = new Vector3(8, 8);
            deleteButton.tooltip = Str.xml_DeleteLabel;

            saveLoadButton = UIUtils.CreateButton(this);
            saveLoadButton.name = "MoveIt_SaveLoadFileButton";
            saveLoadButton.text = IsExport ? Str.xml_Export : Str.xml_Import;
            saveLoadButton.size = new Vector2(70f, 30f);
            saveLoadButton.textScale = 0.9f;
            saveLoadButton.relativePosition = new Vector3(UISaveLoadWindow.instance.width - saveLoadButton.size.x - 42, 8);

            if (!IsExport) // Importing
            {
                loadToPosition = UIUtils.CreateButton(this);
                loadToPosition.name = "MoveIt_loadToPosition";
                loadToPosition.text = Str.xml_Restore;
                loadToPosition.tooltip = Str.xml_Restore_Tooltip;
                loadToPosition.size = new Vector2(70f, 30f);
                loadToPosition.textScale = 0.9f;
                loadToPosition.relativePosition = new Vector3(saveLoadButton.relativePosition.x - loadToPosition.width - 8, 8);

                loadToPosition.eventClicked += (c, p) =>
                {
                    UIView.Find("DefaultTooltip")?.Hide();
                    UILoadWindow.Close();
                    Destroy(loadToPosition);
                    MoveItTool.instance.Restore(fileNameLabel.text);
                };
            }
            else
            {
                loadToPosition = null;
            }

            saveLoadButton.eventClicked += (c, p) =>
            {
                UIView.Find("DefaultTooltip")?.Hide();
                if (IsExport)
                {
                    UISaveWindow.Export(fileNameLabel.text);
                }
                else
                {
                    UILoadWindow.Close();
                    MoveItTool.instance.Import(fileNameLabel.text);
                }
            };

            deleteButton.eventClicked += (c, p) =>
            {
                ConfirmPanel.ShowModal(Str.xml_DeleteConfirmTitle, String.Format(Str.xml_DeleteConfirmMessage, fileNameLabel.text), (comp, ret) =>
                {
                    if (ret == 1)
                    {
                        MoveItTool.instance.Delete(fileNameLabel.text);

                        UISaveLoadWindow.instance.RefreshFileList();
                    }
                });
            };

            fileDateLabel.relativePosition = new Vector3((IsExport ? saveLoadButton : loadToPosition).relativePosition.x - fileDateLabel.width - 8f, 8);
            fileSizeLabel.relativePosition = new Vector3(fileDateLabel.relativePosition.x - 4f - fileSizeLabel.width, 8);
            fileNameLabel.width = fileSizeLabel.relativePosition.x - 8f - fileNameLabel.relativePosition.x;
        }

        public void Display(FileData data, int i)
        {
            fileNameLabel.text = data.m_name;
            fileSizeLabel.text = data.GetSize();
            fileDateLabel.text = data.GetDate();
            fileDateLabel.tooltip = data.GetDateExtended();

            fileNameLabel.textColor = inactiveColor;
            fileSizeLabel.textColor = inactiveColor;
            fileDateLabel.textColor = inactiveColor;
            switch (MoveItTool.sortType)
            {
                case UISaveLoadWindow.SortTypes.Name:
                    fileNameLabel.textColor = activeColor;
                    break;

                case UISaveLoadWindow.SortTypes.Size:
                    fileSizeLabel.textColor = activeColor;
                    break;

                default:
                    fileDateLabel.textColor = activeColor;
                    break;
            }

            if (i % 2 == 1)
            {
                background.backgroundSprite = "UnlockingItemBackground";
                background.color = new Color32(0, 0, 0, 128);
                background.width = parent.width;
            }
            else
            {
                background.backgroundSprite = null;
            }
        }

        public void Select(bool isRowOdd)
        {

        }
        public void Deselect(bool isRowOdd)
        {

        }
    }
}
