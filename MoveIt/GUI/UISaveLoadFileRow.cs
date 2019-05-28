using UnityEngine;
using ColossalFramework.UI;

using UIUtils = SamsamTS.UIUtils;

namespace MoveIt
{
    public class UISaveLoadFileRow : UIPanel, IUIFastListRow<string>
    {
        public UILabel fileNameLabel;

        public UIButton loadToPosition;
        public UIButton saveLoadButton;
        public UIButton deleteButton;

        private UIPanel m_background;

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
            fileNameLabel.relativePosition = new Vector3(8, 8);

            deleteButton = UIUtils.CreateButton(this);
            deleteButton.name = "MoveIt_DeleteFileButton";
            deleteButton.text = "X";
            deleteButton.size = new Vector2(40f, 30f);
            deleteButton.relativePosition = new Vector3((UISaveWindow.instance != null ? 430 : 510) - deleteButton.width - 8, 8);
            deleteButton.tooltip = "Delete saved selection";

            saveLoadButton = UIUtils.CreateButton(this);
            saveLoadButton.name = "MoveIt_SaveLoadFileButton";
            saveLoadButton.text = UISaveWindow.instance != null ? "Export" : "Import";
            saveLoadButton.size = new Vector2(80f, 30f);
            saveLoadButton.relativePosition = new Vector3(deleteButton.relativePosition.x - saveLoadButton.width - 8, 8);

            if (UISaveWindow.instance == null) // Importing
            {
                loadToPosition = UIUtils.CreateButton(this);
                loadToPosition.name = "MoveIt_loadToPosition";
                loadToPosition.text = "Restore";
                loadToPosition.tooltip = "Import the selection to the position it was exported";
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
                if (UISaveWindow.instance != null)
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
                ConfirmPanel.ShowModal("Delete file", "Do you want to delete the file '" + fileNameLabel.text + "' permanently?", (comp, ret) =>
                {
                    if (ret == 1)
                    {
                        MoveItTool.instance.Delete(fileNameLabel.text);

                        if (UISaveWindow.instance != null)
                        {
                            UISaveWindow.instance.RefreshFileList();
                        }
                        else
                        {
                            UILoadWindow.instance.RefreshFileList();
                        }
                    }
                });
            };

            if (UISaveWindow.instance == null) // Importing
            {
                fileNameLabel.width = loadToPosition.relativePosition.x - 16f;
            }
            else
            {
                fileNameLabel.width = saveLoadButton.relativePosition.x - 16f;
            }
        }

        public void Display(string data, int i)
        {
            fileNameLabel.text = data;

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
