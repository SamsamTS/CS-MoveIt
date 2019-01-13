using ProceduralObjects;
using ProceduralObjects.Classes;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;

// Low level PO wrapper, only accessed by high level

namespace MoveIt
{
    internal class PO_WrapperEnabled : IPO_Wrapper
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

        public List<PO_ObjectBase> Objects
        {
            get
            {
                Debug.Log($"PO List: Active");
                List<ProceduralObject> objectList = Logic.proceduralObjects;
                if (MoveItTool.POOnlySelectedAreVisible)
                {
                    objectList = Logic.pObjSelection;
                }
                List<PO_ObjectBase> objects = new List<PO_ObjectBase>();
                foreach (ProceduralObject obj in objectList)
                {
                    PO_ObjectBase o = new PO_ObjectEnabled(obj);
                    objects.Add(o);
                }
                return objects;
            }
        }
    }


    internal class PO_ObjectEnabled : PO_ObjectBase
    {
        private ProceduralObject procObj;
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
                return a * Mathf.Deg2Rad;
            }

            set
            {
                float a = -(value * Mathf.Rad2Deg) % 360f;
                if (a < 0) a += 360f;
                procObj.m_rotation.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
            }
        }

        public PO_ObjectEnabled(ProceduralObject obj)
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
