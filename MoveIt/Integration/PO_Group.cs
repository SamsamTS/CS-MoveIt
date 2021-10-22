using System;
using System.Collections.Generic;

namespace MoveIt
{
    internal class PO_Group
    {
        internal List<PO_Object> objects = new List<PO_Object>();
        internal PO_Object root = null;
        internal int count;
        internal Type tPO = null, tPOGroup = null;

        public PO_Group(object group)
        {
            tPO = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");
            tPOGroup = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.POGroup");

            var objList = tPOGroup.GetField("objects").GetValue(group);
            count = (int)objList.GetType().GetProperty("Count").GetValue(objList, null);

            for (int i = 0; i < count; i++)
            {
                var v = objList.GetType().GetMethod("get_Item").Invoke(objList, new object[] { i });
                PO_Object obj = MoveItTool.PO.GetProcObj(Convert.ToUInt32(tPO.GetField("id").GetValue(v)) + 1);
                obj.Group = this;
                objects.Add(obj);

                if (obj.isGroupRoot())
                {
                    root = obj;
                }
            }

            //string msg = $"AAA1 - Count:{count}\n";
            //foreach (PO_Object o in objects)
            //{
            //    msg += $"{o.Id} ({o.Group}), ";
            //}
            //msg += "\n\n";
            //foreach (Instance instance in Action.selection)
            //{
            //    if (instance is MoveableProc mpo)
            //    {
            //        msg += $"{mpo.m_procObj.Id} ({mpo.m_procObj.Group}), ";
            //    }
            //}
            //Log.Debug(msg);
        }
    }
}
