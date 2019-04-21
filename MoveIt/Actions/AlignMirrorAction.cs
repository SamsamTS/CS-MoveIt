using System;
using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class AlignMirrorAction : Action
    {
        protected static Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
        protected static PropInstance[] propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;
        protected static TreeInstance[] treeBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public bool followTerrain;
        private bool containsNetwork = false;

        public Vector3 center;
        public Vector3 mirrorPivot;
        public float mirrorAngle;

        public AlignMirrorAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.GetState());

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
            const float TWO_PI = (float)Math.PI * 2;

            Bounds originalBounds = GetTotalBounds(false);

            Debug.Log($"Mirror:{mirrorPivot}/{mirrorAngle}");

            Matrix4x4 matrix4x = default(Matrix4x4);
            //matrix4x.SetTRS(mirrorPivot, Quaternion.Euler(0f, 0f, 0f), Vector3.one); //Axis(0f * Mathf.Rad2Deg, Vector3.down)

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    float oldAngle = state.angle % TWO_PI;
                    if (oldAngle < 0) oldAngle += TWO_PI;

                    float newAngle = oldAngle - ((oldAngle - mirrorAngle) * 2);
                    newAngle = newAngle % TWO_PI;
                    if (newAngle < 0) newAngle += TWO_PI;

                    float angleDelta = newAngle - oldAngle;
                    // float angleDelta = (state.angle - ((state.angle - mirrorAngle) * 2)) - state.angle;

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(mirrorAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    state.instance.Transform(state, ref matrix4x, 0f, angleDelta, mirrorPivot, followTerrain);
                    Debug.Log($"Mirror:{mirrorAngle}, Actual Old:{state.angle}, Actual New:{state.instance.angle}\n" +
                        $"Old:{oldAngle}, New:{newAngle}, Delta:{angleDelta}\n" +
                        $"{newAngle} = {oldAngle} - (({oldAngle} - {mirrorAngle}) * 2)      [{(oldAngle - mirrorAngle) * 2}]\n");
                }
            }

            bool fast = MoveItTool.fastMove != Event.current.shift;
            UpdateArea(originalBounds, !fast || containsNetwork);
            UpdateArea(GetTotalBounds(false), !fast);
        }

        public override void Undo()
        {
            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                state.instance.SetState(state);
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
                    DebugUtils.Log("MirrorAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }
}
