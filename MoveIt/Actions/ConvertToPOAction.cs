using System.Collections.Generic;
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
                if ((instance is MoveableBuilding || instance is MoveableProp) && instance.isValid)
                {
                    m_states.Add(instance.GetState());
                }
            }
        }

        public override void Do()
        {
            if (!firstRun) return; // TODO this disables Redo
            m_clones.Clear();
            m_oldSelection = new HashSet<Instance>(selection);

            //string msg = "Selection\n";
            //foreach (Instance i in m_oldSelection)
            //{
            //    msg += $"{i.id.Prop}:{i.Info.Name}\n";
            //}
            //Debug.Log(msg);

            //Debug.Log($"state: {MoveItTool.instance.ToolState}");
            //if (MoveItTool.instance.ToolState != MoveItTool.ToolStates.Default)
            //{
            //    return;
            //}

            foreach (InstanceState instanceState in m_states)
            {
                Instance instance = instanceState.instance;

                if (!((instance is MoveableBuilding || instance is MoveableProp) || !instance.isValid))
                {
                    continue;
                }

                IPO_Object obj = MoveItTool.PO.ConvertToPO(instance);
                if (obj == null)
                {
                    continue;
                }

                MoveItTool.PO.visibleObjects.Add(obj.Id, obj);

                InstanceID instanceID = default(InstanceID);
                instanceID.NetLane = obj.Id;
                MoveableProc mpo = new MoveableProc(instanceID);
                m_clones.Add(mpo);

                mpo.angle = instance.angle;
                mpo.position = instance.position;

                selection.Add(mpo);
                selection.Remove(instance);
                instance.Delete();
                MoveItTool.m_debugPanel.Update();
            }
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

            //string msg = "Clones:\n";
            //foreach (Instance c in m_clones)
            //{
            //    msg += $"{c.id.NetLane}:{c.Info.Name}\n";
            //}

            //msg += "Cloned ids\n";
            foreach (InstanceState state in m_states)
            {
                Instance clone = state.instance.Clone(state, null);
                toReplace.Add(state.instance, clone);
            //    msg += $"{clone.id.Prop}:{state.instance.GetType()},{clone.GetType()},{clone.Info.Name}\n";
            }
            //Debug.Log(msg);

            ReplaceInstances(toReplace);
            ActionQueue.instance.ReplaceInstancesBackward(toReplace);

            selection = m_oldSelection;
            MoveItTool.m_debugPanel.Update();

            //msg = "\"old\" Selection\n";
            //foreach (Instance i in m_oldSelection)
            //{
            //    msg += $"{i.id.Prop}:{i.Info.Name}\n";
            //}
            //Debug.Log(msg);
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            //string msg = "STATES\n";
            //foreach (InstanceState i in m_states)
            //{
            //    msg += $"{i.id}:{i.Info.Name}\n";
            //}
            //Debug.Log(msg);
            //msg = "REPLACE\n";
            //foreach (KeyValuePair<Instance,Instance> kvp in toReplace)
            //{
            //    msg += $"{kvp.Key.id.Prop}:{kvp.Value.id.Prop}\n";
            //}
            //Debug.Log(msg);

            foreach (InstanceState state in m_states)
            {
                if (toReplace.ContainsKey(state.instance))
                {
                    DebugUtils.Log("ConvertToPO Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
                    state.ReplaceInstance(toReplace[state.instance]);
                }
            }

            foreach (Instance instance in toReplace.Keys)
            {
                if (m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("ConvertToPO Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }
            }
        }
    }
}
