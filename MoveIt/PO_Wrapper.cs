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

        public static List<PO_Object> Objects
        {
            get
            {
                List<PO_Object> list = new List<PO_Object>();
                foreach (ProceduralObjects.Classes.ProceduralObject obj in Logic.pObjSelection)
                {
                    list.Add(new PO_Object(obj));
                }
                return list;
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
        public uint Id { get; set; } // The InstanceID.NetLane value
        public bool Selected { get; set; }
        private int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        public Vector3 Position { get => procObj.m_position; set => procObj.m_position = value; }
        private Quaternion Rotation { get => procObj.m_rotation; set => procObj.m_rotation = value; }

        public float Angle
        {
            get
            {
                float a = -Rotation.eulerAngles.y * Mathf.Deg2Rad;
                //Debug.Log($"Getting:{a} ({a * Mathf.Rad2Deg})");
                return a;
            }

            set
            {
                float a = -(value * Mathf.Rad2Deg) % 360f;
                if (a < 0) a += 360f;
                //if (a >= 360f) a -= 360f;
                //Debug.Log($"Setting:{a * Mathf.Deg2Rad} ({a})");
                procObj.m_rotation.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
            }
        }

        public PO_Object(ProceduralObjects.Classes.ProceduralObject obj)
        {
            procObj = obj;
            ProcId = obj.id;
        }


        public void SetPositionY(float h)
        {
            procObj.m_position.y = h;
        }

        public float GetDistance(Vector3 location)
        {
            return Vector3.Distance(Position, location);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        {
            float size = 4f;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, Position, size, Position.y - 100f, Position.y + 100f, renderLimits: false, alphaBlend: true);
        }

        public string DebugQuaternion()
        {
            return $"{Id}:{Rotation.w},{Rotation.x},{Rotation.y},{Rotation.z}";
        }
    }

    static class PO_Utils
    {
        public static ProceduralObjects.Classes.ProceduralObject GetProcWithId(this List<ProceduralObjects.Classes.ProceduralObject> list, int id)
        {
            if (list.Any(po => po.id == id))
            {
                return list.FirstOrDefault(po => po.id == id);
            }
            return null;
        }
    }
}
