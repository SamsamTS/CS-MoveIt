using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt.Actions
{
    class AlignSlopeAction : Action
    {
        protected static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
        protected static PropInstance[] propBuffer = PropManager.instance.m_props.m_buffer;
        protected static TreeInstance[] treeBuffer = TreeManager.instance.m_trees.m_buffer;
        protected static NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
        protected static NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;

        protected InstanceID[] keyPoints = new InstanceID[2];

        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public AlignSlopeAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.GetState());
                }
            }
        }

        public override void Do()
        {
            throw new NotImplementedException();
        }

        public override void Undo()
        {
            throw new NotImplementedException();
        }


        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("AlignHeightAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }


        protected Vector3 getKeyPosition(int i)
        {
            if (keyPoints[i].Building > 0) return buildingBuffer[keyPoints[i].Building].m_position;
            if (keyPoints[i].Prop > 0) return propBuffer[keyPoints[i].Prop].Position;
            if (keyPoints[i].Tree > 0) return treeBuffer[keyPoints[i].Tree].Position;
            if (keyPoints[i].NetNode > 0) return nodeBuffer[keyPoints[i].NetNode].m_position;
            if (keyPoints[i].NetSegment > 0) return nodeBuffer[keyPoints[i].NetSegment].m_position;

            Debug.Log($"Index {i} fell through - {keyPoints[i].RawData}");
            return Vector3.zero;
        }
    }
}
