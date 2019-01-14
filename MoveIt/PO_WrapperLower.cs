using ProceduralObjects;
using ProceduralObjects.Classes;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;

// Low level PO wrapper, only accessed by high level

namespace MoveIt
{
    internal class PO_LogicEnabled : IPO_Logic
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

        public List<IPO_Object> Objects
        {
            get
            {
                List<ProceduralObject> objectList = Logic.proceduralObjects;
                if (MoveItTool.POOnlySelectedAreVisible)
                {
                    objectList = Logic.pObjSelection;
                }
                List<IPO_Object> objects = new List<IPO_Object>();
                foreach (ProceduralObject obj in objectList)
                {
                    IPO_Object o = new PO_ObjectEnabled(obj);
                    objects.Add(o);
                }
                return objects;
            }
        }

        //public object GetReferenceChain(Type tChain)
        //{
        //    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        //    object chain = null;

        //    if (MoveItTool.PO.Enabled)
        //    {
        //        object[] field;
        //        MethodInfo method;
        //        chain = Activator.CreateInstance(tChain);
        //        chain = tChain.GetMethod("Add", flags, null, new Type[] { typeof(GameObject) }, null).Invoke(chain, new object[] { Logic.gameObject });
        //        Debug.Log($"A {chain}");
        //        chain = tChain.GetMethod("Add", flags, null, new Type[] { typeof(ProceduralObjectsLogic) }, null).Invoke(chain, new object[] { Logic });
        //        Debug.Log($"B {chain}");
        //        method = tChain.GetMethod("Add", flags, null, new Type[] { typeof(List<ProceduralObject>) }, null);
        //        Debug.Log($"C {method}:{method.Name}");
        //        field = new object[] { typeof(ProceduralObjectsLogic).GetField("proceduralObjects") };
        //        Debug.Log($"D {field}");
        //        chain = method.Invoke(chain, field);
        //        Debug.Log($"E {chain}");
        //        //chain = tChain.GetMethod("Add", flags, null, new Type[] { typeof(FieldInfo) }, null).Invoke(chain, new object[] { typeof(Array32<TreeInstance>).GetField("m_buffer") });
        //        Debug.Log($"\n************************************************\nPO Chain:{chain}\n***********************************************");
        //    }
        //    return chain;
        //}
    }


    internal class PO_ObjectEnabled : IPO_Object
    {
        private ProceduralObject procObj;

        public uint Id { get; set; } // The InstanceID.NetLane value
        public bool Selected { get; set; }
        private int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        public Vector3 Position { get => procObj.m_position; set => procObj.m_position = value; }
        private Quaternion Rotation { get => procObj.m_rotation; set => procObj.m_rotation = value; }

        public float Angle
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
}
