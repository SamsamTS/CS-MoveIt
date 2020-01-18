using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

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

        //long[] Ticks = new long[6];

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        internal bool _virtual = false;
        public bool Virtual
        {
            get => _virtual;
            set
            {
                if (value == true)
                {
                    if (_virtual == false)
                    {
                        _virtual = true;
                        foreach (Instance i in selection)
                        {
                            i.Virtual = true;
                        }
                    }
                }
                else
                {
                    if (_virtual == true)
                    {
                        _virtual = false;
                        foreach (Instance i in selection)
                        {
                            i.Virtual = false;
                        }
                        Do();
                        UpdateArea(GetTotalBounds(), true);
                    }
                }
            }
        }

        public TransformAction()
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
            //MoveItTool.instance.m_skipLowSensitivity = false;

            //for (int i = 0; i < Ticks.Length; i++)
            //{
            //    Ticks[i] = 0;
            //}
        }

        public override void Do()
        {
            //Ticks[0]++;
            //Stopwatch sw = Stopwatch.StartNew();
            //Stopwatch sw2 = Stopwatch.StartNew();

            Bounds originalBounds = GetTotalBounds(false);

            Matrix4x4 matrix4x = default;
            matrix4x.SetTRS(center + moveDelta, Quaternion.AngleAxis((angleDelta + snapAngle) * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            //sw.Stop();
            //Ticks[1] += sw.ElapsedTicks;
            //sw = Stopwatch.StartNew();

            foreach (InstanceState state in m_states)
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

            bool full = !(MoveItTool.fastMove != Event.current.shift);// || containsNetwork;
            //sw.Stop();
            //Ticks[2] += sw.ElapsedTicks;
            //sw = Stopwatch.StartNew();

            UpdateArea(originalBounds, full);

            //sw.Stop();
            //Ticks[3] += sw.ElapsedTicks;
            //sw = Stopwatch.StartNew();

            Bounds fullbounds = GetTotalBounds(false);

            //sw.Stop();
            //Ticks[4] += sw.ElapsedTicks;
            //sw = Stopwatch.StartNew();

            UpdateArea(fullbounds, full);

            //sw.Stop();
            //sw2.Stop();
            //Ticks[5] += sw.ElapsedTicks;

            //var sb = new System.Text.StringBuilder();
            //sb.Append($"Iterations:{Ticks[0]}, Selection-count:{m_states.Count}\n");
            //for (int i = 1; i < Ticks.Length; i++)
            //{
            //    float t = Ticks[i];
            //    if (i == 2)
            //    {
            //        float t2 = t / m_states.Count;
            //        sb.Append(string.Format("[A] Total:{0,9} - Avg:{1,9:F2}\n", t2, t2 / Ticks[0]));
            //    }
            //    sb.Append(string.Format("[{0}] Total:{1,9} - Avg:{2,9:F2}\n", i, t, t / Ticks[0]));
            //}
            //sb.Append(string.Format("  Overall:{0,9}", sw2.ElapsedTicks));
            //UnityEngine.Debug.Log(sb);
        }

        public override void Undo()
        {
            Bounds bounds = GetTotalBounds(false);

            foreach (InstanceState state in m_states)
            {
                state.instance.SetState(state);
            }

            UpdateArea(bounds, true);
            UpdateArea(GetTotalBounds(false), true);
        }

        public void InitialiseDrag()
        {
            MoveItTool.dragging = true;
            Virtual = false;

            foreach (InstanceState instanceState in m_states)
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
            MoveItTool.dragging = false;
            Virtual = false;

            foreach (InstanceState instanceState in m_states)
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
            foreach (InstanceState state in m_states)
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
            Matrix4x4 matrix4x = default;
            matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(deltaAngle * Mathf.Rad2Deg, Vector3.down), Vector3.one);

            HashSet<InstanceState> newStates = new HashSet<InstanceState>();

            foreach (InstanceState state in m_states)
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
