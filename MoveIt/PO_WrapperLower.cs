using ProceduralObjects;
using ProceduralObjects.Classes;
using ColossalFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        public uint Clone(uint originalId, Vector3 position)
        {
            int id = (int)originalId - 1;
            var cache = new CacheProceduralObject(Logic.proceduralObjects[id]);

            int newId = Logic.proceduralObjects.GetNextUnusedId();
            Debug.Log($"Cloning {originalId - 1} to {newId}(?), {position}\n{cache.baseInfoType}: {cache}");

            //PropInfo propInfo = Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault((PropInfo info) => info.name == cache.basePrefabName);
            //Debug.Log($"{propInfo.m_material.color}, {propInfo.m_material.mainTexture}, {propInfo.m_material.shader}");

            var obj = Logic.PlaceCacheObject(cache, false);
            //ProceduralObject obj = new ProceduralObject(cache, newId, position);
            //Logic.proceduralObjects.Add(obj);

            return (uint)obj.id + 1;
            //return 0;
        }
    }


    internal class PO_ObjectEnabled : IPO_Object
    {
        private ProceduralObject procObj;

        public uint Id { get; set; } // The InstanceID.NetLane value
        public bool Selected { get; set; }
        private int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        public Vector3 Position { get => procObj.m_position; set => procObj.m_position = value; }
        private Quaternion Rotation { get => procObj.m_rotation; set => procObj.m_rotation = value; }

        public string Name
        {
            get
            {
                if (procObj.basePrefabName.Length < 35)
                    return "[PO]" + procObj.basePrefabName;
                return "[PO]" + procObj.basePrefabName.Substring(0, 35);
            }
            set { }
        }

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

        private Info_POEnabled _info = null;
        public IInfo Info
        {
            get
            {
                if (_info == null)
                    _info = new Info_POEnabled(this);

                return _info;
            }
            set => _info = (Info_POEnabled)value;
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


    public class Info_POEnabled : IInfo
    {
        private PO_ObjectEnabled _obj = null;

        public Info_POEnabled(object i)
        {
            _obj = (PO_ObjectEnabled)i;
        }

        public string Name => _obj.Name;

        public PrefabInfo Prefab { get; set; } = null;
    }
}
