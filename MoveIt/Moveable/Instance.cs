using UnityEngine;

using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace MoveIt
{
    [XmlInclude(typeof(BuildingState)), XmlInclude(typeof(NodeState)), XmlInclude(typeof(PropState)), XmlInclude(typeof(SegmentState)), XmlInclude(typeof(TreeState))]
    public class InstanceState
    {
        [XmlIgnore]
        public Instance instance;

        [XmlIgnore]
        public IInfo Info;

        public Vector3 position;
        public float angle;
        public float terrainHeight;

        private string m_loadedName;

        public uint id
        {
            get
            {
                return instance.id.RawData;
            }

            set
            {
                InstanceID instanceID = InstanceID.Empty;
                instanceID.RawData = value;

                instance = instanceID;
            }
        }

        public string prefabName
        {
            get
            {
                if(Info != null)
                {
                    return Info.Name;
                }
                else
                {
                    return m_loadedName;
                }
            }

            set
            {
                m_loadedName = value;

                switch(instance.id.Type)
                {
                    case InstanceType.Building:
                        {
                            Info.Prefab = PrefabCollection<BuildingInfo>.FindLoaded(value);
                            break;
                        }
                    case InstanceType.Prop:
                        {
                            Info.Prefab = PrefabCollection<PropInfo>.FindLoaded(value);
                            break;
                        }
                    case InstanceType.Tree:
                        {
                            Info.Prefab = PrefabCollection<TreeInfo>.FindLoaded(value);
                            break;
                        }
                    case InstanceType.NetNode:
                    case InstanceType.NetSegment:
                        {
                            Info.Prefab = PrefabCollection<NetInfo>.FindLoaded(value);
                            break;
                        }
                }
            }
        }

        public virtual void ReplaceInstance(Instance newInstance)
        {
            instance = newInstance;

            if (newInstance.id.Type != instance.id.Type)
            {
                DebugUtils.Warning("Mismatching instances type ('" + newInstance.id.Type + "' -> '" + newInstance.id.Type + "').");
            }

            if (newInstance.Info != Info)
            {
                DebugUtils.Warning("Mismatching instances info ('" + Info.Name + "' -> '" + newInstance.Info.Name + "').");
            }
        }
    }

    public interface IInfo
    {
        string Name { get; }
        PrefabInfo Prefab { get; set; }
    }

    public class Info_Prefab : IInfo
    {
        public Info_Prefab(object i) => Prefab = (PrefabInfo)i;

        public string Name => Prefab.name;

        public PrefabInfo Prefab { get; set; } = null;
    }

    public abstract class Instance
    {
        protected static NetManager netManager = NetManager.instance;
        protected static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
        protected static NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

        public Instance(InstanceID instanceID)
        {
            id = instanceID;
        }


        public InstanceID id
        {
            get;
            protected set;
        }

        public abstract HashSet<ushort> segmentList
        {
            get;
        }

        public abstract Vector3 position { get; set; }

        public abstract float angle { get; set; }

        public abstract bool isValid { get; }

        public object data
        {
            get
            {
                switch (id.Type)
                {
                    case InstanceType.Building:
                        {
                            return BuildingManager.instance.m_buildings.m_buffer[id.Building];
                        }
                    case InstanceType.Prop:
                        {
                            return PropManager.instance.m_props.m_buffer[id.Prop];
                        }
                    case InstanceType.Tree:
                        {
                            return TreeManager.instance.m_trees.m_buffer[id.Tree];
                        }
                    case InstanceType.NetNode:
                        {
                            return NetManager.instance.m_nodes.m_buffer[id.NetNode];
                        }
                    case InstanceType.NetSegment:
                        {
                            return NetManager.instance.m_segments.m_buffer[id.NetSegment];
                        }
                }

                return null;
            }
        }

        private IInfo info;
        public IInfo Info { get => info; set => info = value; }


        //public Info_Wrapper Info
        //{
        //    get
        //    {
        //        switch (id.Type)
        //        {
        //            case InstanceType.Building:
        //                {
        //                    return BuildingManager.instance.m_buildings.m_buffer[id.Building].Info;
        //                }
        //            case InstanceType.Prop:
        //                {
        //                    return PropManager.instance.m_props.m_buffer[id.Prop].Info;
        //                }
        //            case InstanceType.Tree:
        //                {
        //                    return TreeManager.instance.m_trees.m_buffer[id.Tree].Info;
        //                }
        //            case InstanceType.NetNode:
        //                {
        //                    return NetManager.instance.m_nodes.m_buffer[id.NetNode].Info;
        //                }
        //            case InstanceType.NetSegment:
        //                {
        //                    return NetManager.instance.m_segments.m_buffer[id.NetSegment].Info;
        //                }
        //                //case InstanceType.NetLane:
        //                //    {
        //                //        return MoveItTool.PO.GetProcObj(id.NetLane).Info;
        //                //    }
        //        }

        //        return null;
        //    }
        //}

        public abstract InstanceState GetState();
        public abstract void SetState(InstanceState state);
        public abstract void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain);
        public abstract void Move(Vector3 location, float angle);
        public abstract void SetHeight(float height);
        public abstract Instance Clone(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes);
        public abstract Instance Clone(InstanceState state, Dictionary<ushort, ushort> clonedNodes);
        public abstract void Delete();
        public abstract Bounds GetBounds(bool ignoreSegments = true);
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor);
        public abstract void RenderCloneOverlay(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor);
        public abstract void RenderCloneGeometry(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor);

        public static implicit operator Instance(InstanceID id)
        {
            switch(id.Type)
            {
                case InstanceType.Building:
                    return new MoveableBuilding(id);
                case InstanceType.NetNode:
                    return new MoveableNode(id);
                case InstanceType.NetSegment:
                    return new MoveableSegment(id);
                case InstanceType.Prop:
                    return new MoveableProp(id);
                case InstanceType.Tree:
                    return new MoveableTree(id);
                case InstanceType.NetLane:
                    return new MoveableProc(id);
            }
            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is Instance instance)
            {
                return instance.id == id;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        protected static void UpdateSegmentBlocks(ushort segment, ref NetSegment data)
        {
            MoveItTool.instance.segmentsToUpdate.Add(segment);
            MoveItTool.instance.segmentUpdateCountdown = 1;
        }

        protected static void CalculateSegmentDirections(ref NetSegment segment, ushort segmentID)
        {
            if (segment.m_flags != NetSegment.Flags.None)
            {
                segment.m_startDirection.y = 0;
                segment.m_endDirection.y = 0;

                segment.m_startDirection.Normalize();
                segment.m_endDirection.Normalize();

                segment.m_startDirection = segment.FindDirection(segmentID, segment.m_startNode);
                segment.m_endDirection = segment.FindDirection(segmentID, segment.m_endNode);
            }
        }
    }
}
