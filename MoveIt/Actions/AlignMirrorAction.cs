using System;
using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class AlignMirrorAction : CloneAction
    {
        private bool containsNetwork = false;

        public Vector3 mirrorPivot;
        public float mirrorAngle;

        public override void Do()
        {
            //const float TWO_PI = (float)Math.PI * 2;

            Bounds originalBounds = GetTotalBounds(false);
            Matrix4x4 matrix4x = default(Matrix4x4);

            base.Do();

            foreach (Instance instance in m_clones)
            {
                if (instance.isValid)
                {
                    InstanceState state = null;

                    foreach (KeyValuePair<Instance, Instance> pair in m_clonedOrigin)
                    {
                        if (pair.Value.id.RawData == instance.id.RawData)
                        {                            
                            state = pair.Key.GetState();
                            break;
                        }
                    }
                    if (state == null)
                    {
                        throw new NullReferenceException($"Original for cloned object not found.");
                    }

                    state = instance.GetState();

                    //float faceOldAngle = state.angle % TWO_PI;
                    //if (faceOldAngle < 0) faceOldAngle += TWO_PI;

                    //float faceDelta = (faceOldAngle - ((faceOldAngle - mirrorAngle) * 2) - faceOldAngle) % TWO_PI;
                    float faceDelta = getMirrorFacingDelta(state.angle, mirrorPivot, mirrorAngle);

                    //Vector3 posOffset = state.position - mirrorPivot;
                    //float posAngle = -Mathf.Atan2(posOffset.x, posOffset.z);
                    //float posDelta = (mirrorAngle - posAngle) * 2;
                    float posDelta = getMirrorPositionDelta(state.position, mirrorPivot, mirrorAngle);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    instance.Transform(state, ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain);
                    Debug.Log($"{instance.Info.Name}\n" +
                        $"Mirror:{mirrorPivot.x},{mirrorPivot.z}/{mirrorAngle} (follow:{followTerrain})\n\n" +
                        $"Angle - Old:{state.angle}, New:{instance.angle}, Delta:{faceDelta}\n" +
                        $"Position - Old:{state.position}, New:{instance.position}, Delta:{posDelta}");
                        //$"Old:{faceOldAngle} - delta:{faceDelta}\n" +
                        //$"newAngle = {faceOldAngle} - (({faceOldAngle} - {mirrorAngle}) * 2)      [{faceOldAngle - mirrorAngle}]\n\n" +
                        //$"posOffset:{posOffset.x},{posOffset.z} / {posAngle} - delta:{posDelta}\n" +
                        //$"delta = ({mirrorAngle} - {posAngle}) * 2");
                }
            }

            bool fast = MoveItTool.fastMove != Event.current.shift;
            UpdateArea(originalBounds, !fast || containsNetwork);
            UpdateArea(GetTotalBounds(false), !fast);
        }

        public static float getMirrorFacingDelta(float startAngle, Vector3 mirrorOrigin, float mirrorAngle)
        {
            return (startAngle - ((startAngle - mirrorAngle) * 2) - startAngle) % ((float)Math.PI * 2);
        }

        public static float getMirrorPositionDelta(Vector3 start, Vector3 mirrorOrigin, float angle)
        {
            Vector3 offset = start - mirrorOrigin;
            float posAngle = -Mathf.Atan2(offset.x, offset.z);
            return (angle - posAngle) * 2;
        }
    }
}
