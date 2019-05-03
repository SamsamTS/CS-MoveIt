using System.Collections.Generic;

namespace MoveIt
{
    public class AlignTerrainHeightAction : Action
    {
        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();

        public AlignTerrainHeightAction()
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
            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    if (state.instance.id.Building > 0)
                    {
                        state.instance.SetHeight();
                    }
                }
            }
            foreach (InstanceState state in m_states)
            {
                if (state.instance.isValid)
                {
                    if (state.instance.id.Building == 0)
                    {
                        state.instance.SetHeight();
                    }
                }
            }

            UpdateArea(GetTotalBounds(false));
        }

        public override void Undo()
        {
            foreach (InstanceState state in m_states)
            {
                state.instance.SetState(state);
            }

            UpdateArea(GetTotalBounds(false));
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("AlignTerrainHeightAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }
        }
    }
}
