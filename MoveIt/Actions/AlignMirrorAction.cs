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

            Matrix4x4 matrix4x = default(Matrix4x4);

            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    float faceOldAngle = (state.angle + (Mathf.PI / 2)) % TWO_PI;
                    if (faceOldAngle < 0) faceOldAngle += TWO_PI;

                    float faceNewAngle = faceOldAngle - ((faceOldAngle - mirrorAngle) * 2);
                    faceNewAngle = faceNewAngle % TWO_PI;
                    if (faceNewAngle < 0) faceNewAngle += TWO_PI;

                    float faceDelta = faceNewAngle - faceOldAngle;
                    // float angleDelta = (faceOldAngle - ((faceOldAngle - mirrorAngle) * 2)) - state.angle;

                    Vector3 posOffset = state.position - mirrorPivot;
                    float posAngle = Mathf.Atan2(posOffset.z, posOffset.x);
                    float posDelta = -((posAngle - mirrorAngle) * 2);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    state.instance.Transform(state, ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain);
                    Debug.Log($"{state.Info.Name}\n" +
                        $"Mirror:{mirrorPivot.x},{mirrorPivot.z}/{mirrorAngle} (follow:{followTerrain})\n" +
                        $"Old:{faceOldAngle}, New:{faceNewAngle} - delta:{faceDelta}\n" +
                        $"newAngle = {faceOldAngle} - (({faceOldAngle} - {mirrorAngle}) * 2)      [{faceOldAngle - mirrorAngle}]\n\n" +
                        $"posOffset:{posOffset.x},{posOffset.z} / {posAngle} - delta:{posDelta}\n" +
                        $"delta = -(({posAngle} - {mirrorAngle}) * 2)      [{posAngle - mirrorAngle}]");
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
