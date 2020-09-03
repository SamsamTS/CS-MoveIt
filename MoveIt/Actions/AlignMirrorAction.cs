using System;
using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class AlignMirrorAction : CloneActionBase
    {
        private bool containsNetwork = false;

        public Vector3 mirrorPivot;
        public float mirrorAngle;
        private Bounds originalBounds;

        public AlignMirrorAction() : base() {}

        public override void Do()
        {
            originalBounds = GetTotalBounds(false);

            base.Do();
        }

        public void DoProcess()
        {
            Matrix4x4 matrix4x = default;
            foreach (Instance instance in m_clones)
            {
                if (instance.isValid)
                {
                    InstanceState state = null;

                    foreach (KeyValuePair<Instance, Instance> pair in m_origToClone)
                    {
                        if (pair.Value.id.RawData == instance.id.RawData)
                        {               
                            if (pair.Value is MoveableSegment)
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

                    float faceDelta = getMirrorFacingDelta(state.angle, mirrorAngle);
                    float posDelta = getMirrorPositionDelta(state.position, mirrorPivot, mirrorAngle);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    instance.Transform(state, ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain);
                }
            }

            // Mirror integrations
            foreach (var item in m_stateToClone)
            {
                foreach (var data in item.Key.IntegrationData)
                {
                    try
                    {
                        CallIntegration(data.Key, item.Value.id, data.Value, m_InstanceID_origToClone);
                        //data.Key.Mirror(item.Value.id, data.Value, m_InstanceID_origToClone);
                    }
                    catch (MissingMethodException e)
                    {
                        Debug.Log($"Failed to find Mirror method, a mod {data.Key.Name} needs updated.\n{e}");
                    }
                    catch (Exception e)
                    {
                        InstanceID sourceInstanceID = item.Key.instance.id;
                        InstanceID targetInstanceID = item.Value.id;
                        Debug.LogError($"integration {data.Key} Failed to paste from " +
                            $"{sourceInstanceID.Type}:{sourceInstanceID.Index} to {targetInstanceID.Type}:{targetInstanceID.Index}");
                        DebugUtils.LogException(e);
                    }
                }
            }

            bool fast = MoveItTool.fastMove != Event.current.shift;
            UpdateArea(originalBounds, !fast || containsNetwork);
            UpdateArea(GetTotalBounds(false), !fast);
        }

        private void CallIntegration(MoveItIntegration.MoveItIntegrationBase method, InstanceID id, object data, Dictionary<InstanceID, InstanceID> map)
        {
            method.Mirror(id, data, map);
        }

        public static float getMirrorFacingDelta(float startAngle, float mirrorAngle)
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
