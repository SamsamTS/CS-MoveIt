using ProceduralObjects;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace MoveIt
{
    static class PO_Utils
    {
        public static ProceduralObjectsLogic GetPOLogic()
        {
            return Object.FindObjectOfType<ProceduralObjectsLogic>();
        }

        public static ProceduralObjects.Classes.ProceduralObject GetProcWithId(this List<ProceduralObjects.Classes.ProceduralObject> list, int id)
        {
            if (list.Any(po => po.id == id))
            {
                return list.FirstOrDefault(po => po.id == id);
            }
            return null;
        }

        public static Quaternion Rotate(this Quaternion rot, float x, float y, float z)
        {
            var gObj = new GameObject("temp_obj");
            gObj.transform.rotation = rot;
            gObj.transform.Rotate(x, y, z);
            var newRot = gObj.transform.rotation;
            Object.Destroy(gObj);
            return newRot;
        }

        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo, Vector3 position, float scale, float angle, Color color)
        {
            float size = 8;// Mathf.Max(8, 8) * scale;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, position, size, position.y - 100f, position.y + 100f, renderLimits: false, alphaBlend: true);
            
        }
    }
}
