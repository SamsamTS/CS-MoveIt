using System;
using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class AlignMirrorAction : CloneAction
    {

        //public HashSet<InstanceState> m_states = new HashSet<InstanceState>();
        //protected HashSet<Instance> m_oldSelection;

        //public bool followTerrain;
        private bool containsNetwork = false;

        //public Vector3 center;
        public Vector3 mirrorPivot;
        public float mirrorAngle;

        //public AlignMirrorAction() : base()
        //{
            //m_oldSelection = selection;

            //HashSet<Instance> newSelection = CloneAction.GetCleanSelection(out center);
            //if (newSelection.Count == 0) return;

            //// Save states
            //foreach (Instance instance in newSelection)
            //{
            //    if (instance.isValid)
            //    {
            //        m_states.Add(instance.GetState());
            //    }
            //}


            //foreach (Instance instance in selection)
            //{
            //    if (instance.isValid)
            //    {
            //        m_states.Add(instance.GetState());

            //        if (instance is MoveableNode || instance is MoveableSegment)
            //        {
            //            containsNetwork = true;
            //        }
            //    }
            //}

            //center = GetCenter();
        //}

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
                    Debug.Log($"C1");
                    float faceOldAngle = (instance.angle + (Mathf.PI / 2)) % TWO_PI;
                    //if (faceOldAngle < 0) faceOldAngle += TWO_PI;

                    //float faceNewAngle = faceOldAngle - ((faceOldAngle - mirrorAngle) * 2);
                    //faceNewAngle = faceNewAngle % TWO_PI;
                    //if (faceNewAngle < 0) faceNewAngle += TWO_PI;

                    //float faceDelta = faceNewAngle - faceOldAngle;

                    float faceDelta = faceOldAngle - ((faceOldAngle - mirrorAngle) * 2) - faceOldAngle;

                    Debug.Log($"C2");
                    Vector3 posOffset = instance.position - mirrorPivot;
                    float posAngle = Mathf.Atan2(posOffset.z, posOffset.x);
                    float posDelta = -((posAngle - mirrorAngle) * 2);

                    matrix4x.SetTRS(mirrorPivot, Quaternion.AngleAxis(posDelta * Mathf.Rad2Deg, Vector3.down), Vector3.one);

                    instance.Transform(instance.GetState(), ref matrix4x, 0f, faceDelta, mirrorPivot, followTerrain, true);
                    Debug.Log($"C4");
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
            Debug.Log($"D");
            ActionQueue.instance.Invalidate();
            Debug.Log($"E");
        }

        //public override void Undo()
        //{
        //    Bounds bounds = GetTotalBounds(false);

        //    foreach (InstanceState state in m_states)
        //    {
        //        state.instance.SetState(state);
        //    }

        //    UpdateArea(bounds);
        //    UpdateArea(GetTotalBounds(false));
        //}

        //public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        //{
        //    foreach (InstanceState state in m_states)
        //    {
        //        if (toReplace.ContainsKey(state.instance))
        //        {
        //            DebugUtils.Log("MirrorAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
        //            state.ReplaceInstance(toReplace[state.instance]);
        //        }
        //    }
        //}
    }
}
