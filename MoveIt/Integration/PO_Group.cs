using System;
using System.Collections.Generic;

namespace MoveIt
{
    internal class PO_Group
    {
        internal List<PO_Object> objects = new List<PO_Object>();
        internal PO_Object root = null;
        internal Type tPO = null, tPOGroup = null, tPOLogic = null;
        internal readonly object POGroup = null;

        /// <summary>
        /// Create a new PO Group
        /// </summary>
        public PO_Group()
        {
            tPOLogic = PO_Logic.POAssembly.GetType("ProceduralObjects.ProceduralObjectsLogic");

            object groupList = tPOLogic.GetField("groups").GetValue(PO_Logic.POLogic);
            POGroup = tPOLogic.Assembly.CreateInstance("ProceduralObjects.Classes.POGroup");

            groupList.GetType().GetMethod("Add", new Type[] { PO_Logic.tPOGroup }).Invoke(groupList, new[] { POGroup });
            MoveItTool.PO.Groups.Add(this);
        }

        /// <summary>
        /// Load an existing group
        /// </summary>
        /// <param name="group">The PO Group data</param>
        public PO_Group(object group)
        {
            tPO = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");
            tPOGroup = PO_Logic.POAssembly.GetType("ProceduralObjects.Classes.POGroup");
            tPOLogic = PO_Logic.POAssembly.GetType("ProceduralObjects.ProceduralObjectsLogic");
            POGroup = group;

            var objList = tPOGroup.GetField("objects").GetValue(group);
            int count = (int)objList.GetType().GetProperty("Count").GetValue(objList, null);

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
        }

        /// <summary>
        /// Change a group's root PO object
        /// </summary>
        /// <param name="newRoot">The PO object to set as root</param>
        internal void SetNewRoot(PO_Object newRoot)
        {
            if (root != null)
            {
                root.SetGroupRoot(false);
            }
            root = newRoot;
            POGroup.GetType().GetField("root").SetValue(POGroup, newRoot.procObj);
            root.SetGroupRoot(true);
        }

        /// <summary>
        /// Add an object to this group
        /// </summary>
        /// <param name="obj">The PO_Object</param>
        internal void AddObject(PO_Object obj)
        {
            objects.Add(obj);

            // Add object to group
            var objList = POGroup.GetType().GetField("objects").GetValue(POGroup);
            objList.GetType().GetMethod("Add").Invoke(objList, new[] { obj.procObj });

            // Add group to object
            var groupField = obj.procObj.GetType().GetField("group");
            groupField.SetValue(obj.procObj, POGroup);
        }
    }
}
