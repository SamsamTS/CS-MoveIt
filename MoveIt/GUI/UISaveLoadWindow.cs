using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UIUtils = SamsamTS.UIUtils;
using UnityEngine;
using MoveIt.Localization;
using System.Collections;

namespace MoveIt.GUI
{
    public abstract class UISaveLoadWindow : UIPanel
    {
        public class UIFastList : UIFastList<FileData, UISaveLoadFileRow> { }
        public UIFastList fastList;

        public static UISaveLoadWindow instance;

        public UIPanel sortPanel;
        public UIButton openFolder, sortTypeBtn, sortOrderBtn;

        public UIButton close;

        public UITextField fileNameInput;

        public enum sortTypes
        {
            Name,
            Date
        }
        public enum sortOrders
        {
            Descending,
            Ascending
        }
        public sortTypes sortType;
        public sortOrders sortOrder;

        protected UIDragHandle AddDragHandle()
        {
            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.target = parent;
            dragHandle.relativePosition = Vector3.zero;
            return dragHandle;
        }

        protected void AddLabel(string label)
        {
            UILabel labelPnl = AddUIComponent<UILabel>();
            labelPnl.textScale = 0.9f;
            labelPnl.text = label;
            labelPnl.relativePosition = new Vector2(8, 8);
            labelPnl.SendToBack();
        }

        protected void AddCloseButton()
        {
            close = AddUIComponent<UIButton>();
            close.size = new Vector2(30f, 30f);
            close.text = "X";
            close.textScale = 0.9f;
            close.textColor = new Color32(118, 123, 123, 255);
            close.focusedTextColor = new Color32(118, 123, 123, 255);
            close.hoveredTextColor = new Color32(140, 142, 142, 255);
            close.pressedTextColor = new Color32(99, 102, 102, 102);
            close.textPadding = new RectOffset(8, 8, 8, 8);
            close.canFocus = false;
            close.playAudioEvents = true;
            close.relativePosition = new Vector3(width - close.width, 0);

            close.eventClicked += (c, p) =>
            {
                Close();
            };
        }

        protected void AddSortingPanel(float yPos)
        {
            // Sorting
            sortPanel = AddUIComponent<UIPanel>();
            sortPanel.atlas = atlas;
            sortPanel.color = new Color32(206, 206, 206, 255);
            sortPanel.size = new Vector2(width - 16, 40);
            sortPanel.relativePosition = new Vector2(8, yPos);

            // Open Folder
            openFolder = UIUtils.CreateButton(sortPanel);
            openFolder.name = "MoveIt_OpenFolderButton";
            openFolder.text = Str.xml_OpenFolder;
            openFolder.size = new Vector2(150f, 30f);
            openFolder.relativePosition = new Vector3(8, 8);

            sortOrderBtn = UIUtils.CreateButton(sortPanel);
            sortOrderBtn.name = "MoveIt_SortOrderButton";
            sortOrderBtn.text = Str.xml_Desc;
            sortOrderBtn.size = new Vector2(100f, 30f);
            sortOrderBtn.relativePosition = new Vector3(sortPanel.width - sortOrderBtn.width - 8, 8);

            sortTypeBtn = UIUtils.CreateButton(sortPanel);
            sortTypeBtn.name = "MoveIt_SortTypeButton";
            sortTypeBtn.text = Str.xml_Date;
            sortTypeBtn.size = new Vector2(100f, 30f);
            sortTypeBtn.relativePosition = new Vector3(sortOrderBtn.relativePosition.x - sortTypeBtn.width - 8, 8);

            sortType = sortTypes.Date;
            sortOrder = sortOrders.Descending;

            openFolder.eventClicked += (c, p) =>
            {
                Application.OpenURL(MoveItTool.saveFolder);
            };

            sortOrderBtn.eventClicked += (c, p) =>
            {
                sortOrder = sortOrder == sortOrders.Descending ? sortOrders.Ascending : sortOrders.Descending;
                sortOrderBtn.text = sortOrder == sortOrders.Descending ? Str.xml_Desc : Str.xml_Asc;

                RefreshFileList();
            };

            sortTypeBtn.eventClicked += (c, p) =>
            {
                sortType = sortType == sortTypes.Name ? sortTypes.Date : sortTypes.Name;
                sortTypeBtn.text = sortType == sortTypes.Date ? Str.xml_Date : Str.xml_Name;

                RefreshFileList();
            };
        }

        public static void Close()
        {
            if (instance != null)
            {
                UIView.PopModal();

                UIComponent modalEffect = instance.GetUIView().panelsLibraryModalEffect;
                if (modalEffect != null && modalEffect.isVisible)
                {
                    modalEffect.Hide();
                }

                instance.isVisible = false;
                Destroy(instance.gameObject);
                instance = null;
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

        protected void OnPositionChanged(SavedInt x, SavedInt y)
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

            x.value = (int)absolutePosition.x;
            y.value = (int)absolutePosition.y;

            base.OnPositionChanged();
        }

        public virtual void RefreshFileList()
        {
            fastList.rowsData.Clear();

            if (Directory.Exists(MoveItTool.saveFolder))
            {
                int i = 0;
                string[] fileList = Directory.GetFiles(MoveItTool.saveFolder, "*.xml");
                FileData[] files = new FileData[fileList.Length];

                foreach (string file in fileList)
                {
                    FileData data = new FileData()
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Date = File.GetLastWriteTime(file)
                    };
                    files[i++] = data;
                }

                if (sortType == sortTypes.Name)
                {
                    Array.Sort(files, new CompareName());
                }
                else if (sortType == sortTypes.Date)
                {
                    Array.Sort(files, new CompareDate());
                }
                if (sortOrder == sortOrders.Descending)
                {
                    Array.Reverse(files);
                }

                foreach (FileData file in files)
                {
                    fastList.rowsData.Add(file);
                }

                fastList.DisplayAt(0);
            }
        }
    }

    class CompareName : IComparer<FileData>
    {
        int IComparer<FileData>.Compare(FileData a, FileData b)
        {
            return ((new CaseInsensitiveComparer()).Compare(a.Name, b.Name));
        }
    }

    class CompareDate : IComparer<FileData>
    {
        int IComparer<FileData>.Compare(FileData a, FileData b)
        {
            if (a.Date > b.Date)
            {
                return 1;
            }
            return -1;
        }
    }
}
