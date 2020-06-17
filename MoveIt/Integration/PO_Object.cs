using ColossalFramework;
using UnityEngine;
using System;

namespace MoveIt
{
    internal class PO_Object
    {
        internal object procObj;

        /// <summary>
        /// The InstanceID.NetLane value
        /// </summary>
        public uint Id { get; set; }
        public bool Selected { get; set; }
        public int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        internal Type tPOLogic = null, tPOMod = null, tPO = null, tPOLayer = null;

        internal Vector3 Position
        {
            get => (Vector3)tPO.GetField("m_position").GetValue(procObj);
            set
            {
                tPO.GetField("m_position").SetValue(procObj, value);
            }
        }
        internal Color POColor
        {
            get => (Color)tPO.GetField("m_color").GetValue(procObj);
            set
            {
                tPO.GetField("m_color").SetValue(procObj, value);
            }
        }
        private Quaternion Rotation
        {
            get => (Quaternion)tPO.GetField("m_rotation").GetValue(procObj);
            set => tPO.GetField("m_rotation").SetValue(procObj, value);
        }

        public string Name
        {
            get
            {
                string name = (string)tPO.GetField("basePrefabName").GetValue(procObj);
                if (name.Length < 35)
                    return "[PO]" + name;
                return "[PO]" + name.Substring(0, 35);
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
                
                Quaternion q = Rotation;
                q.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
                Rotation = q;
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

        public object GetProceduralObject()
        {
            return procObj;
        }

        public PO_Object(object obj)
        {
            tPOLogic = PO_Logic.POAssembly.GetType("ProceduralObjects.3Logic");
            tPOMod = PO_Logic.POAssembly.GetType("ProceduralObjects.ProceduralObjectsMod");
            tPO = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");
            tPOLayer = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.Layer");

            procObj = obj;
            ProcId = (int)tPO.GetField("id").GetValue(procObj);
        }

        public bool isHidden()
        {
            object layer = tPO.GetField("layer").GetValue(procObj);
            if (layer == null)
            {
                return false;
            }
            return (bool)tPOLayer.GetField("m_isHidden").GetValue(layer);
        }

        public void SetPositionY(float h)
        {
            Position = new Vector3(Position.x, h, Position.z);
        }

        public float GetDistance(Vector3 location)
        {
            return Vector3.Distance(Position, location);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        {
            RenderOverlay(cameraInfo, color, Position);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color, Vector3 position)
        {
            float size = 4f;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, position, size, Position.y - 100f, Position.y + 100f, renderLimits: false, alphaBlend: true);
        }
    }
}
