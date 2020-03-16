using System;
using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    public class AlignIndividualAction : AlignRotationAction
    {
    }

    public class AlignGroupAction : AlignRotationAction
    {
    }

    public class AlignRandomAction : AlignRotationAction
    {
    }

    public class AlignRotationAction : Action
    {
        public float newAngle;
        public bool followTerrain;
        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();
        protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

        public AlignRotationAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.SaveToState());
                }
            }
        }


        public override void Do()
        {
            Vector3 PoR;
            Matrix4x4 matrix = default;
            Bounds bounds = GetTotalBounds(true, true);
            float angleDelta, firstValidAngle = 0;
            System.Random random = new System.Random();
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    if (state.instance is MoveableBuilding || state.instance is MoveableProp || state.instance is MoveableProc)
                    {
                        firstValidAngle = state.angle;
                        break;
                    }
                }
            }
            angleDelta = 0 - firstValidAngle + newAngle;
            PoR = bounds.center;

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    if (state.instance is MoveableBuilding mb)
                    {
                        //BuildingState bs = (BuildingState)state;

                        if (Mathf.Abs(Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(mb.position) - mb.position.y) > 0.01f)
                        {
                            mb.AddFixedHeightFlag(mb.id.Building);
                        }
                        else
                        {
                            mb.RemoveFixedHeightFlag(mb.id.Building);
                        }

                        if (this is AlignIndividualAction)
                        {
                            angleDelta = 0 - mb.angle + newAngle;
                            PoR = state.position;
                        }
                        else if (this is AlignRandomAction)
                        {
                            angleDelta = 0 - mb.angle + (float)(random.NextDouble() * Math.PI * 2);
                            PoR = state.position;
                        }

                        matrix.SetTRS(PoR, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                        mb.Transform(state, ref matrix, 0f, angleDelta, PoR, followTerrain);

                        BuildingInfo prefab = (BuildingInfo)state.Info.Prefab;
                        ushort id = mb.id.Building;
                        Building building = buildingManager.m_buildings.m_buffer[id];

                        if (prefab.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
                        {
                            buildingManager.UpdateParkingSpaces(id, ref building);
                        }

                        buildingManager.UpdateBuildingRenderer(id, true);
                    }
                    else if (state.instance is MoveableProp mp)
                    {
                        if (this is AlignIndividualAction)
                        {
                            angleDelta = 0 - mp.angle + newAngle;
                            PoR = state.position;
                        }
                        else if (this is AlignRandomAction)
                        {
                            angleDelta = 0 - mp.angle + (float)(random.NextDouble() * Math.PI * 2);
                            PoR = state.position;
                        }
                        matrix.SetTRS(PoR, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                        mp.Transform(state, ref matrix, 0f, angleDelta, PoR, followTerrain);
                    }
                    else if (state.instance is MoveableProc mpo)
                    {
                        if (this is AlignIndividualAction)
                        {
                            angleDelta = 0 - mpo.angle + newAngle;
                            PoR = state.position;
                        }
                        else if (this is AlignRandomAction)
                        {
                            angleDelta = 0 - mpo.angle + (float)(random.NextDouble() * Math.PI * 2);
                            PoR = state.position;
                        }
                        matrix.SetTRS(PoR, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                        mpo.Transform(state, ref matrix, 0f, angleDelta, PoR, followTerrain);
                    }
                    else if (state.instance is MoveableTree mt)
                    {
                        if (this is AlignIndividualAction || this is AlignRandomAction)
                        {
                            continue;
                        }
                        matrix.SetTRS(PoR, Quaternion.AngleAxis(angleDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);
                        mt.Transform(state, ref matrix, 0f, angleDelta, PoR, followTerrain);
                    }
                }
            }

            MoveItTool.instance.ToolState = MoveItTool.ToolStates.Default;
            MoveItTool.instance.AlignMode = MoveItTool.AlignModes.Off;
            MoveItTool.instance.AlignToolPhase = 0;
            UIMoreTools.UpdateMoreTools();
            UpdateArea(bounds);
            UpdateArea(GetTotalBounds(false));
        }


        public override void Undo()
        {
            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                state.instance.LoadFromState(state);
            }

            UpdateArea(bounds);
            UpdateArea(GetTotalBounds(false));
        }


        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("AlignRotationAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }
}
