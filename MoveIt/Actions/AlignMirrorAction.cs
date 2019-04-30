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
            const float TWO_PI = (float)Math.PI * 2;

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

                    float faceOldAngle = (state.angle + (Mathf.PI / 2)) % TWO_PI;
                    //if (faceOldAngle < 0) faceOldAngle += TWO_PI;

                    //float faceNewAngle = faceOldAngle - ((faceOldAngle - mirrorAngle) * 2);
                    //faceNewAngle = faceNewAngle % TWO_PI;
                    //if (faceNewAngle < 0) faceNewAngle += TWO_PI;

                    //float faceDelta = faceNewAngle - faceOldAngle;

                    float faceDelta = faceOldAngle - ((faceOldAngle - mirrorAngle) * 2) - faceOldAngle;

                    Vector3 posOffset = state.position - mirrorPivot;
                    float posAngle = Mathf.Atan2(posOffset.z, posOffset.x);
                    float posDelta = -((posAngle - mirrorAngle) * 2);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    instance.Transform(state, ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain, true);
                    //Debug.Log($"{instance.Info.Name}\n" +
                    //    $"Mirror:{mirrorPivot.x},{mirrorPivot.z}/{mirrorAngle} (follow:{followTerrain})\n" +
                    //    $"Old:{faceOldAngle} - delta:{faceDelta}\n" +
                    //    //$"Old:{faceOldAngle}, New:{faceNewAngle} - delta:{faceDelta}\n" +
                    //    $"newAngle = {faceOldAngle} - (({faceOldAngle} - {mirrorAngle}) * 2)      [{faceOldAngle - mirrorAngle}]\n\n" +
                    //    $"posOffset:{posOffset.x},{posOffset.z} / {posAngle} - delta:{posDelta}\n" +
                    //    $"delta = -(({posAngle} - {mirrorAngle}) * 2)      [{posAngle - mirrorAngle}]");
                }
            }

            bool fast = MoveItTool.fastMove != Event.current.shift;
            UpdateArea(originalBounds, !fast || containsNetwork);
            UpdateArea(GetTotalBounds(false), !fast);

            //ActionQueue.instance.Invalidate();
        }
    }
}
