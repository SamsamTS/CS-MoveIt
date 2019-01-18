using ICities;
using UnityEngine;

namespace MoveIt
{
    public class MoveItLoader : LoadingExtensionBase
    {
        private static bool isGameLoaded = false;
        public static bool IsGameLoaded {
            get => isGameLoaded;
            private set => isGameLoaded = value;
        }

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
                Statistics.counters.Clear();

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
            if (MoveItTool.instance != null)
            {
                MoveItTool.instance.enabled = false;
            }

            //MoveItTool.debugPanel.Panel.parent.RemoveUIComponent(MoveItTool.debugPanel.Panel);
            IsGameLoaded = false;
        }
    }
}
