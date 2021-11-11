using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using UnityEngine;
using EManagersLib.API;

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
            Info = new Info_Prefab(PropAPI.Wrapper.GetInfo(instanceID)); // Use new EML API
            //Info = PropLayer.Manager.GetInfo(instanceID);
        }

        public override InstanceState SaveToState(bool integrate = true)
        {
            PropState state = new PropState
            {
                instance = this,
                isCustomContent = Info.Prefab.m_isCustomContent
            };

            //IProp prop = PropLayer.Manager.Buffer(id);

            state.Info = Info;
            // Use new EML API
            state.position = PropAPI.Wrapper.GetPosition(id); // prop.Position;
            state.angle = PropAPI.Wrapper.GetAngle(id); // prop.Angle;
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);
            state.single = PropAPI.Wrapper.GetSingle(id); // prop.Single;
            state.fixedHeight = PropAPI.Wrapper.GetFixedHeight(id); // prop.FixedHeight;

            state.SaveIntegrations(integrate);

            return state;
        }

        public override void LoadFromState(InstanceState state)
        {
            if (!(state is PropState propState)) return;

            // Updated to use new EML API
            uint propID = id.GetProp32(); // id.GetProp32 works for both Non-EML and EML environment. It gets the first 24-bit value of InstanceID::m_rawData
            PropAPI.Wrapper.SetAngle(propID, propState.angle); //prop.Angle = propState.angle;
            PropAPI.Wrapper.SetFixedHeight(propID, propState.fixedHeight); //prop.FixedHeight = propState.fixedHeight;

            PropAPI.Wrapper.MoveProp(propID, propState.position); //prop.MoveProp(propState.position);
            PropAPI.Wrapper.UpdatePropRenderer(propID, true); //prop.UpdatePropRenderer(true);

            //ushort prop = id.Prop;
            //PropManager.instance.m_props.m_buffer[prop].Angle = propState.angle;
            //PropManager.instance.m_props.m_buffer[prop].FixedHeight = propState.fixedHeight;

            //PropManager.instance.MoveProp(prop, propState.position);
            //PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override Vector3 position
        {
            // Use new EML API
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return PropAPI.Wrapper.GetPosition(id); // PropLayer.Manager.Buffer(id).Position;
                //return PropManager.instance.m_props.m_buffer[id.Prop].Position;
            }
            set
            {
                if (id.IsEmpty) PropAPI.Wrapper.SetPosition(id, Vector3.zero); // PropLayer.Manager.Buffer(id).Position = Vector3.zero;
                else PropAPI.Wrapper.SetPosition(id, value); // PropLayer.Manager.Buffer(id).Position = value;
            }
        }

        public override float angle
        {
            // Use new EML API
            get
            {
                if (id.IsEmpty) return 0f;
                return PropAPI.Wrapper.GetAngle(id); // PropLayer.Manager.Buffer(id).Angle;
            }
            set
            {
                if (id.IsEmpty) return;
                PropAPI.Wrapper.SetAngle(id, (value + Mathf.PI * 2) % (Mathf.PI * 2)); //PropLayer.Manager.Buffer(id).Angle = (value + Mathf.PI * 2) % (Mathf.PI * 2);
            }
        }

        public override bool isValid
        {
            // Use new EML API
            get
            {
                if (id.IsEmpty) return false;
                return PropAPI.Wrapper.IsValid(id); // PropLayer.Manager.Buffer(id).m_flags != 0;
            }
        }

        public override void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;
            // Use new EML API
            if (!PropAPI.Wrapper.GetFixedHeight(id)/*PropLayer.Manager.Buffer(id).FixedHeight*/ && deltaHeight != 0 && (MoveItLoader.loadMode == ICities.LoadMode.LoadAsset || MoveItLoader.loadMode == ICities.LoadMode.NewAsset))
            {
                PropAPI.Wrapper.SetFixedHeight(id, true); // PropLayer.Manager.Buffer(id).FixedHeight = true;
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
            // Use new EML API; NOTE** id.GetProp32() works for both non EML and EML environments, as it just gets the first 24 bit of InstanceID::m_rawData
            uint propID = id.GetProp32(); // IProp prop = PropLayer.Manager.Buffer(id);
            PropAPI.Wrapper.SetAngle(propID, angle); // prop.Angle = angle;
            PropAPI.Wrapper.MoveProp(propID, location); // prop.MoveProp(location);
            PropAPI.Wrapper.UpdatePropRenderer(propID, true); // prop.UpdatePropRenderer(true);

            //ushort prop = id.Prop;
            //PropManager.instance.m_props.m_buffer[prop].Angle = angle;
            //PropManager.instance.MoveProp(prop, location);
            //PropManager.instance.UpdatePropRenderer(prop, true);
        }

        public override void SetHeight(float height)
        {
            Vector3 newPosition = position;
            newPosition.y = height;
            // Use new EML API;
            uint propID = id.GetProp32(); // IProp prop = PropLayer.Manager.Buffer(id);
            PropAPI.Wrapper.MoveProp(propID, newPosition); // prop.MoveProp(newPosition);
            PropAPI.Wrapper.UpdatePropRenderer(propID, true); // prop.UpdatePropRenderer(true);

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

            // Use new EML API
            //if (PropLayer.Manager.CreateProp(out uint clone, state.Info.Prefab as PropInfo, newPosition, state.angle + deltaAngle, state.single))
            if (PropAPI.Wrapper.CreateProp(out uint clone, state.Info.Prefab as PropInfo, newPosition, state.angle + deltaAngle, state.single))
            {
                InstanceID cloneID = default;
                cloneID.SetProp32(clone); // cloneID = PropLayer.Manager.SetProp(cloneID, clone);
                PropAPI.Wrapper.SetFixedHeight(clone, state.fixedHeight); // PropLayer.Manager.Buffer(cloneID).FixedHeight = state.fixedHeight;
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override Instance Clone(InstanceState instanceState, Dictionary<ushort, ushort> clonedNodes)
        {
            PropState state = instanceState as PropState;

            Instance cloneInstance = null;

            // Use new EML API
            //if (PropLayer.Manager.CreateProp(out uint clone, state.Info.Prefab as PropInfo, state.position, state.angle, state.single))
            if (PropAPI.Wrapper.CreateProp(out uint clone, state.Info.Prefab as PropInfo, state.position, state.angle, state.single))
            {
                InstanceID cloneID = default;
                cloneID.SetProp32(clone); // cloneID = PropLayer.Manager.SetProp(cloneID, clone);
                cloneInstance = new MoveableProp(cloneID);
            }

            return cloneInstance;
        }

        public override void Delete()
        {
            // Use new EML API
            if (isValid) PropAPI.Wrapper.ReleaseProp(id); // PropLayer.Manager.Buffer(id).ReleaseProp();
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            // Use new EML API
            PropInfo info = PropAPI.Wrapper.GetInfo(id); // IProp prop = PropLayer.Manager.Buffer(id);

            //Randomizer randomizer = new Randomizer(prop.Index);
            float scale = PropAPI.Wrapper.GetScale(id); // prop.Info.m_minScale + (float)randomizer.Int32(10000u) * (prop.Info.m_maxScale - prop.Info.m_minScale) * 0.0001f;
            float radius = EMath.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale; // Mathf.Max(prop.Info.m_generatedInfo.m_size.x, prop.Info.m_generatedInfo.m_size.z) * scale;

            return new Bounds(PropAPI.Wrapper.GetPosition(id)/*prop.Position*/, new Vector3(radius, 0, radius));
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;
            if (MoveItTool.m_isLowSensitivity) return;
            // Use new EML API
            //ushort prop = id.Prop;
            uint propID = id.GetProp32(); // IProp prop = PropLayer.Manager.Buffer(id);
            PropInfo propInfo = PropAPI.Wrapper.GetInfo(propID); // prop.Info;
            Vector3 position = PropAPI.Wrapper.GetPosition(propID); // prop.Position;
            float angle = PropAPI.Wrapper.GetAngle(propID); // prop.Angle;
            //Randomizer randomizer = new Randomizer(prop.Index);
            float scale = PropAPI.Wrapper.GetScale(propID); // propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
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
