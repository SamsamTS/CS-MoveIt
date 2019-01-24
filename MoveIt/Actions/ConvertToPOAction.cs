using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class ConvertToPOAction : Action
    {
        private HashSet<Instance> m_oldSelection;

        public override void Do()
        {
            m_oldSelection = new HashSet<Instance>(selection);

            foreach (Instance instance in m_oldSelection)
            {
                if (!(instance is MoveableBuilding || instance is MoveableProp))
                {
                    continue;
                }

                IPO_Object obj = MoveItTool.PO.ConvertToPO(instance);
                MoveItTool.PO.visibleObjects.Add(obj.Id, obj);

                InstanceID instanceID = default(InstanceID);
                instanceID.NetLane = obj.Id;
                MoveableProc mpo = new MoveableProc(instanceID);

                mpo.angle = instance.angle;
                mpo.position = instance.position;

                selection.Add(mpo);
                selection.Remove(instance);
                instance.Delete();
            }
        }

        public override void Undo()
        { }

        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
        { }
    }
}
