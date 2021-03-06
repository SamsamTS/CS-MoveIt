using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    public class SelectAction : Action
    {
        private HashSet<Instance> m_oldSelection;
        private HashSet<Instance> m_newSelection;

        public SelectAction(bool append = false)
        {
            m_oldSelection = selection;

            if (append && selection != null)
            {
                m_newSelection = new HashSet<Instance>(selection);
            }
            else
            {
                m_newSelection = new HashSet<Instance>();
            }

            selection = m_newSelection;
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        // Used by Prop Painter
        public void Add(Instance instance)
        {
            if (!selection.Contains(instance))
            {
                m_newSelection.AddObject(instance);
            }
        }

        public void Remove(Instance instance)
        {
            m_newSelection.RemoveObject(instance);
        }

        public override void Do()
        {
            selection = m_newSelection;
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public override void Undo()
        {
            selection = m_oldSelection;
            MoveItTool.m_debugPanel.UpdatePanel();
        }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        {
            foreach (Instance instance in toReplace.Keys)
            {
                if (m_oldSelection.Remove(instance))
                {
                    DebugUtils.Log("SelectAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_oldSelection.Add(toReplace[instance]);
                }

                if (m_newSelection.Remove(instance))
                {
                    DebugUtils.Log("SelectAction Replacing: " + instance.id.RawData + " -> " + toReplace[instance].id.RawData);
                    m_newSelection.Add(toReplace[instance]);
                }
            }
        }
    }

    public static class MyExtensions
    {
        public static void AddObject(this HashSet<Instance> selection, Instance ins)
        {
            selection.Add(ins);

            // Add the rest of the PO group
            if (ins is MoveableProc mpo)
            {
                if (mpo.m_procObj.Group == null)
                    return;

                foreach (PO_Object po in mpo.m_procObj.Group.objects)
                {
                    if (po.Id != mpo.m_procObj.Id)
                    {
                        InstanceID insId = default;
                        insId.NetLane = po.Id;
                        selection.Add(new MoveableProc(insId));
                    }
                }
            }
        }

        public static void RemoveObject(this HashSet<Instance> selection, Instance ins)
        {
            selection.Remove(ins);

            // Add the rest of the PO group
            if (ins is MoveableProc mpo)
            {
                if (mpo.m_procObj.Group == null)
                    return;

                foreach (PO_Object po in mpo.m_procObj.Group.objects)
                {
                    if (po.Id != mpo.m_procObj.Id)
                    {
                        InstanceID insId = default;
                        insId.NetLane = po.Id;
                        selection.Remove(new MoveableProc(insId));
                    }
                }
            }
        }
    }
}
