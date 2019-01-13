using ProceduralObjects;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;

// Low level PO wrapper, only accessed by high level

namespace MoveIt.PO
{
    public class PO_WrapperEnabled : PO_WrapperBase
    {
        private ProceduralObjectsLogic logic = null;
        public ProceduralObjectsLogic Logic
        {
            get
            {
                if (logic == null)
                    logic = Object.FindObjectOfType<ProceduralObjectsLogic>();
                return logic;
            }
        }

        public override List<PO_ObjectBase> Objects
        {
            get
            {
                Debug.Log($"Active");
                List<ProceduralObjects.Classes.ProceduralObject> objectList = Logic.proceduralObjects;
                if (MoveItTool.POOnlySelectedAreVisible)
                {
                    objectList = Logic.pObjSelection;
                }
                List<PO_ObjectBase> objects = new List<PO_ObjectBase>();
                foreach (ProceduralObjects.Classes.ProceduralObject obj in objectList)
                {
                    PO_ObjectBase o = new PO_ObjectEnabled(obj);
                    objects.Add(o);
                }
                return objects;
            }
        }
    }


    public class PO_ObjectEnabled : PO_ObjectBase
    {
        private ProceduralObjects.Classes.ProceduralObject procObj;
        public bool Selected { get; set; }
        private int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        public override Vector3 Position { get => procObj.m_position; set => procObj.m_position = value; }
        private Quaternion Rotation { get => procObj.m_rotation; set => procObj.m_rotation = value; }

        public override float Angle
        {
            get
            {
                float a = -Rotation.eulerAngles.y % 360f;
                if (a < 0) a += 360f;
                //Debug.Log($"Getting:{a * Mathf.Deg2Rad} ({a})\nRaw:{Rotation.eulerAngles.y},{-Rotation.eulerAngles.y % 360f}");
                return a * Mathf.Deg2Rad;
            }

            set
            {
                float a = -(value * Mathf.Rad2Deg) % 360f;
                if (a < 0) a += 360f;
                procObj.m_rotation.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
                //float b = Mathf.Abs(a - 360f);
                //Debug.Log($"Setting:{b * Mathf.Deg2Rad} ({b})\n - actual:{a} => {Rotation.eulerAngles.y}");
            }
        }

        public PO_ObjectEnabled(ProceduralObjects.Classes.ProceduralObject obj)
        {
            procObj = obj;
            ProcId = obj.id;
        }

        public override void SetPositionY(float h)
        {
            procObj.m_position.y = h;
        }

        public override float GetDistance(Vector3 location)
        {
            return Vector3.Distance(Position, location);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        {
            float size = 4f;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, Position, size, Position.y - 100f, Position.y + 100f, renderLimits: false, alphaBlend: true);
        }

        public override string DebugQuaternion()
        {
            return $"{Id}:{Rotation.w},{Rotation.x},{Rotation.y},{Rotation.z}";
        }
    }
}
