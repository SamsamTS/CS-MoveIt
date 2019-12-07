using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Serialization;

using ColossalFramework;
using ColossalFramework.Math;


namespace MoveIt
{
    public class BuildingState : InstanceState
    {
        public Building.Flags flags;
        public int length;

        [XmlElement("subStates")]
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
                ushort building = buildingBuffer[id.Building].m_subBuilding;
                int count = 0;
                while (building != 0)
                {
                    InstanceID buildingID = default;
                    buildingID.Building = building;

                    yield return new MoveableBuilding(buildingID);
                    building = buildingBuffer[building].m_subBuilding;

                    if (++count > 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Buildings: Invalid list detected!\n" + Environment.StackTrace);
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
                        InstanceID nodeID = default;
                        nodeID.NetNode = node;
                        yield return new MoveableNode(nodeID);
                    }

                    node = nodeBuffer[node].m_nextBuildingNode;
                    if ((nodeBuffer[node].m_flags & NetNode.Flags.Created) != NetNode.Flags.Created)
                    {
                        node = 0;
                    }

                    if (++count > 32768)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Nodes: Invalid list detected!\n" + Environment.StackTrace);
                        break;
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
                        InstanceID nodeID = default;
                        nodeID.NetNode = node;
                        segments.UnionWith(new MoveableNode(nodeID).segmentList);
                    }

                    node = nodeBuffer[node].m_nextBuildingNode;

                    if (++count > 32768)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Nodes: Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }

                return segments;
            }
        }

        public MoveableBuilding(InstanceID instanceID) : base(instanceID)
        {
            //if ((BuildingManager.instance.m_buildings.m_buffer[instanceID.Building].m_flags & Building.Flags.Created) == Building.Flags.None)
            //{
            //    throw new Exception($"Building #{instanceID.Building} not found!");
            //}
            Info = new Info_Prefab(BuildingManager.instance.m_buildings.m_buffer[instanceID.Building].Info);
        }

        public override InstanceState GetState()
        {
            return GetBuildingState(0);
        }

        public InstanceState GetBuildingState(int depth)
        { 
            BuildingState state = new BuildingState
            {
                instance = this,
                Info = Info,
                position = buildingBuffer[id.Building].m_position,
                angle = buildingBuffer[id.Building].m_angle,
                flags = buildingBuffer[id.Building].m_flags,
                length = buildingBuffer[id.Building].Length
            };
            state.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(state.position);

            List<InstanceState> subStates = new List<InstanceState>();

            foreach (Instance subInstance in subInstances)
            {
                if (subInstance != null && subInstance.isValid)
                {
                    if (subInstance.id.Building > 0)
                    {
                        if (depth < 1)
                        {
                            subStates.Add(((MoveableBuilding)subInstance).GetBuildingState(depth + 1));
                        }
                    }
                    else
                    {
                        subStates.Add(subInstance.GetState());
                    }
                }
            }

            if (subStates.Count > 0)
                state.subStates = subStates.ToArray();

            return state;
        }

        public override void SetState(InstanceState state)
        {
            if (!(state is BuildingState buildingState)) return;

            ushort building = buildingState.instance.id.Building;

            buildingBuffer[building].m_flags = buildingState.flags;
            AddFixedHeightFlag(building);
            RelocateBuilding(building, ref buildingBuffer[building], buildingState.position, buildingState.angle);

            if (buildingState.subStates != null)
            {
                foreach (InstanceState subState in buildingState.subStates)
                {
                    subState.instance.SetState(subState);
                }
            }
            buildingBuffer[building].m_flags = buildingState.flags;
        }

        public override Vector3 position
        {
            get
            {
                if (id.IsEmpty) return Vector3.zero;
                return buildingBuffer[id.Building].m_position;
            }
            set
            {
                if (id.IsEmpty) return;
                buildingBuffer[id.Building].m_position = value;
            }
        }

        public override float angle
        {
            get
            {
                if (id.IsEmpty) return 0f;
                return buildingBuffer[id.Building].m_angle;
            }
            set
            {
                if (id.IsEmpty) return;
                buildingBuffer[id.Building].m_angle = value;
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

        public int Length
        {
            get
            {
                return buildingBuffer[id.Building].m_length;
            }
        }

        private bool _virtual = false;
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
                        SetHiddenFlag(true);
                    }
                }
                else
                {
                    if (_virtual == true)
                    { 
                        _virtual = false;
                        SetHiddenFlag(false);
                        Action.UpdateArea(Action.GetTotalBounds(), true);
                    }
                }
            }
        }

        public override void Transform(InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            BuildingState state = instanceState as BuildingState;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            bool isFixed = GetFixedHeightFlag(id.Building);
            if (!isFixed) AddFixedHeightFlag(id.Building);

            if (followTerrain)
            {
                newPosition.y = newPosition.y + terrainHeight - state.terrainHeight;
            }

            AddFixedHeightFlag(id.Building);
            Move(newPosition, state.angle + deltaAngle);

            Matrix4x4 matrixSub = default;
            matrixSub.SetTRS(Vector3.zero, Quaternion.AngleAxis(deltaAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            if (state.subStates != null)
            {
                foreach (InstanceState subState in state.subStates)
                {
                    Vector3 subOffset = (subState.position - center) - (state.position - center);
                    Vector3 subPosition = position + matrixSub.MultiplyPoint(subOffset);

                    subPosition.y = subState.position.y - state.position.y + newPosition.y;

                    subState.instance.Move(subPosition, subState.angle + deltaAngle);
                    if (subState.instance is MoveableNode mn)
                    {
                        if (mn.Pillar != null)
                        {
                            mn.Pillar.Move(subPosition, subState.angle + deltaAngle);
                        }
                    }

                    if (subState is BuildingState bs)
                    {
                        if (bs.subStates != null)
                        {
                            foreach (InstanceState subSubState in bs.subStates)
                            {
                                Vector3 subSubOffset = (subSubState.position - center) - (state.position - center);
                                Vector3 subSubPosition = position + matrixSub.MultiplyPoint(subSubOffset);

                                subSubPosition.y = subSubState.position.y - state.position.y + newPosition.y;

                                subSubState.instance.Move(subSubPosition, subSubState.angle + deltaAngle);
                                if (subSubState.instance is MoveableNode mn2)
                                {
                                    if (mn2.Pillar != null)
                                    {
                                        mn2.Pillar.Move(subSubPosition, subSubState.angle + deltaAngle);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!isFixed && Mathf.Abs(terrainHeight - newPosition.y) < 0.01f)
            {
                RemoveFixedHeightFlag(id.Building);
            }

            //if (!isFixed) RemoveFixedHeightFlag(id.Building);
            //if (Mathf.Abs(terrainHeight - newPosition.y) > 0.01f)
            //{
            //    AddFixedHeightFlag(id.Building);
            //}
            //else
            //{
            //    RemoveFixedHeightFlag(id.Building);
            //}
        }

        private void SetHiddenFlag(bool hide)
        {
            buildingBuffer[id.Building].m_flags = ToggleHiddenFlag(id.Building, hide);

            foreach (Instance sub in subInstances)
            {
                if (sub is MoveableNode mn)
                {
                    if (mn.Pillar != null)
                    {
                        buildingBuffer[mn.Pillar.id.Building].m_flags = ToggleHiddenFlag(mn.Pillar.id.Building, hide);
                    }
                }

                if (sub is MoveableBuilding bs)
                {
                    buildingBuffer[sub.id.Building].m_flags = ToggleHiddenFlag(sub.id.Building, hide);

                    Building subBuilding = (Building)sub.data;

                    foreach (Instance subSub in bs.subInstances)
                    {
                        if (subSub is MoveableNode mn2)
                        {
                            if (mn2.Pillar != null)
                            {
                                buildingBuffer[mn2.Pillar.id.Building].m_flags = ToggleHiddenFlag(mn2.Pillar.id.Building, hide);
                            }
                        }
                    }
                }
            }
        }

        private static Building.Flags ToggleHiddenFlag(ushort id, bool hide)
        {
            if (hide)
            {
                if ((buildingBuffer[id].m_flags & Building.Flags.Hidden) == Building.Flags.Hidden)
                {
                    throw new Exception($"Building already hidden");
                }

                return buildingBuffer[id].m_flags | Building.Flags.Hidden;
            }
            else
            {
                if ((buildingBuffer[id].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden)
                {
                    throw new Exception($"Building not hidden");
                }
            }

            return buildingBuffer[id].m_flags & ~Building.Flags.Hidden;
        }

        public void InitialiseDrag()
        {
            Virtual = false;

            Bounds bounds = new Bounds(position, new Vector3(Length, 0, Length));
            bounds.Expand(64f);
            Action.UpdateArea(bounds);
        }

        public void FinaliseDrag()
        {
            Virtual = false;

            Bounds bounds = new Bounds(position, new Vector3(Length, 0, Length));
            bounds.Expand(64f);
            Action.UpdateArea(bounds);
        }

        public override void Move(Vector3 location, float angle)
        {
            if (!isValid) return;

            //Singleton<BuildingManager>.instance.RelocateBuilding(id.Building, location, angle);
            RelocateBuilding(id.Building, ref buildingBuffer[id.Building], location, angle);
        }

        public override void SetHeight(float height)
        {
            Vector3 newPosition = position;

            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            bool isFixed = GetFixedHeightFlag(id.Building);
            if (!isFixed) AddFixedHeightFlag(id.Building);

            foreach (Instance subInstance in subInstances)
            {
                Vector3 subPosition = subInstance.position;
                subPosition.y = subPosition.y - newPosition.y + height;

                subInstance.Move(subPosition, subInstance.angle);
            }

            newPosition.y = height;
            Move(newPosition, angle);

            if (!isFixed && Mathf.Abs(terrainHeight - newPosition.y) < 0.01f)
            {
                RemoveFixedHeightFlag(id.Building);
            }
        }

        public override void SetHeight()
        {
            Building b = (Building)data;
            b.m_baseHeight = 0;
            SetHeight(TerrainManager.instance.SampleOriginalRawHeightSmooth(position));
        }

        public override Instance Clone(InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes, Action action)
        {
            BuildingState state = instanceState as BuildingState;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);

            if (followTerrain)
            {
                newPosition.y = newPosition.y + terrainHeight - state.terrainHeight;
            }
            MoveableBuilding cloneInstance = null;
            BuildingInfo info = state.Info.Prefab as BuildingInfo;

            float newAngle = state.angle + deltaAngle;
            if (BuildingManager.instance.CreateBuilding(out ushort clone, ref SimulationManager.instance.m_randomizer,
                info, newPosition, newAngle,
                state.length, SimulationManager.instance.m_currentBuildIndex))
            {
                SimulationManager.instance.m_currentBuildIndex++;

                InstanceID cloneID = default;
                cloneID.Building = clone;
                cloneInstance = new MoveableBuilding(cloneID);

                if ((state.flags & Building.Flags.Completed) != Building.Flags.None)
                {
                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.Completed;
                }
                if ((state.flags & Building.Flags.FixedHeight) != Building.Flags.None)
                {
                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.FixedHeight;
                }
                if ((state.flags & Building.Flags.Historical) != Building.Flags.None)
                {
                    buildingBuffer[clone].m_flags = buildingBuffer[clone].m_flags | Building.Flags.Historical;
                }

                // TODO: when should the flag be set?
                if (Mathf.Abs(terrainHeight - newPosition.y) > 0.01f)
                {
                    AddFixedHeightFlag(clone);
                }
                else
                {
                    RemoveFixedHeightFlag(clone);
                }

                if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
                {
                    Matrix4x4 subMatrix4x = default;
                    subMatrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                    for (int i = 0; i < info.m_subBuildings.Length; i++)
                    {
                        BuildingInfo subInfo = info.m_subBuildings[i].m_buildingInfo;
                        Vector3 subPosition = subMatrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                        float subAngle = info.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;

                        if (BuildingManager.instance.CreateBuilding(out ushort subClone, ref SimulationManager.instance.m_randomizer,
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

        // For Deletion Undo
        public override Instance Clone(InstanceState instanceState, Dictionary<ushort, ushort> clonedNodes)
        {
            BuildingState state = instanceState as BuildingState;

            MoveableBuilding cloneInstance = null;
            BuildingInfo info = state.Info.Prefab as BuildingInfo;

            if (BuildingManager.instance.CreateBuilding(out ushort clone, ref SimulationManager.instance.m_randomizer,
                info, state.position, state.angle,
                state.length, SimulationManager.instance.m_currentBuildIndex))
            {
                SimulationManager.instance.m_currentBuildIndex++;

                InstanceID cloneID = default;
                cloneID.Building = clone;
                cloneInstance = new MoveableBuilding(cloneID);

                buildingBuffer[clone].m_flags = state.flags;

                if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
                {
                    Matrix4x4 subMatrix4x = default;
                    subMatrix4x.SetTRS(state.position, Quaternion.AngleAxis(state.angle * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                    for (int i = 0; i < info.m_subBuildings.Length; i++)
                    {
                        BuildingInfo subInfo = info.m_subBuildings[i].m_buildingInfo;
                        Vector3 subPosition = subMatrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                        float subAngle = info.m_subBuildings[i].m_angle * 0.0174532924f + state.angle;

                        if (BuildingManager.instance.CreateBuilding(out ushort subClone, ref SimulationManager.instance.m_randomizer,
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
            if (isValid)
            {
                SimulationManager.instance.AddAction(() =>
                {
                    BuildingManager.instance.ReleaseBuilding(id.Building);
                });
            }
        }

        public void AddFixedHeightFlag(ushort building)
        {
            buildingBuffer[building].m_flags = buildingBuffer[building].m_flags | Building.Flags.FixedHeight;
        }

        public void RemoveFixedHeightFlag(ushort building)
        {
            buildingBuffer[building].m_flags = buildingBuffer[building].m_flags & ~Building.Flags.FixedHeight;
        }

        public bool GetFixedHeightFlag(ushort building)
        {
            return (buildingBuffer[building].m_flags & Building.Flags.FixedHeight) == Building.Flags.FixedHeight;
        }

        public override Bounds GetBounds(bool ignoreSegments = true)
        {
            return GetBuildingBounds(0, ignoreSegments);
        }

        public Bounds GetBuildingBounds(int depth, bool ignoreSegments = true)
        {
            BuildingInfo info = buildingBuffer[id.Building].Info;

            float radius = Mathf.Max(info.m_cellWidth * 4f, info.m_cellLength * 4f);
            Bounds bounds = new Bounds(buildingBuffer[id.Building].m_position, new Vector3(radius, 0, radius));

            if (depth < 1)
            { 
                foreach (Instance subInstance in subInstances)
                {
                    if (subInstance.id.Building > 0)
                    {
                            bounds.Encapsulate(((MoveableBuilding)subInstance).GetBuildingBounds(depth + 1, ignoreSegments));
                    }
                    else
                    {
                        bounds.Encapsulate(subInstance.GetBounds(ignoreSegments));
                    }
                }
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
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Nodes: Invalid list detected!\n" + Environment.StackTrace);
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
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Buildings: Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        public override void RenderCloneOverlay(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            BuildingState state = instanceState as BuildingState;

            BuildingInfo buildingInfo = state.Info.Prefab as BuildingInfo;

            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y - state.terrainHeight + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            }

            float newAngle = state.angle + deltaAngle;

            buildingInfo.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, newPosition, newAngle, default);
            BuildingTool.RenderOverlay(cameraInfo, buildingInfo, state.length, newPosition, newAngle, toolColor, false);
            if (buildingInfo.m_subBuildings != null && buildingInfo.m_subBuildings.Length != 0)
            {
                Matrix4x4 subMatrix4x = default;
                subMatrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                for (int i = 0; i < buildingInfo.m_subBuildings.Length; i++)
                {
                    BuildingInfo buildingInfo2 = buildingInfo.m_subBuildings[i].m_buildingInfo;
                    Vector3 position = subMatrix4x.MultiplyPoint(buildingInfo.m_subBuildings[i].m_position);
                    float angle = buildingInfo.m_subBuildings[i].m_angle * 0.0174532924f + newAngle;
                    buildingInfo2.m_buildingAI.RenderBuildOverlay(cameraInfo, toolColor, position, angle, default);
                    BuildingTool.RenderOverlay(cameraInfo, buildingInfo2, 0, position, angle, toolColor, true);
                }
            }
        }

        public override void RenderCloneGeometry(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor)
        {
            RenderCloneGeometryImplementation(instanceState, ref matrix4x, deltaPosition, deltaAngle, center, followTerrain, cameraInfo);
        }

        public static void RenderCloneGeometryImplementation(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo)
        {
            BuildingInfo info = instanceState.Info.Prefab as BuildingInfo;
            Color color = GetColor(instanceState.instance.id.Building, info);

            Vector3 newPosition = matrix4x.MultiplyPoint(instanceState.position - center);
            newPosition.y = instanceState.position.y + deltaPosition.y;

            if (followTerrain)
            {
                newPosition.y = newPosition.y - instanceState.terrainHeight + TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);
            }

            float newAngle = instanceState.angle + deltaAngle;

            info.m_buildingAI.RenderBuildGeometry(cameraInfo, newPosition, newAngle, 0);
            BuildingTool.RenderGeometry(cameraInfo, info, info.GetLength(), newPosition, newAngle, false, color);
            if (info.m_subBuildings != null && info.m_subBuildings.Length != 0)
            {
                Matrix4x4 subMatrix4x = default;
                subMatrix4x.SetTRS(newPosition, Quaternion.AngleAxis(newAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                for (int i = 0; i < info.m_subBuildings.Length; i++)
                {
                    BuildingInfo buildingInfo2 = info.m_subBuildings[i].m_buildingInfo;
                    Vector3 position = subMatrix4x.MultiplyPoint(info.m_subBuildings[i].m_position);
                    float angle = info.m_subBuildings[i].m_angle * Mathf.Deg2Rad + newAngle;
                    buildingInfo2.m_buildingAI.RenderBuildGeometry(cameraInfo, position, angle, 0);
                    BuildingTool.RenderGeometry(cameraInfo, buildingInfo2, 0, position, angle, true, color);
                }
            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo, Color toolColor, int depth = 0)
        {
            BuildingInfo buildingInfo = Info.Prefab as BuildingInfo;
            Color color = GetColor(id.Building, buildingInfo);

            buildingInfo.m_buildingAI.RenderBuildGeometry(cameraInfo, position, angle, 0);
            BuildingTool.RenderGeometry(cameraInfo, buildingInfo, Length, position, angle, false, color);

            if (depth < 1)
            {
                foreach (Instance subInstance in subInstances)
                {
                    MoveableBuilding msb = subInstance as MoveableBuilding;

                    if (msb != null)
                    {
                        msb.RenderGeometry(cameraInfo, toolColor, depth + 1);
                    }
                }
            }
        }

        internal static Color GetColor(ushort buildingID, BuildingInfo info)
        {
            if (!info.m_useColorVariations)
            {
                return info.m_color0;
            }
            Randomizer randomizer = new Randomizer((int)buildingID);
            switch (randomizer.Int32(4u))
            {
                case 0:
                    return info.m_color0;
                case 1:
                    return info.m_color1;
                case 2:
                    return info.m_color2;
                case 3:
                    return info.m_color3;
                default:
                    return info.m_color0;
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

            //if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            //{
            //    Debug.Log($"PARKING (RB)\n#{building}:{info.name}");
            //    BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            //}

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
