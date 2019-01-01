using UnityEngine;
using System.Collections.Generic;
using ColossalFramework.Math;
using ProceduralObjects;
using System.Linq;


namespace MoveIt
{
    public class ProcState : InstanceState
    {
        public bool single;
    }

    public class MoveableProc : Instance
    {
        public ProceduralObjectsLogic m_procLogic;
        public ProceduralObjects.Classes.ProceduralObject m_procObj;

        public override HashSet<ushort> segmentList
        {
            get
            {
                return new HashSet<ushort>();
            }
        }

        public int ProcId { get => (int)id.NetLane - 1; }
        
        public MoveableProc(InstanceID instanceID) : base(instanceID)
        {
            m_procLogic = PO_Utils.GetPOLogic();
            m_procObj = m_procLogic.pObjSelection.GetProcWithId(ProcId);
        }


        public override InstanceState GetState()
        {
            ProcState state = new ProcState();
            state.instance = this;

            state.position = m_procObj.m_position;
            state.angle = m_procObj.m_rotation.y;

            return state;
        }


        public override void SetState(InstanceState state)
        {
            m_procObj.m_position = state.position;
            m_procObj.m_rotation.y = state.angle;
        }


        public override Vector3 position
        {
            get
            {
                //if (!isValid) return Vector3.zero;
                Debug.Log($"Position:{m_procObj.m_position} {MoveItTool.InstanceIDDebug(id)}");
                return m_procObj.m_position;
            }
        }

        public override float angle
        {
            get
            {
                //if (!isValid) return 0f;
                Debug.Log($"RotationY:{m_procObj.m_rotation.y} {MoveItTool.InstanceIDDebug(id)}");
                return m_procObj.m_rotation.y;
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                return true;
            }
        }


        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                //newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            Move(newPosition, state.angle + deltaAngle);
        }


        public override void Move(Vector3 location, float angle)
        {
            m_procObj.m_rotation.ToAngleAxis(out float initialAngle, out Vector3 devNull);
            angle *= Mathf.Rad2Deg;
            while (angle < 0f) angle += 360f;
            while (angle >= 360f) angle -= 360f;

            Debug.Log($"Proc {m_procObj.id} from {m_procObj.m_position} to {location}\nRotating from {initialAngle} to {angle} - (delta:{initialAngle - angle})\n" +
                $"w:{m_procObj.m_rotation.w} x:{m_procObj.m_rotation.x} y:{m_procObj.m_rotation.y} z:{m_procObj.m_rotation.z}\n");
            m_procObj.m_position = location;
            m_procObj.m_rotation = m_procObj.m_rotation.Rotate(0, initialAngle - angle, 0);
        }


        public override void SetHeight(float height)
        {
            if (!isValid) return;
            m_procObj.m_position.y = height;
        }


        public override Instance Clone(InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes)
        {
            return instanceState.instance;
        }

        public override Instance Clone(InstanceState instanceState, Dictionary<ushort, ushort> clonedNodes)
        {
            return instanceState.instance;
        }

        public override void Delete()
        { }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            return new Bounds(m_procObj.m_position, new Vector3(10, 0, 10));
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;
            PO_Utils.RenderOverlay(cameraInfo, m_procObj.m_position, 1f, 0f, new Color32(180, 30, 240, 170));
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }
    }
}
