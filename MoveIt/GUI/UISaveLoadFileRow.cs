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
        public UILabel fileNameLabel;
        public UILabel fileDateLabel;

        public UIButton loadToPosition;
        public UIButton saveLoadButton;
        public UIButton deleteButton;

        private UIPanel m_background;
        private Color active = Color.white;
        private Color inactive = new Color(0, 225, 225);

        private bool IsExport => UISaveLoadWindow.instance is UISaveWindow;

        public UIPanel background
        {
            get
            {
                if (m_background == null)
                {
                    m_background = AddUIComponent<UIPanel>();
                    m_background.width = width;
                    m_background.height = 40;
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
            fileNameLabel.relativePosition = new Vector3(56, 8);
            fileNameLabel.textColor = inactive;

            fileDateLabel = AddUIComponent<UILabel>();
            fileDateLabel.textScale = 0.9f;
            fileDateLabel.autoSize = false;
            fileDateLabel.size = new Vector2(100f, 30f);
            fileDateLabel.height = 30;
            fileDateLabel.verticalAlignment = UIVerticalAlignment.Middle;
            fileDateLabel.textAlignment = UIHorizontalAlignment.Right;
            fileDateLabel.relativePosition = new Vector3(56, 8);
            fileDateLabel.textColor = active;

            deleteButton = UIUtils.CreateButton(this);
            deleteButton.name = "MoveIt_DeleteFileButton";
            deleteButton.text = "X";
            deleteButton.size = new Vector2(40f, 30f);
            deleteButton.relativePosition = new Vector3(8 /*(IsExport ? 430 : 510) - deleteButton.width - 8*/, 8);
            deleteButton.tooltip = Str.xml_DeleteLabel;

            saveLoadButton = UIUtils.CreateButton(this);
            saveLoadButton.name = "MoveIt_SaveLoadFileButton";
            saveLoadButton.text = UISaveLoadWindow.instance != null ? Str.xml_Export : Str.xml_Import;
            saveLoadButton.size = new Vector2(80f, 30f);
            saveLoadButton.relativePosition = new Vector3(520, 8);

            if (!IsExport) // Importing
            {
                loadToPosition = UIUtils.CreateButton(this);
                loadToPosition.name = "MoveIt_loadToPosition";
                loadToPosition.text = Str.xml_Restore;
                loadToPosition.tooltip = Str.xml_Restore_Tooltip;
                loadToPosition.size = new Vector2(80f, 30f);
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

            if (!IsExport) // Importing
            {
                fileDateLabel.relativePosition = new Vector3(loadToPosition.relativePosition.x - 108f, 8);
            }
            else
            {
                fileDateLabel.relativePosition = new Vector3(saveLoadButton.relativePosition.x - 108f, 8);
            }
            fileNameLabel.width = fileDateLabel.relativePosition.x - 8f - fileNameLabel.relativePosition.x;
        }

        public void Display(FileData data, int i)
        {
            fileNameLabel.text = data.Name;
            fileDateLabel.text = data.Date.ToShortDateString();

            if (UISaveLoadWindow.instance.sortType == UISaveLoadWindow.sortTypes.Date)
            {
                fileNameLabel.textColor = inactive;
                fileDateLabel.textColor = active;
            }
            else
            {
                fileNameLabel.textColor = active;
                fileDateLabel.textColor = inactive;
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
