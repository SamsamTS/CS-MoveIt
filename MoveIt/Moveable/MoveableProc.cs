using UnityEngine;
using System.Collections.Generic;

namespace MoveIt
{
    public class ProcState : InstanceState
    {
    }

    public class MoveableProc : Instance
    {
        internal PO_Object m_procObj;

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
            m_procObj = MoveItTool.PO.GetProcObj(instanceID.NetLane);
        }


        public override InstanceState GetState()
        {
            ProcState state = new ProcState();
            state.instance = this;
            state.position = m_procObj.Position;
            state.angle = m_procObj.Angle;
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);

            return state;
        }


        public override void SetState(InstanceState state)
        {
            m_procObj.Position = state.position;
            m_procObj.Angle = state.angle;
        }


        public override Vector3 position
        {
            get
            {
                if (!isValid) return Vector3.zero;
                return m_procObj.Position;
            }
        }

        public override float angle
        {
            get
            {
                if (!isValid) return 0f;
                return m_procObj.Angle;
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


        // deltaAngleRad is clumulative delta since Transform Action started, CCW
        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngleRad, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            float a = state.angle + deltaAngleRad;
            //Debug.Log($"{state.angle} + {deltaAngleRad} = {a} ({a % (Mathf.PI * 2)})");
            Move(newPosition, a % (Mathf.PI * 2));
        }


        // angleRad is absolute angle, CCW
        public override void Move(Vector3 location, float angleRad)
        {
            Debug.Log($"{id.NetLane}\n{m_procObj.Position} => {location}\nRadians {m_procObj.Angle} => {angleRad} ({angleRad - m_procObj.Angle})\n" +
                $"Degrees {m_procObj.Angle * Mathf.Rad2Deg} => {angleRad * Mathf.Rad2Deg} ({(angleRad - m_procObj.Angle) * Mathf.Rad2Deg})\n");
            m_procObj.Position = location;
            m_procObj.Angle = angleRad;
        }


        public override void SetHeight(float height)
        {
            if (!isValid) return;
            m_procObj.SetPositionY(height);
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
            return new Bounds(m_procObj.Position, new Vector3(8, 0, 8));
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;
            m_procObj.RenderOverlay(cameraInfo, MoveItTool.m_POselectedColor); 
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }
    }
}
