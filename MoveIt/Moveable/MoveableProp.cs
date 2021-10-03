using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace MoveIt
{
    public class PropState : InstanceState
    {
        public bool single;
        public bool fixedHeight;
    }

    public class MoveableProp : Instance
    {
        public override HashSet<ushort> segmentList
        {
            get
            {
                return new HashSet<ushort>();
            }
        }

        public MoveableProp(InstanceID instanceID) : base(instanceID)
        {
            Info = PropLayer.Manager.GetInfo(instanceID);
        }

        public override InstanceState SaveToState(bool integrate = true)
        {
            PropState state = new PropState
            {
                instance = this,
                isCustomContent = Info.Prefab.m_isCustomContent
            };

            IProp prop = PropLayer.Manager.Buffer(id);

            state.Info = Info;
            state.position = prop.Position;
            state.angle = prop.Angle;
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);
            state.single = prop.Single;
            state.fixedHeight = prop.FixedHeight;

            state.SaveIntegrations(integrate);

            return state;
        }

        public override void LoadFromState(InstanceState state)
        {
            if (!(state is PropState propState)) return;

            IProp prop = PropLayer.Manager.Buffer(id);
            prop.Angle = propState.angle;
            prop.FixedHeight = propState.fixedHeight;

            prop.MoveProp(propState.position);
            prop.UpdatePropRenderer(true);

            //ushort prop = id.Prop;
            //PropManager.instance.m_props.m_buffer[prop].Angle = propState.angle;
            //PropManager.instance.m_props.m_buffer[prop].FixedHeight = propState.fixedHeight;

            //PropManager.instance.MoveProp(prop, propState.position);
            //PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return PropLayer.Manager.Buffer(id).Position;
                //return PropManager.instance.m_props.m_buffer[id.Prop].Position;
            }
            set
            {
                if (id.IsEmpty) PropLayer.Manager.Buffer(id).Position = Vector3.zero;
                else PropLayer.Manager.Buffer(id).Position = value;
            }
        }

        public override float angle
        {
            get
            {
                if (id.IsEmpty) return 0f;
                return PropLayer.Manager.Buffer(id).Angle;
            }
            set
            {
                if (id.IsEmpty) return;
                PropLayer.Manager.Buffer(id).Angle = (value + Mathf.PI * 2) % (Mathf.PI * 2);
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                return PropLayer.Manager.Buffer(id).m_flags != 0;
            }
        }

        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (!PropLayer.Manager.Buffer(id).FixedHeight && deltaHeight != 0 && (MoveItLoader.loadMode == ICities.LoadMode.LoadAsset || MoveItLoader.loadMode == ICities.LoadMode.NewAsset))
            {
                PropLayer.Manager.Buffer(id).FixedHeight = true;
            }

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            Move(newPosition, state.angle + deltaAngle);
        }

        public override void Move(Vector3 location, float angle)
        {
            if (!isValid) return;

            IProp prop = PropLayer.Manager.Buffer(id);
            prop.Angle = angle;
            prop.MoveProp(location);
            prop.UpdatePropRenderer(true);

            //ushort prop = id.Prop;
            //PropManager.instance.m_props.m_buffer[prop].Angle = angle;
            //PropManager.instance.MoveProp(prop, location);
            //PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override void SetHeight(float height)
        {
            Vector3 newPosition = position;
            newPosition.y = height;

            IProp prop = PropLayer.Manager.Buffer(id);
            prop.MoveProp(newPosition);
            prop.UpdatePropRenderer(true);

            //ushort prop = id.Prop;
            //PropManager.instance.MoveProp(prop, newPosition);
            //PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override void SetHeight()
        {
            SetHeight(TerrainManager.instance.SampleDetailHeight(position));
        }

        public override Instance Clone(InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes, Action action)
        {
            PropState state = instanceState as PropState;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            Instance cloneInstance = null;

            if (PropLayer.Manager.CreateProp(out uint clone, state.Info.Prefab as PropInfo, newPosition, state.angle + deltaAngle, state.single))
            {
                InstanceID cloneID = default;
                cloneID = PropLayer.Manager.SetProp(cloneID, clone);
                PropLayer.Manager.Buffer(cloneID).FixedHeight = state.fixedHeight;
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override Instance Clone(InstanceState instanceState, Dictionary<ushort, ushort> clonedNodes)
        {
            PropState state = instanceState as PropState;

            Instance cloneInstance = null;

            if (PropLayer.Manager.CreateProp(out uint clone, state.Info.Prefab as PropInfo, state.position, state.angle, state.single))
            {
                InstanceID cloneID = default;
                cloneID = PropLayer.Manager.SetProp(cloneID, clone);
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override void Delete()
        {
            if (isValid) PropLayer.Manager.Buffer(id).ReleaseProp();
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            IProp prop = PropLayer.Manager.Buffer(id);

            Randomizer randomizer = new Randomizer(prop.Index);
            float scale = prop.Info.m_minScale + (float)randomizer.Int32(10000u) * (prop.Info.m_maxScale - prop.Info.m_minScale) * 0.0001f;
            float radius = Mathf.Max(prop.Info.m_generatedInfo.m_size.x, prop.Info.m_generatedInfo.m_size.z) * scale;

            return new Bounds(prop.Position, new Vector3(radius, 0, radius));
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;
            if (MoveItTool.m_isLowSensitivity) return;

            //ushort prop = id.Prop;
            IProp prop = PropLayer.Manager.Buffer(id);
            PropInfo propInfo = prop.Info;
            Vector3 position = prop.Position;
            float angle = prop.Angle;
            Randomizer randomizer = new Randomizer(prop.Index);
            float scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
            float alpha = 1f;
            PropTool.CheckOverlayAlpha(propInfo, scale, ref alpha);
            toolColor.a *= alpha;
            PropTool.RenderOverlay(cameraInfo, propInfo, position, scale, angle, toolColor);
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            if (MoveItTool.m_isLowSensitivity) return;

            PropState state = instanceState as PropState;

            PropInfo info = state.Info.Prefab as PropInfo;
            Randomizer randomizer = new Randomizer(state.instance.id.Prop);
            float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y - state.terrainHeight + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            }

            float newAngle = state.angle + deltaAngle;

            PropTool.RenderOverlay(cameraInfo, info, newPosition, scale, newAngle, toolColor);
        }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            RenderCloneGeometryImplementation(instanceState, ref matrix4x, deltaPosition, deltaAngle, center, followTerrain, cameraInfo);
        }

        public static void RenderCloneGeometryImplementation(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo)
        {
            InstanceID id = instanceState.instance.id;

            PropInfo info = instanceState.Info.Prefab as PropInfo;
            Randomizer randomizer = new Randomizer(id.Prop);
            float scale = info.m_minScale + (float)randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;

            Vector3 newPosition = matrix4x.MultiplyPoint(instanceState.position - center);
            newPosition.y = instanceState.position.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y - instanceState.terrainHeight + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            }

            float newAngle = instanceState.angle + deltaAngle;
            
            if (info.m_requireHeightMap)
            {
                TerrainManager.instance.GetHeightMapping(newPosition, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                PropInstance.RenderInstance(cameraInfo, info, id, newPosition, scale, newAngle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
            }
            else
            {
                PropInstance.RenderInstance(cameraInfo, info, id, newPosition, scale, newAngle, info.GetColor(ref randomizer), RenderManager.DefaultColorLocation, true);
            }
        }
    }
}
