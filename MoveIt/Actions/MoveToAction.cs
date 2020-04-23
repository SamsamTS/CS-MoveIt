using System.Collections.Generic;

namespace MoveIt
{
    public class MoveToAction : Action
    {
        public bool followTerrain;
        public float height;

        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public MoveToAction()
        {
            foreach (Instance instance in selection)
            {
                if (instance.isValid)
                {
                    m_states.Add(instance.SaveToState());
                }
            }
        }

        public override void Do()
        {
            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    state.instance.SetHeight(height);
                }
            }

            UpdateArea(GetTotalBounds(false));
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
                    DebugUtils.Log("MoveToAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }
}
