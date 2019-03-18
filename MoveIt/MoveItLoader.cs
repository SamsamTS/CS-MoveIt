using ICities;
using UnityEngine;

namespace MoveIt
{
    public class MoveItLoader : LoadingExtensionBase
    {
        public static bool IsGameLoaded { get; private set; } = false;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
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
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();

                MoveItTool.instance = toolController.gameObject.AddComponent<MoveItTool>();

                MoveItTool.stepOver = new StepOver();

                UIFilters.FilterCBs.Clear();
                UIFilters.NetworkCBs.Clear();

                MoveItTool.filterBuildings = true;
                MoveItTool.filterProps = true;
                MoveItTool.filterDecals = true;
                MoveItTool.filterSurfaces = true;
                MoveItTool.filterTrees = true;
                MoveItTool.filterNodes = true;
                MoveItTool.filterSegments = true;
                MoveItTool.filterNetworks = false;
            }

            IsGameLoaded = true;
        }

        public static void UninstallMod()
        {
            MoveItTool.m_debugPanel = null;
            UIToolOptionPanel.instance = null;
            UIAlignTools.AlignToolsPanel = null;
            UIAlignTools.AlignToolsBtn = null;
            Action.selection.Clear();

            if (MoveItTool.instance != null)
            {
                MoveItTool.instance.enabled = false;
            }

            IsGameLoaded = false;
        }
    }
}
