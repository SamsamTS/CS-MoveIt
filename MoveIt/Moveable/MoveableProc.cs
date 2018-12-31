using UnityEngine;
using System.Collections.Generic;
using ColossalFramework.Math;
using ProceduralObjects;
using System.Linq;


namespace MoveIt
{
    public class PObjState : InstanceState
    {
        public bool single;
    }

    public class MoveableProc : Instance
    {
        public ProceduralObjectsLogic procLogic;
        public ProceduralObjects.Classes.ProceduralObject ProcObj;

        public override HashSet<ushort> segmentList
        {
            get
            {
                return new HashSet<ushort>();
            }
        }

        private bool isWithinRange = false;

        private int procId;
        public int ProcId {
            get => (int)id.NetLane;
            set {
                if (value < 2147483647)
                {
                    procId = value;
                    isWithinRange = true;
                }
                else
                {
                    procId = 0;
                }
            }
        }

        public MoveableProc(InstanceID instanceID) : base(instanceID)
        {
            procLogic = Object.FindObjectOfType<ProceduralObjectsLogic>();
            ProcObj = PObjUtils._getObjectWithId(procLogic.pObjSelection, ProcId);
        }


        public override InstanceState GetState()
        {
            PObjState state = new PObjState();

            return state;
        }


        public override void SetState(InstanceState state)
        {}


        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty || !isWithinRange) return Vector3.zero;
                return ProcObj.m_position;
            }
        }

        public override float angle
        {
            get
            {
                if (id.IsEmpty || !isWithinRange) return 0f;
                return ProcObj.m_rotation.y;
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                if (!isWithinRange) return false;
                return true;
            }
        }


        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        { }


        public override void Move(Vector3 location, float angle)
        { }


        public override void SetHeight(float height)
        {
            if (!isValid) return;

            ProcObj.m_position.y = height;
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
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        { }
    }

    public static class PObjUtils
    {
        public static ProceduralObjects.Classes.ProceduralObject _getObjectWithId(this List<ProceduralObjects.Classes.ProceduralObject> list, int id)
        {
            if (list.Any(po => po.id == id))
            {
                return list.FirstOrDefault(po => po.id == id);
            }
            return null;
        }
    }
}
