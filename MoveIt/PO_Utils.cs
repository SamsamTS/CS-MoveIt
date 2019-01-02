using ProceduralObjects;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace MoveIt
{
    internal static class PO_Logic
    {
        private static ProceduralObjectsLogic logic = null;

        private static ProceduralObjectsLogic Logic
        {
            get
            {
                if (logic == null)
                    logic = Object.FindObjectOfType<ProceduralObjectsLogic>();
                return logic;
            }
        }
       
        public static PO_Object GetProcObj(uint id)
        {
            foreach (ProceduralObjects.Classes.ProceduralObject obj in Logic.proceduralObjects)
            {
                if (obj.id == id - 1)
                {
                    return new PO_Object(obj);
                }
            }

            throw new System.Exception($"Id {id} (actual:{id-1}) not found!");
        }
    }

    internal class PO_Object
    {
        private ProceduralObjects.Classes.ProceduralObject procObj;
        private uint id; // The NetLane value, object.id+1
        public uint Id { get => id; set => id = value; }
        private int ProcId { get => (int)id - 1; set => id = (uint)value + 1; }

        public PO_Object(ProceduralObjects.Classes.ProceduralObject obj)
        {
            procObj = obj;
            ProcId = obj.id;
        }

        public Vector3 Position { get => procObj.m_position; set => procObj.m_position = value; }
        private Quaternion Rotation { get => procObj.m_rotation; set => procObj.m_rotation = value; }

        public float GetAngleRadY()
        {
            Rotation.ToAngleAxis(out float a, out Vector3 devNull);
            return a * Mathf.Deg2Rad;
        }

        public void SetAngleDeltaRadY(float a)
        {
            a *= Mathf.Rad2Deg;
            while (a < 0f) a += 360f;
            while (a >= 360f) a -= 360f;
            Rotation = Rotation.Rotate(0, a, 0);
        }

        public void SetAngleRadY(float a)
        {
            SetAngleDeltaRadY(a - GetAngleRadY());
        }

        public void SetPositionY(float h)
        {
            procObj.m_position.y = h;
        }

        public string DebugQuaternion()
        {
            return $"{Id}:{Rotation.w},{Rotation.x},{Rotation.y},{Rotation.z}";
        }
    }

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
            float size = 8f * scale;// Mathf.Max(8, 8) * scale;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, position, size, position.y - 100f, position.y + 100f, renderLimits: false, alphaBlend: true);
            
        }
    }
}
