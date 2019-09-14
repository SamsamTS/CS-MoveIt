using ICities;
using UnityEngine;

namespace MoveIt
{
    public class MoveItLoader : LoadingExtensionBase
    {
        public static bool IsGameLoaded { get; private set; } = false;
        public static LoadMode loadMode;
        private static GameObject DebugGameObject;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            loadMode = mode;
            InstallMod();
        }

        public override void OnLevelUnloading()
        {
            UninstallMod();
            base.OnLevelUnloading();
        }

        public static void InstallMod()
        {
            if (MoveItTool.instance == null)
            {
                // Creating the instance
                ToolController toolController = Object.FindObjectOfType<ToolController>();

                MoveItTool.instance = toolController.gameObject.AddComponent<MoveItTool>();
            }
            else
            {
                Debug.Log($"InstallMod with existing instance!");
            }

            MoveItTool.stepOver = new StepOver();
            MoveItTool.m_debugPanel = new DebugPanel();

            DebugGameObject = new GameObject("MIT_DebugPanel");
            DebugGameObject.AddComponent<DebugPanel>();
            MoveItTool.m_debugPanel = DebugGameObject.GetComponent<DebugPanel>();

            UIFilters.FilterCBs.Clear();
            UIFilters.NetworkCBs.Clear();

            Filters.Picker = new PickerFilter();

            MoveItTool.filterBuildings = true;
            MoveItTool.filterProps = true;
            MoveItTool.filterDecals = true;
            MoveItTool.filterSurfaces = true;
            MoveItTool.filterTrees = true;
            MoveItTool.filterNodes = true;
            MoveItTool.filterSegments = true;
            MoveItTool.filterNetworks = false;

            IsGameLoaded = true;
        }

        public static void UninstallMod()
        {
            MoveItTool.m_debugPanel = null;
            Object.Destroy(DebugGameObject);
            UIToolOptionPanel.instance = null;
            UIAlignTools.AlignToolsPanel = null;
            UIAlignTools.AlignToolsBtn = null;
            Action.selection.Clear();
            Filters.Picker = null;
            MoveItTool.PO = null;

            if (MoveItTool.instance != null)
            {
                MoveItTool.instance.enabled = false;
            }

            IsGameLoaded = false;
        }
    }
}
