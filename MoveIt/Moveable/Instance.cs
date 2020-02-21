using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    [XmlInclude(typeof(BuildingState)), XmlInclude(typeof(NodeState)), XmlInclude(typeof(PropState)), XmlInclude(typeof(SegmentState)), XmlInclude(typeof(TreeState))]
    public class InstanceState
    {
        [XmlIgnore]
        public Instance instance;

        [XmlIgnore]
        public IInfo Info = new Info_Prefab();

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
                if (Info.Prefab != null)
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

                switch (instance.id.Type)
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

            if (newInstance.Info.Prefab != Info.Prefab)
            {
                DebugUtils.Warning($"Mismatching instances info:\n{Info.Prefab.name} <{Info.GetHashCode()}>\n{newInstance.Info.Prefab.name} <{newInstance.Info.GetHashCode()}>\n");
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
        public Info_Prefab() => Prefab = null;

        public string Name
        {
            get => (Prefab == null) ? "<null>" : Prefab.name;
        }

        public PrefabInfo Prefab { get; set; } = null;
    }

    public abstract class Instance
    {
        protected static NetManager netManager = NetManager.instance;
        protected static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
        protected static NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

        public List<Instance> subInstances = new List<Instance>();

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

        internal bool _virtual = false;
        public bool Virtual
        {
            get => _virtual;
            set
            {
                if (value == true)
                {
                    if (_virtual == false)
                    {
                        _virtual = true;
                        InitialiseTransform();
                        SetHidden(true);
                    }
                }
                else
                {
                    if (_virtual == true)
                    {
                        _virtual = false;
                        SetHidden(false);
                        Action.UpdateArea(Action.GetTotalBounds(), true);
                    }
                }
            }
        }

        internal virtual void InitialiseTransform()
        {
            TransformPosition = position;
            TransformAngle = angle;
        }

        internal virtual void SetHidden(bool hide) { }

        internal Building.Flags ToggleBuildingHiddenFlag(ushort id, bool hide)
        {
            if (hide)
            {
                if ((buildingBuffer[id].m_flags & Building.Flags.Hidden) == Building.Flags.Hidden)
                {
                    throw new Exception($"Building already hidden\n#{id}:{buildingBuffer[id].Info.name}");
                }

                return buildingBuffer[id].m_flags | Building.Flags.Hidden;
            }
            else
            {
                if ((buildingBuffer[id].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden)
                {
                    throw new Exception($"Building not hidden\n#{id}:{buildingBuffer[id].Info.name}");
                }
            }

            return buildingBuffer[id].m_flags & ~Building.Flags.Hidden;
        }

        public abstract Vector3 position { get; set; }

        public abstract float angle { get; set; }

        public virtual Vector3 TransformPosition { get; set; }

        public virtual float TransformAngle { get; set; }

        public Vector3 OverlayPosition
        {
            get
            {
                if (Virtual)
                    return TransformPosition;
                return position;
            }
        }

        public float OverlayAngle
        {
            get
            {
                if (Virtual)
                    return TransformAngle;
                return angle;
            }
        }

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

        private IInfo _info;
        public IInfo Info { get => _info; set => _info = value; }

        public abstract InstanceState GetState();
        public abstract void SetState(InstanceState state);
        public abstract void Transform(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain);
        public abstract void Move(Vector3 location, float angle);
        public abstract void SetHeight(float height);
        public abstract Instance Clone(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes, Action action);
        public abstract Instance Clone(InstanceState state, Dictionary<ushort, ushort> clonedNodes); // For Deletion Undo (bulldoze, convertToPO)
        public abstract void Delete();
        public abstract Bounds GetBounds(bool ignoreSegments = true);
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor);
        public abstract void RenderCloneOverlay(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor);
        public abstract void RenderCloneGeometry(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor);
        public virtual void RenderGeometry(RenderManager.CameraInfo cameraInfo, Color toolColor) { }

        public virtual void SetHeight()
        {
            //SetHeight(TerrainManager.instance.SampleDetailHeight(position));
            SetHeight(TerrainManager.instance.SampleRawHeightSmooth(position));
        }

        internal static bool isVirtual()
        {
            return ActionQueue.instance.current is TransformAction ta && ta.Virtual;
        }

        public static implicit operator Instance(InstanceID id)
        {
            switch (id.Type)
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
            MoveItTool.instance.segmentUpdateCountdown = 10;
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

        public override string ToString()
        {
            string msg = "";

            switch (id.Type)
            {
                case InstanceType.Building:
                    msg += "B" + id.Building;
                    break;

                case InstanceType.Prop:
                    msg += "P" + id.Prop;
                    break;

                case InstanceType.Tree:
                    msg += "T" + id.Tree;
                    break;

                case InstanceType.NetLane:
                    msg += "PO" + id.NetLane;
                    break;

                case InstanceType.NetNode:
                    msg += "N" + id.NetNode;
                    break;

                case InstanceType.NetSegment:
                    msg += "S" + id.NetSegment;
                    break;
            }

            return msg;
        }
    }
}
