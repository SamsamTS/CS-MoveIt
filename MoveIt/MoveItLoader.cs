using ICities;
using UnityEngine;

namespace MoveIt
{
    public class MoveItLoader : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (MoveItTool.instance == null)
            {
                // Creating the instance
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();

                MoveItTool.instance = toolController.gameObject.AddComponent<MoveItTool>();
            }
        }

        public override void OnLevelUnloading()
        {
            if (MoveItTool.instance != null)
            {
                MoveItTool.instance.enabled = false;
            }
        }
    }
}
