using UnityEngine;
using System.Collections.Generic;
using ColossalFramework.Math;
using System.Linq;

namespace MoveIt
{
    public class ProcState : InstanceState
    {
        public bool single;
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
            m_procObj = PO_Logic.GetProcObj(instanceID.NetLane);
        }


        public override InstanceState GetState()
        {
            ProcState state = new ProcState();
            state.instance = this;

            state.position = m_procObj.Position;
            state.angle = m_procObj.GetAngleRadY();

            return state;
        }


        public override void SetState(InstanceState state)
        {
            m_procObj.Position = state.position;
            m_procObj.SetAngleDeltaRadY(state.angle);
        }


        public override Vector3 position
        {
            get
            {
                //if (!isValid) return Vector3.zero;
                Debug.Log($"Position:{m_procObj.Position} {MoveItTool.InstanceIDDebug(id)}");
                return m_procObj.Position;
            }
        }

        public override float angle
        {
            get
            {
                //if (!isValid) return 0f;
                return m_procObj.GetAngleRadY();
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


        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngleRad, Vector3 center, bool followTerrain)
        {
            //float deltaAngleDeg = deltaAngleRad * Mathf.Rad2Deg;
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                //newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }
            Debug.Log($"{state.angle} + {deltaAngleRad} = {state.angle + deltaAngleRad}");
            Move(newPosition, state.angle + deltaAngleRad);
        }


        public override void Move(Vector3 location, float angleRad)
        {
            float initialAngle = m_procObj.GetAngleRadY();

            Debug.Log($"\nRotate {initialAngle} - {angleRad} = {initialAngle - angleRad}\nRotate {initialAngle * Mathf.Rad2Deg} - {angleRad * Mathf.Rad2Deg} = {initialAngle * Mathf.Rad2Deg - angleRad * Mathf.Rad2Deg}\n" +
                $"    {m_procObj.DebugQuaternion()}\n");
            m_procObj.Position = location;
            m_procObj.SetAngleRadY(angleRad);
            //m_procObj.Rotation = m_procObj.Rotation.Rotate(0, initialAngle - angleDeg, 0);
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
            PO_Utils.RenderOverlay(cameraInfo, m_procObj.Position, 2f, 0f, MoveItTool.m_POselectedColor); 
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }
    }
}
