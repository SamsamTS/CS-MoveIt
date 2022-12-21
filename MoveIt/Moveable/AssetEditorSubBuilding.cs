using ColossalFramework.UI;
using QCommonLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.Threading;

namespace MoveIt
{
    public class AssetEditorSubBuilding
    {
        internal static readonly Vector3 m_heightOffset = new Vector3(0, -60, 0);
        internal BuildingInfo.SubInfo m_info = null;
        internal BuildingInfo EditPrefabInfo => ToolsModifierControl.toolController.m_editPrefabInfo as BuildingInfo;

        internal static DecorationPropertiesPanel PropertiesPanel => UnityEngine.Object.FindObjectOfType<DecorationPropertiesPanel>();
        internal static FieldInfo SubBuildingsField => PropertiesPanel.GetType().GetField("m_SubBuildings", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static Dictionary<ushort, BuildingInfo.SubInfo> SubBuildingsDict => (Dictionary<ushort, BuildingInfo.SubInfo>)SubBuildingsField.GetValue(PropertiesPanel);

        public AssetEditorSubBuilding(InstanceID instanceID)
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;

            if (EditPrefabInfo != null && EditPrefabInfo.m_subBuildings.Length > 0)
            {
                foreach (var sub in SubBuildingsDict)
                {
                    if (sub.Key == instanceID.Building)
                    {
                        m_info = sub.Value;
                    }
                }
            }
        }

        public void Create(ushort id, float angle, BuildingInfo info, bool fixedheight, Vector3 position)
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;

            m_info = new BuildingInfo.SubInfo()
            {
                m_angle = Angle(angle),
                m_buildingInfo = info,
                m_fixedHeight = fixedheight,
                m_position = Position(position)
            };

            SubBuildingsDict.Add(id, m_info);

            UpdatePanel();
        }

        public void Destroy(ushort id)
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;
            if (m_info == null) return;

            SubBuildingsDict.Remove(id);
        }

        public void LoadFromState(BuildingState state)
        {
            Move(state.position, state.angle);
        }

        public void Move(Vector3 newLocation, float angle)
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;
            if (m_info == null) return;

            m_info.m_position = Position(newLocation);
            m_info.m_angle = Angle(angle);

            //Log.Debug($"AAA01 {m_info.m_buildingInfo.name} {m_info.m_position}/{m_info.m_angle}");
        }

        public void SetFixedHeight(bool isFixed)
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;
            if (m_info == null) return;

            m_info.m_fixedHeight = isFixed;
        }

        internal static void UpdatePanel()
        {
            if (QCommon.Scene != QCommon.SceneTypes.AssetEditor) return;
            if (PropertiesPanel == null) return;

            try
            {
                PropertiesPanel.Refresh();

                foreach (UIButton c in PropertiesPanel.GetComponentsInChildren<UIButton>())
                {
                    if (c.text == "Sub buildings")
                    {
                        c.SimulateClick();
                        break;
                    }
                }
            }
            catch { }
        }

        private static Vector3 Position(Vector3 original)
        {
            return original + m_heightOffset;
        }

        private static float Angle(float original)
        {
            return original * Mathf.Rad2Deg;
        }
    }
}
