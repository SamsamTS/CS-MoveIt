﻿using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveIt
{
    class LineAction : Action
    {
        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public enum Modes
        {
            Spaced, Unspaced
        }
        public Modes mode = Modes.Spaced;

        private Instance[] keyInstance = new Instance[2];

        public Instance PointA
        {
            get
            {
                return keyInstance[0];
            }
            set
            {
                keyInstance[0] = value;
            }
        }
        public Instance PointB
        {
            get
            {
                return keyInstance[1];
            }
            set
            {
                keyInstance[1] = value;
            }
        }

        public bool followTerrain;

        public LineAction() : base()
        {
            affectsSegments = true;
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.SaveToState(false));
                }
            }
        }

        public override void Do()
        {
            if (selection.Count < 3) return;

            Dictionary<InstanceState, float> distances = new Dictionary<InstanceState, float>();
            Bounds originalBounds = GetTotalBounds(false);
            Matrix4x4 matrix4x = default;

            GetExtremeObjects(out keyInstance[0], out keyInstance[1]);
            double angleToB = GetAngleBetweenPointsRads(PointA.position, PointB.position);

            foreach (InstanceState state in m_states)
            {
                Instance inst = state.instance;

                if (inst is MoveableSegment || inst == PointA || inst == PointB)
                {
                    continue;
                }

                double angle = (GetAngleBetweenPointsRads(PointA.position, inst.position) - angleToB + (Mathf.PI * 2)) % (Mathf.PI * 2);
                float distance = Mathf.Cos((float)angle) * (inst.position - PointA.position).magnitude;
                distances.Add(state, distance);
            }

            IOrderedEnumerable<KeyValuePair<InstanceState, float>> sorted = distances.OrderBy(key => key.Value);
            Vector3 interval = (PointB.position - PointA.position) / (sorted.Count() + 1);
            float totalDistance = (PointB.position - PointA.position).magnitude;

            //string msg = $"{totalDistance}, {mode}";
            //foreach (KeyValuePair<InstanceState, float> pair in distances)
            //{
            //    Instance inst = pair.Key.instance;
            //    float d = pair.Value;

            //    msg += $"\n{inst}:{d}";
            //}
            //Log.Debug(msg);

            Vector3 cumulative = Vector3.zero;
            foreach (KeyValuePair<InstanceState, float> pair in sorted)
            {
                InstanceState state = pair.Key;
                float heightDelta;

                if (mode == Modes.Spaced)
                {
                    cumulative += interval;
                }
                else if (mode == Modes.Unspaced)
                {
                    cumulative = (PointB.position - PointA.position) * (pair.Value / totalDistance);
                }

                if (followTerrain)
                {
                    heightDelta = 0f;
                }
                else
                {
                    heightDelta = (PointA.position.y - state.position.y) + cumulative.y;
                }

                matrix4x.SetTRS(PointA.position + cumulative, Quaternion.AngleAxis(0f, Vector3.down), Vector3.one);
                state.instance.Transform(state, ref matrix4x, heightDelta, 0f, state.position, followTerrain);
            }

            UpdateArea(originalBounds, true);
            UpdateArea(GetTotalBounds(false), true);
        }

        public override void Undo()
        {
            foreach (InstanceState state in m_states)
            {
                state.instance.LoadFromState(state);
            }

            UpdateArea(GetTotalBounds(false));
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("LineAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }

        //private double GetAngleBetweenPoints(Vector3 a, Vector3 b)
        //{
        //    return (Math.Atan2(b.x - a.x, b.z - a.z) + (Mathf.PI * 2)) % (Mathf.PI * 2);
        //}
    }
}
