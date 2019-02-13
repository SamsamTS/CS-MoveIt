using UnityEngine;

using System.Collections.Generic;

namespace MoveIt
{

    public class TransformAction : Action
    {
        public Vector3 moveDelta;
        public Vector3 center;
        public float angleDelta;
        public float snapAngle;
        public bool followTerrain;

        public bool autoCurve;
        public NetSegment segmentCurve;

        private bool containsNetwork = false;

        public HashSet<InstanceState> savedStates = new HashSet<InstanceState>();

        public TransformAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    savedStates.Add(instance.GetState());

                    if (instance is MoveableNode || instance is MoveableSegment)
                    {
                        containsNetwork = true;
                    }
                }
            }

            center = GetCenter();
        }

        public override void Do()
        {
            Bounds originalBounds = GetTotalBounds(false);

            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis((angleDelta + snapAngle) * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            foreach (InstanceState state in savedStates)
            {
                if (state.instance.isValid)
                {
                    state.instance.Transform(state, ref matrix4x, moveDelta.y, angleDelta + snapAngle, center, followTerrain);

                    if (autoCurve && state.instance is MoveableNode node)
                    {
                        node.AutoCurve(segmentCurve);
                    }
                }
            }

            UpdateArea(originalBounds, containsNetwork);
            UpdateArea(GetTotalBounds(false));
        }

        public override void Undo()
        {
            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in savedStates)
            {
                state.instance.SetState(state);
            }

            UpdateArea(bounds);
            UpdateArea(GetTotalBounds(false));
        }

        public void InitialiseDrag()
        {
            foreach (InstanceState instanceState in savedStates)
            {
                MoveableBuilding mb = instanceState.instance as MoveableBuilding;
                if (mb != null)
                {
                    mb.InitialiseDrag();
                }
            }
        }

        public void FinaliseDrag()
        {
            foreach (InstanceState instanceState in savedStates)
            {
                MoveableBuilding mb = instanceState.instance as MoveableBuilding;
                if (mb != null)
                {
                    mb.FinaliseDrag();
                }
            }
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in savedStates)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("TransformAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }

        public HashSet<InstanceState> CalculateStates(Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain)
        {
            Matrix4x4 matrix4x = default(Matrix4x4);
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in savedStates)
            {
                if (state.instance.isValid)
                {
                    InstanceState newState = new InstanceState();
                    newState.instance = state.instance;
                    newState.Info = state.Info;

                    newState.position = matrix4x.MultiplyPoint(state.position - center);
                    newState.position.y = state.position.y + deltaPosition.y;

                    if (followTerrain)
                    {
                        newState.terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newState.position);
                        newState.position.y = newState.position.y + newState.terrainHeight - state.terrainHeight;
                    }

                    newState.angle = state.angle + deltaAngle;

                    newStates.Add(newState);
                }
            }
            return newStates;
        }
    }
}
