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

        public void Add(Instance instance)
        {
            Debug.Log($"Adding:{instance.id}");
            if (!selection.Contains(instance))
            {
                m_newSelection.Add(instance);
            }
        }

        public void Remove(Instance instance)
        {
            m_newSelection.Remove(instance);
        }

        public override void Do()
        {
            if (MoveItTool.POProcessing) return;

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
}
