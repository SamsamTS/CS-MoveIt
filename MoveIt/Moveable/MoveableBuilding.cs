using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.Math;


namespace MoveIt
{
    public class BuildingState : InstanceState
    {
        public Building.Flags flags;
        public int length;

        public InstanceState[] subStates;
        
        public override void ReplaceInstance(Instance instance)
        {
            base.ReplaceInstance(instance);

            MoveableBuilding building = instance as MoveableBuilding;

            int count = 0;
            foreach(Instance subInstance in building.subInstances)
            {
                subStates[count++].instance = subInstance;
            }
        }
    }

    public class MoveableBuilding : Instance
    {
        public IEnumerable<Instance> subInstances
        {
            get
            {
                if (buildingBuffer[id.Building].m_parentBuilding == 0)
                {
                    ushort building = buildingBuffer[id.Building].m_subBuilding;
                    int count = 0;
                    while (building != 0)
                    {

                        InstanceID buildingID = default(InstanceID);
                        buildingID.Building = building;

                        yield return new MoveableBuilding(buildingID);
                        building = buildingBuffer[building].m_subBuilding;

                        if (++count > 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }

                    ushort node = buildingBuffer[id.Building].m_netNode;
                    count = 0;
                    while (node != 0)
                    {
                        ItemClass.Layer layer = nodeBuffer[node].Info.m_class.m_layer;
                        if (layer != ItemClass.Layer.PublicTransport)
                        {
                            InstanceID nodeID = default(InstanceID);
                            nodeID.NetNode = node;
                            yield return new MoveableNode(nodeID);
                        }

                        node = nodeBuffer[node].m_nextBuildingNode;

                        if (++count > 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        public override HashSet<ushort> segmentList
        {
            get
            {
                HashSet<ushort> segments = new HashSet<ushort>();

                ushort node = buildingBuffer[id.Building].m_netNode;
                int count = 0;
                while (node != 0)
                {
                    ItemClass.Layer layer = nodeBuffer[node].Info.m_class.m_layer;
                    if (layer != ItemClass.Layer.PublicTransport)
                    {
                        InstanceID nodeID = default(InstanceID);
                        nodeID.NetNode = node;
                        segments.UnionWith(new MoveableNode(nodeID).segmentList);
                    }

                    node = nodeBuffer[node].m_nextBuildingNode;

                    if (++count > 32768)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }

                return segments;
            }
        }

        public MoveableBuilding(InstanceID instanceID) : base(instanceID) { }

        public override InstanceState GetState()
        {
            BuildingState state = new BuildingState();

            state.instance = this;
            state.info = info;

            state.position = buildingBuffer[id.Building].m_position;
            state.angle = buildingBuffer[id.Building].m_angle;
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);
            state.flags = buildingBuffer[id.Building].m_flags;
            state.length = buildingBuffer[id.Building].Length;

            List<InstanceState> subStates = new List<InstanceState>();

            foreach (Instance instance in subInstances)
            {
                if (instance != null && instance.isValid)
                {
                    subStates.Add(instance.GetState());
                }
            }

            if (subStates.Count > 0)
                state.subStates = subStates.ToArray();

            return state;
        }

        public override void SetState(InstanceState state)
        {
            BuildingState buildingState = state as BuildingState;
            if (buildingState == null) return;

            ushort building = buildingState.instance.id.Building;

            buildingBuffer[building].m_flags = buildingState.flags;
            RelocateBuilding(building, ref buildingBuffer[building], buildingState.position, buildingState.angle);

            if (buildingState.subStates != null)
            {
                foreach (InstanceState subState in buildingState.subStates)
                {
                    subState.instance.SetState(subState);
                }
            }
        }

        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return buildingBuffer[id.Building].m_position;
            }
        }

        public override float angle
        {
            get
            {
                if (id.IsEmpty) return 0f;
                return buildingBuffer[id.Building].m_angle;
            }
        }

        public override bool isValid
        {
            get
            {
                if (id.IsEmpty) return false;
                return (buildingBuffer[id.Building].m_flags & Building.Flags.Created) != Building.Flags.None;
            }
        }

        public override void Transform(InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            BuildingState state = instanceState as BuildingState;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);

            if (followTerrain)
            {
                newPosition.y = newPosition.y + terrainHeight - state.terrainHeight;
            }

            // TODO: when should the flag be set?
            if (Mathf.Abs(terrainHeight - newPosition.y) > 0.01f)
            {
                AddFixedHeightFlag();
            }
            else
            {
                RemoveFixedHeightFlag();
            }

            Move(newPosition, state.angle + deltaAngle);

            if (state.subStates != null)
            {
                foreach (InstanceState subState in state.subStates)
                {
                    Vector3 subPosition = subState.position - center;
                    subPosition = matrix4x.MultiplyPoint(subPosition);
                    subPosition.y = subState.position.y - state.position.y + newPosition.y;

                    subState.instance.Move(subPosition, subState.angle + deltaAngle);
                }
            }
        }

        public override void Move(Vector3 location, float angle)
        {
            if (!isValid) return;

            RelocateBuilding(id.Building, ref buildingBuffer[id.Building], location, angle);
        }

        public override void SetHeight(float height)
        {
            Vector3 newPosition = position;

            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);

            // TODO: when should the flag be set?
            if (Mathf.Abs(terrainHeight - height) > 0.01f)
            {
                AddFixedHeightFlag();
            }
            else
            {
                RemoveFixedHeightFlag();
            }

            foreach (Instance subInstance in subInstances)
            {
                Vector3 subPosition = subInstance.position;
                subPosition.y = subPosition.y - newPosition.y + height;

                subInstance.Move(subPosition, subInstance.angle);
            }

            newPosition.y = height;
            Move(newPosition, angle);
        }

        public override Instance Clone(InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes)
        {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            if (followTerrain)
            {
                newPosition.y = newPosition.y + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition) - state.terrainHeight;
            }

            MoveableBuilding cloneInstance = null;

            ushort building = id.Building;
            BuildingInfo info = buildingBuffer[building].Info;

            if (buildingBuffer[building].FindParentNode(building) == 0)
            {
                float newAngle = buildingBuffer[building].m_angle + deltaAngle;
                ushort clone;
                if (BuildingManager.instance.CreateBuilding(out clone, ref SimulationManager.instance.m_randomizer,
                    info, newPosition, newAngle,
                    buildingBuffer[building].Length, SimulationManager.instance.m_currentBuildIndex))
                {
                    SimulationManager.instance.m_currentBuildIndex++;

                    InstanceID cloneID = default(InstanceID);
                    cloneID.Building = clone;
                    cloneInstance = new MoveableBuilding(cloneID);

                    if ((buildingBuffer[building].m_flags & Building.Flags.Completed) != Building.Flags.None)
                    {
                        buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.Completed;
                    }
                    if ((buildingBuffer[building].m_flags & Building.Flags.FixedHeight) != Building.Flags.None)
                    {
                        buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.FixedHeight;
                    }

                    if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
                    {
                        Matrix4x4 subMatrix4x = default(Matrix4x4);
                        subMatrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * 57.29578f, Vector3.down), Vector3.one);
                        for (int i = 0; i < info.m_subBuildings.Length; i++)
                        {
                            BuildingInfo subInfo = info.m_subBuildings[i].m_buildingInfo;
                            Vector3 subPosition = subMatrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                            float subAngle = info.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;

                            ushort subClone;
                            if (BuildingManager.instance.CreateBuilding(out subClone, ref SimulationManager.instance.m_randomizer,
                                subInfo, subPosition, subAngle, 0, SimulationManager.instance.m_currentBuildIndex))
                            {
                                SimulationManager.instance.m_currentBuildIndex++;
                                if (info.m_subBuildings[i].m_fixedHeight)
                                {
                                    buildingBuffer[subClone].m_flags = buildingBuffer[subClone].m_flags | Building.Flags.FixedHeight;
                                }
                            }
                            if (clone != 0 && subClone != 0)
                            {
                                buildingBuffer[clone].m_subBuilding = subClone;
                                buildingBuffer[subClone].m_parentBuilding = clone;
                                buildingBuffer[subClone].m_flags = buildingBuffer[subClone].m_flags | Building.Flags.Untouchable;
                                clone = subClone;
                            }
                        }
                    }
                }
            }

            return cloneInstance;
        }

        public override Instance Clone(InstanceState instanceState)
        {
            BuildingState state = instanceState as BuildingState;

            MoveableBuilding cloneInstance = null;
            BuildingInfo info = state.info as BuildingInfo;

            ushort clone;
            if (BuildingManager.instance.CreateBuilding(out clone, ref SimulationManager.instance.m_randomizer,
                info, state.position, state.angle,
                state.length, SimulationManager.instance.m_currentBuildIndex))
            {
                SimulationManager.instance.m_currentBuildIndex++;

                InstanceID cloneID = default(InstanceID);
                cloneID.Building = clone;
                cloneInstance = new MoveableBuilding(cloneID);

                buildingBuffer[clone].m_flags = state.flags;

                if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
                {
                    Matrix4x4 subMatrix4x = default(Matrix4x4);
                    subMatrix4x.SetTRS(state.position, Quaternion.AngleAxis(state.angle * 57.29578f, Vector3.down), Vector3.one);
                    for (int i = 0; i < info.m_subBuildings.Length; i++)
                    {
                        BuildingInfo subInfo = info.m_subBuildings[i].m_buildingInfo;
                        Vector3 subPosition = subMatrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                        float subAngle = info.m_subBuildings[i].m_angle * 0.0174532924f + state.angle;

                        ushort subClone;
                        if (BuildingManager.instance.CreateBuilding(out subClone, ref SimulationManager.instance.m_randomizer,
                            subInfo, subPosition, subAngle, 0, SimulationManager.instance.m_currentBuildIndex))
                        {
                            SimulationManager.instance.m_currentBuildIndex++;
                            if (info.m_subBuildings[i].m_fixedHeight)
                            {
                                buildingBuffer[subClone].m_flags = buildingBuffer[subClone].m_flags | Building.Flags.FixedHeight;
                            }
                        }
                        if (clone != 0 && subClone != 0)
                        {
                            buildingBuffer[clone].m_subBuilding = subClone;
                            buildingBuffer[subClone].m_parentBuilding = clone;
                            buildingBuffer[subClone].m_flags = buildingBuffer[subClone].m_flags | Building.Flags.Untouchable;
                            clone = subClone;
                        }
                    }
                }
            }

            return cloneInstance;
        }

        public override void Delete()
        {
            if (isValid) BuildingManager.instance.ReleaseBuilding(id.Building);
        }

        public void AddFixedHeightFlag()
        {
            buildingBuffer[id.Building].m_flags = buildingBuffer[id.Building].m_flags | Building.Flags.FixedHeight;
        }

        public void RemoveFixedHeightFlag()
        {
            buildingBuffer[id.Building].m_flags = buildingBuffer[id.Building].m_flags & ~Building.Flags.FixedHeight;
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            BuildingInfo info = buildingBuffer[id.Building].Info;

            float radius = Mathf.Max(info.m_cellWidth * 4f, info.m_cellLength * 4f);
            Bounds bounds = new Bounds(buildingBuffer[id.Building].m_position, new Vector3(radius, 0, radius));

            foreach (Instance subInstance in subInstances)
            {
                bounds.Encapsulate(subInstance.GetBounds(ignoreSegments));
            }

            return bounds;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
        {
            if (!isValid) return;

            ushort building = id.Building;
            BuildingInfo buildingInfo = buildingBuffer[building].Info;

            if (WillBuildingDespawn(building))
            {
                toolColor = despawnColor;
            }

            float alpha = 1f;
            BuildingTool.CheckOverlayAlpha(buildingInfo, ref alpha);
            toolColor.a *= alpha;

            int length = buildingBuffer[building].Length;
            Vector3 position = buildingBuffer[building].m_position;
            float angle = buildingBuffer[building].m_angle;
            BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, position, angle, toolColor, false);

            ushort node = buildingBuffer[building].m_netNode;
            int count = 0;
            while (node != 0)
            {
                for (int k = 0; k < 8; k++)
                {
                    ushort segment2 = netManager.m_nodes.m_buffer[node].GetSegment(k);
                    if (segment2 != 0 && netManager.m_segments.m_buffer[segment2].m_startNode == node && (netManager.m_segments.m_buffer[segment2].m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
                    {
                        NetTool.RenderOverlay(cameraInfo, ref netManager.m_segments.m_buffer[segment2], toolColor, toolColor);
                    }
                }
                node = netManager.m_nodes.m_buffer[node].m_nextBuildingNode;

                if (++count > 32768)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            ushort subBuilding = buildingBuffer[building].m_subBuilding;
            count = 0;
            while (subBuilding != 0)
            {
                BuildingInfo subBuildingInfo = buildingBuffer[subBuilding].Info;
                int subLength = buildingBuffer[subBuilding].Length;
                Vector3 subPosition = buildingBuffer[subBuilding].m_position;
                float subAngle = buildingBuffer[subBuilding].m_angle;
                BuildingTool.RenderOverlay(cameraInfo, subBuildingInfo, subLength, subPosition, subAngle, toolColor, false);
                subBuilding = buildingBuffer[subBuilding].m_subBuilding;

                if (++count > 49152)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public override void RenderCloneOverlay(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            ushort building = id.Building;

            BuildingInfo buildingInfo = buildingBuffer[building].Info;
            int length = buildingBuffer[building].Length;
            Color color = buildingInfo.m_buildingAI.GetColor(0, ref buildingBuffer[building], InfoManager.instance.CurrentMode);

            buildingInfo.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, state.position, state.angle, default(Segment3));
            BuildingTool.RenderOverlay(cameraInfo, buildingInfo, length, state.position, state.angle, toolColor, false);
            if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
            {
                Matrix4x4 subMatrix4x = default(Matrix4x4);
                subMatrix4x.SetTRS(state.position, Quaternion.AngleAxis(state.angle * 57.29578f, Vector3.down), Vector3.one);
                for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                {
                    BuildingInfo buildingInfo2 = buildingInfo.m_subBuildings[i].m_buildingInfo;
                    Vector3 position = subMatrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                    float angle = buildingInfo.m_subBuildings[i].m_angle * 0.0174532924f + state.angle;
                    buildingInfo2.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, position, angle, default(Segment3));
                    BuildingTool.RenderOverlay(cameraInfo, buildingInfo2, 0, position, angle, toolColor, true);
                }
            }
        }

        public override void RenderCloneGeometry(InstanceState state, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            ushort building = id.Building;

            BuildingInfo buildingInfo = buildingBuffer[building].Info;
            int length = buildingBuffer[building].Length;
            float angle = buildingBuffer[building].m_angle;
            Color color = buildingInfo.m_buildingAI.GetColor(0, ref buildingBuffer[building], InfoManager.instance.CurrentMode);

            buildingInfo.m_buildingAI.RenderBuildGeometry(cameraInfo, state.position, state.angle, 0);
            BuildingTool.RenderGeometry(cameraInfo, buildingInfo, length, state.position, state.angle, false, color);
            if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
            {
                Matrix4x4 subMatrix4x = default(Matrix4x4);
                subMatrix4x.SetTRS(state.position, Quaternion.AngleAxis(state.angle * 57.29578f, Vector3.down), Vector3.one);
                for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                {
                    BuildingInfo buildingInfo2 = buildingInfo.m_subBuildings[i].m_buildingInfo;
                    Vector3 position = subMatrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                    angle = buildingInfo.m_subBuildings[i].m_angle * 0.0174532924f + state.angle;
                    buildingInfo2.m_buildingAI.RenderBuildGeometry(cameraInfo, position, angle, 0);
                    BuildingTool.RenderGeometry(cameraInfo, buildingInfo2, 0, position, angle, true, color);
                }
            }
        }

        private bool WillBuildingDespawn(ushort building)
        {
            BuildingInfo info = buildingBuffer[building].Info;

            ItemClass.Zone zone1 = info.m_class.GetZone();
            ItemClass.Zone zone2 = info.m_class.GetSecondaryZone();

            if (info.m_placementStyle != ItemClass.Placement.Automatic || zone1 == ItemClass.Zone.None)
            {
                return false;
            }

            info.m_buildingAI.CheckRoadAccess(building, ref buildingBuffer[building]);
            if ((buildingBuffer[building].m_problems & Notification.Problem.RoadNotConnected) == Notification.Problem.RoadNotConnected ||
                !buildingBuffer[building].CheckZoning(zone1, zone2, true))
            {
                return true;
            }

            return false;
        }

        private void RelocateBuilding(ushort building, ref Building data, Vector3 position, float angle)
        {
            BuildingInfo info = data.Info;
            RemoveFromGrid(building, ref data);
            if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            {
                BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            }

            data.m_position = position;
            data.m_angle = angle;

            AddToGrid(building, ref data);
            data.CalculateBuilding(building);
            BuildingManager.instance.UpdateBuildingRenderer(building, true);
        }

        private static void AddToGrid(ushort building, ref Building data)
        {
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(BuildingManager.instance.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                buildingBuffer[(int)building].m_nextGridBuilding = BuildingManager.instance.m_buildingGrid[num3];
                BuildingManager.instance.m_buildingGrid[num3] = building;
            }
            finally
            {
                Monitor.Exit(BuildingManager.instance.m_buildingGrid);
            }
        }

        private static void RemoveFromGrid(ushort building, ref Building data)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                ushort num4 = 0;
                ushort num5 = buildingManager.m_buildingGrid[num3];
                int num6 = 0;
                while (num5 != 0)
                {
                    if (num5 == building)
                    {
                        if (num4 == 0)
                        {
                            buildingManager.m_buildingGrid[num3] = data.m_nextGridBuilding;
                        }
                        else
                        {
                            buildingBuffer[(int)num4].m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = buildingBuffer[(int)num5].m_nextGridBuilding;
                    if (++num6 > 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                data.m_nextGridBuilding = 0;
            }
            finally
            {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
            if (info != null)
            {
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 270, num2 * 45 / 270, info.m_prefabDataLayer);
            }
        }
    }
}
