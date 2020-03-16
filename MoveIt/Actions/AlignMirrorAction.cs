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
            Bounds originalBounds = GetTotalBounds(false);
            Matrix4x4 matrix4x = default;

            base.Do();

            foreach (Instance instance in m_clones)
            {
                if (instance.isValid)
                {
                    InstanceState state = null;

                    foreach (KeyValuePair<Instance, Instance> pair in m_origToClone)
                    {
                        if (pair.Value.id.RawData == instance.id.RawData)
                        {               
                            if (pair.Value.id.NetSegment > 0)
                            { // Segments need original state because nodes move before clone's position is saved
                                state = pair.Key.SaveToState();
                            }
                            else
                            { // Buildings need clone state to access correct subInstances. Others don't matter, but clone makes most sense
                                state = pair.Value.SaveToState();
                            }
                            break;
                        }
                    }
                    if (state == null)
                    {
                        throw new NullReferenceException($"Original for cloned object not found.");
                    }


                    float faceDelta = getMirrorFacingDelta(state.angle, mirrorPivot, mirrorAngle);
                    float posDelta = getMirrorPositionDelta(state.position, mirrorPivot, mirrorAngle);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    instance.Transform(state, ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain);
                    //Debug.Log($"{instance.Info.Name}\n" +
                    //    $"Mirror:{mirrorPivot.x},{mirrorPivot.z}/{mirrorAngle} (follow:{followTerrain})\n\n" +
                    //    $"Angle - Old:{state.angle}, New:{instance.angle}, Delta:{faceDelta}\n" +
                    //    $"Position - Old:{state.position}, New:{instance.position}, Delta:{posDelta}");
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
            return (angle + Mathf.Atan2(offset.x, offset.z)) * 2;
        }
    }
}
