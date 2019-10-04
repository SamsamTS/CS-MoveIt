using ColossalFramework.UI;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MoveIt
{
    class ConvertToPOAction : Action
    {
        private HashSet<InstanceState> m_states = new HashSet<InstanceState>();
        private HashSet<Instance> m_clones = new HashSet<Instance>();
        private HashSet<Instance> m_oldSelection;
        private bool firstRun = true; // TODO this disables Redo

        public ConvertToPOAction()
        {
            foreach (Instance instance in selection)
            {
                //if ((instance is MoveableBuilding || instance is MoveableProp) && instance.isValid)
                {
                    m_states.Add(instance.GetState());
                }
            }
        }

        public override void Do()
        {
            if (m_states.Count < 20)
            {
                DoImplementation();
                return;
            }

            ConfirmPanel panel = UIView.library.ShowModal<ConfirmPanel>("ConfirmPanel", delegate (UIComponent comp, int value)
            {
                if (value == 1)
                    DoImplementation();
            });
            panel.SetMessage("Converting to PO", "Converting many objects to Procedural Objects at once can take along time. The game will appear to have frozen, but it will eventually complete. Continue?");
        }

        public void DoImplementation()
        {
            if (!firstRun) return; // Disables Redo
            m_clones.Clear();
            m_oldSelection = new HashSet<Instance>();

            foreach (InstanceState instanceState in m_states)
            {
                Instance instance = instanceState.instance;

                lock (instance.data)
                {
                    if (!instance.isValid)
                    {
                        continue;
                    }

                    if (!(instance is MoveableBuilding || instance is MoveableProp))
                    {
                        m_oldSelection.Add(instance);
                        continue;
                    }

                    IPO_Object obj = MoveItTool.PO.ConvertToPO(instance);
                    if (obj == null)
                    {
                        continue;
                    }

                    MoveItTool.PO.visibleObjects.Add(obj.Id, obj);

                    InstanceID instanceID = default;
                    instanceID.NetLane = obj.Id;
                    MoveableProc mpo = new MoveableProc(instanceID);
                    m_clones.Add(mpo);

                    mpo.angle = instance.angle;
                    mpo.position = instance.position;

                    selection.Add(mpo);
                    selection.Remove(instance);
                    instance.Delete();
                }
            }

            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public override void Undo()
        {
            firstRun = false; // TODO this disables Redo
            if (m_states == null) return;

            Dictionary<Instance, Instance> toReplace = new Dictionary<Instance, Instance>();
            foreach (Instance clone in m_clones)
            {
                MoveItTool.PO.visibleObjects.Remove(clone.id.NetLane);
                clone.Delete();
            }

            foreach (InstanceState state in m_states)
            {
                if (state.instance is MoveableBuilding || state.instance is MoveableProp)
                {
                    Instance clone = state.instance.Clone(state, null);
                    toReplace.Add(state.instance, clone);
                    m_oldSelection.Add(clone);
                }
            }

            ReplaceInstances(toReplace);
            ActionQueue.instance.ReplaceInstancesBackward(toReplace);

            selection = m_oldSelection;
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log($"ConvertToPO Replacing: {state.instance} ({state.instance.id.RawData}) -> {toReplace[state.instance]} ({toReplace[state.instance].id.RawData})");
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }

            foreach (Instance instance in toReplace.Keys)
            {
                if (m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log($"ConvertToPO Replacing: {instance} ({instance.id.RawData} -> {toReplace[instance]} ({toReplace[instance].id.RawData})");
                    m_oldSelection.Add(toReplace[instance]);
                }
            }
        }
    }
}
