using UnityEngine;

using System.Collections.Generic;
using ColossalFramework.Math;


namespace MoveIt
{
    public class MoveableProp : Instance
    {
        public class PropState : InstanceState
        {
            public bool single;
        }

        public override HashSet<ushort> segmentList
        {
            get
            {
                return new HashSet<ushort>();
            }
        }

        public MoveableProp(InstanceID instanceID) : base(instanceID) { }

        public override InstanceState GetState()
        {
            PropState state = new PropState();

            state.instance = this;

            ushort prop = id.Prop;
            state.info = info;

            state.position = PropManager.instance.m_props.m_buffer[prop].Position;
            state.angle = PropManager.instance.m_props.m_buffer[prop].Angle;
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);

            state.single = PropManager.instance.m_props.m_buffer[prop].Single;

            return state;
        }

        public override void SetState(InstanceState state)
        {
            InstanceState propState = state as InstanceState;
            if (propState == null) return;

            ushort prop = id.Prop;
            PropManager.instance.m_props.m_buffer[prop].Angle = propState.angle;
            PropManager.instance.MoveProp(prop, propState.position);
            PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return PropManager.instance.m_props.m_buffer[id.Prop].Position;
            }
        }

        public override float angle
        {
            get
            {
                if (id.IsEmpty) return 0f;
                return PropManager.instance.m_props.m_buffer[id.Prop].Angle;
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                return PropManager.instance.m_props.m_buffer[id.Prop].m_flags != 0;
            }
        }

        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            Move(newPosition, state.angle + deltaAngle);
        }

        public override void Move(Vector3 location, float angle)
        {
            if (!isValid) return;

            ushort prop = id.Prop;
            PropManager.instance.m_props.m_buffer[prop].Angle = angle;
            PropManager.instance.MoveProp(prop, location);
            PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override void SetHeight(float height)
        {
            Vector3 newPosition = position;
            newPosition.y = height;

            ushort prop = id.Prop;
            PropManager.instance.MoveProp(prop, newPosition);
            PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override Instance Clone(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            Instance cloneInstance = null;

            PropInstance[] buffer = PropManager.instance.m_props.m_buffer;
            ushort prop = id.Prop;

            ushort clone;
            if (PropManager.instance.CreateProp(out clone, ref SimulationManager.instance.m_randomizer,
                buffer[prop].Info, newPosition, buffer[prop].Angle + deltaAngle, buffer[prop].Single))
            {
                InstanceID cloneID = default(InstanceID);
                cloneID.Prop = clone;
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override Instance Clone(InstanceState instanceState)
        {
            PropState state = instanceState as PropState;

            Instance cloneInstance = null;

            ushort clone;
            if (PropManager.instance.CreateProp(out clone, ref SimulationManager.instance.m_randomizer,
                state.info as PropInfo, state.position, state.angle, state.single))
            {
                InstanceID cloneID = default(InstanceID);
                cloneID.Prop = clone;
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override void Delete()
        {
            if (isValid) PropManager.instance.ReleaseProp(id.Prop);
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            ushort prop = id.Prop;
            PropInfo info = PropManager.instance.m_props.m_buffer[prop].Info;

            Randomizer randomizer = new Randomizer(prop);
            float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
            float radius = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale;

            return new Bounds(PropManager.instance.m_props.m_buffer[prop].Position, new Vector3(radius, 0, radius));
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;

            ushort prop = id.Prop;
            PropManager propManager = PropManager.instance;
            PropInfo propInfo = propManager.m_props.m_buffer[prop].Info;
            Vector3 position = propManager.m_props.m_buffer[prop].Position;
            float angle = propManager.m_props.m_buffer[prop].Angle;
            Randomizer randomizer = new Randomizer((int)prop);
            float scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
            float alpha = 1f;
            PropTool.CheckOverlayAlpha(propInfo, scale, ref alpha);
            toolColor.a *= alpha;
            PropTool.RenderOverlay(cameraInfo, propInfo, position, scale, angle, toolColor);
        }

        public override void RenderCloneOverlay(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            ushort prop = id.Prop;

            PropInfo info = PropManager.instance.m_props.m_buffer[prop].Info;
            Randomizer randomizer = new Randomizer(prop);
            float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

            PropTool.RenderOverlay(cameraInfo, info, state.position, scale, state.angle, toolColor);
        }

        public override void RenderCloneGeometry(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            ushort prop = id.Prop;

            PropInfo info = PropManager.instance.m_props.m_buffer[prop].Info;
            Randomizer randomizer = new Randomizer(prop);
            float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
            
            if (info.m_requireHeightMap)
            {
                Texture heightMap;
                Vector4 heightMapping;
                Vector4 surfaceMapping;
                TerrainManager.instance.GetHeightMapping(state.position, out heightMap, out heightMapping, out surfaceMapping);
                PropInstance.RenderInstance(cameraInfo, info, id, state.position, scale, state.angle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
            }
            else
            {
                PropInstance.RenderInstance(cameraInfo, info, id, state.position, scale, state.angle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true);
            }
        }
    }
}
