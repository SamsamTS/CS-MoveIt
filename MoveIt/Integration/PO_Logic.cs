using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace MoveIt
{
    internal class PO_Logic : MonoBehaviour
    {
        internal static Assembly POAssembly = null;
        protected Type _tPOLogic = null;
        internal static Type tPOLogic = null, tPOMod = null, tPO = null, tPInfo = null, tPUtils = null, tVertex = null, tPOMoveIt = null, tPOGroup = null;
        internal static object POLogic = null;
        internal static PO_Object PObuffer = null;
        internal bool POHasFilters = true;
        internal bool POHasGroups = true;
        //internal List<PO_Group> Groups;

        private readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        private void Start()
        {
            POAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Length >= 17 && assembly.FullName.Substring(0, 17) == "ProceduralObjects")
                {
                    POAssembly = assembly;
                    break;
                }
            }

            if (POAssembly == null)
            {
                throw new NullReferenceException("PO Assembly not found [PO-F3]");
            }

            tPOLogic = POAssembly.GetType("ProceduralObjects.ProceduralObjectsLogic");
            tPOMod = POAssembly.GetType("ProceduralObjects.ProceduralObjectsMod");
            tPO = POAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");
            tPInfo = POAssembly.GetType("ProceduralObjects.Classes.ProceduralInfo");
            tPUtils = POAssembly.GetType("ProceduralObjects.Classes.ProceduralUtils");
            tVertex = POAssembly.GetType("ProceduralObjects.Classes.Vertex");
            tPOMoveIt = POAssembly.GetType("ProceduralObjects.Classes.PO_MoveIt");
            if (POAssembly.GetType("ProceduralObjects.SelectionMode.SelectionFilters") == null)
            {
                POHasFilters = false;
            }
            if (POAssembly.GetType("ProceduralObjects.Classes.POGroup") == null)
            {
                POHasGroups = false;
            }
            POLogic = FindObjectOfType(tPOLogic);

            if (POLogic is null)
            {
                throw new Exception("PO Logic not found!");
            }

            //Log.Debug($"POHasFilters:{POHasFilters}");
        }

        public List<PO_Object> AllObjects
        {
            get
            {
                List<PO_Object> objects = new List<PO_Object>();

                var objectList = tPOLogic.GetField("proceduralObjects", flags).GetValue(POLogic);
                int count = (int)objectList.GetType().GetProperty("Count").GetValue(objectList, null);

                HashSet<int> activeIds = new HashSet<int>();

                for (int i = 0; i < count; i++)
                {
                    var v = objectList.GetType().GetMethod("get_Item").Invoke(objectList, new object[] { i });
                    PO_Object o = new PO_Object(v);

                    if (activeIds.Contains(o.ProcId))
                    {
                        Log.Info($"PO Object #{o.Id} (PO:{o.ProcId}) has duplicate with index {i}.");
                    }
                    else
                    {
                        objects.Add(o);
                        activeIds.Add(o.ProcId);
                    }
                }

                //string msg = $"total:{count}, activeIds ({activeIds.Count}):\n";
                //foreach (int a in activeIds)
                //{
                //    msg += $"{a},";
                //}
                //Log.Debug(msg);

                return objects;
            }
        }

        public List<PO_Object> Objects
        {
            get
            {
                List<PO_Object> objects = new List<PO_Object>();

                foreach (PO_Object obj in AllObjects)
                {
                    if (POHasFilters)
                    {
                        var filters = tPOLogic.GetField("filters", flags).GetValue(POLogic);
                        if ((bool)filters.GetType().GetMethod("FiltersAllow", new Type[] { tPO }).Invoke(filters, new object[] { obj.procObj }))
                        {
                            objects.Add(obj);
                        }
                    }
                    else
                    {
                        objects.Add(obj);
                    }
                }

                return objects;
            }
        }

        public void Paste(InstanceID original, InstanceID target, int id)
        {
            InstanceID cloneID = target;
            cloneID.NetLane = (uint)id + 1;
            MoveItTool.PO.visibleObjects.Add(cloneID.NetLane, GetPOById((uint)id + 1));

            MoveableProc cloneInstance = new MoveableProc(cloneID){};

            Action.selection.Add(cloneInstance);
            if (ActionQueue.instance.current is CloneActionBase ca)
            {
                ca.m_clones.Add(cloneInstance);
                ca.m_origToClone.Add(new MoveableProc(original), cloneInstance);
            }
            else
            {
                Log.Debug($"Current action is {ActionQueue.instance.current.GetType()}, not CloneActionBase");
            }

            MoveItTool.SetToolState();
            MoveItTool.instance.ProcessSensitivityMode(false);
            Log.Info($"Cloned PO {original.NetLane} to #{cloneInstance.id.NetLane} (new method)");
        }

        //public uint Clone(ProcState original, Vector3 position, float angle)
        //{
        //    // Create an empty PO object that integration will manipulate

        //    //PrefabInfo prefab = PrefabCollection<BuildingInfo>.FindLoaded(original.prefabName);
        //    //if (prefab is null)
        //    //{
        //    //    prefab = PrefabCollection<PropInfo>.FindLoaded(original.prefabName);
        //    //}

        //    object raw = tPOLogic.Assembly.CreateInstance("ProceduralObjects.Classes.ProceduralObject");
        //    int id = (int)tPUtils.GetMethod("GetNextUnusedId").Invoke(null, new[] { tPOLogic.GetField("proceduralObjects").GetValue(POLogic) });
        //    tPO.GetField("id").SetValue(raw, id);
        //    object POlist = tPOLogic.GetField("proceduralObjects").GetValue(POLogic);
        //    POlist.GetType().GetMethod("Add", new Type[] { tPO }).Invoke(POlist, new[] { raw });
        //    return (uint)id + 1;
        //}

        public void Clone(MoveableProc original, Vector3 position, float angle, Action action)
        {
            MoveItTool.POProcessing++;
            tPOMoveIt.GetMethod("CallPOCloning", new Type[] { tPO }).Invoke(null, new[] { original.m_procObj.GetProceduralObject() });
            StartCoroutine(RetrieveClone(original, position, angle, action));
        }

        public IEnumerator<object> RetrieveClone(MoveableProc original, Vector3 position, float angle, Action action)
        {
            const uint MaxAttempts = 100_000;
            CloneActionBase ca = (CloneActionBase)action;
            Single halfWidth = 4f;

            if (!(original.m_procObj is PO_Object))
            {
                Log.Info($"PO Cloning failed: object not found");
                MoveItTool.POProcessing--;
                yield break;
            }

            Type[] types = new Type[] { tPO, tPO.MakeByRefType(), typeof(uint).MakeByRefType() };
            object originalObject = original.m_procObj.GetProceduralObject();
            object[] paramList = new[] { originalObject, null, null };
            if (tPO.GetField("halfOverlayDiam") != null) // Get the halfWidth, if it exists
            {
                halfWidth = Math.Max((float)tPO.GetField("halfOverlayDiam").GetValue(originalObject), 2f);
            }
            MethodInfo retrieve = tPOMoveIt.GetMethod("TryRetrieveClone", BindingFlags.Public | BindingFlags.Static, null, types, null);
            if (retrieve == null)
            {
                Log.Info($"PO Cloning failed: retrieve not found");
                MoveItTool.POProcessing--;
                yield break;
            }

            uint c = 0;
            while (c < MaxAttempts && !(bool)retrieve.Invoke(null, paramList))
            {
                //if (c % 100 == 0)
                //{
                //    BindingFlags f = BindingFlags.Static | BindingFlags.Public;
                //    object queueObj = tPOMoveIt.GetField("queuedCloning", f).GetValue(null);
                //    int queueCount = (int)queueObj.GetType().GetProperty("Count").GetValue(queueObj, null);
                //    object doneObj = tPOMoveIt.GetField("doneCloning", f).GetValue(null);
                //    int doneCount = (int)doneObj.GetType().GetProperty("Count").GetValue(doneObj, null);
                //}
                c++;
                yield return new WaitForSeconds(0.05f);
            }

            if (c == MaxAttempts)
            {
                throw new Exception($"Failed to clone object #{original.m_procObj.Id}! [PO-F4]");
            }

            try
            {
                PO_Object clone = new PO_Object(paramList[1])
                {
                    POColor = original.m_procObj.POColor
                };

                if (tPO.GetField("halfOverlayDiam") != null) // Set the halfWidth, if it exists
                {
                    tPO.GetField("halfOverlayDiam").SetValue(clone.GetProceduralObject(), halfWidth);
                }

                InstanceID cloneID = default;
                cloneID.NetLane = clone.Id;
                MoveItTool.PO.visibleObjects.Add(cloneID.NetLane, clone);

                MoveableProc cloneInstance = new MoveableProc(cloneID)
                {
                    position = position,
                    angle = angle
                };

                Action.selection.Add(cloneInstance);
                ca.m_clones.Add(cloneInstance);
                ca.m_origToClone.Add(original, cloneInstance);

                MoveItTool.SetToolState();
                MoveItTool.instance.ProcessSensitivityMode(false);
                Log.Info($"Cloned PO {original.m_procObj.Id} to #{clone.Id}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception when cloning PO:\n{e}");
            }

            yield return new WaitForSeconds(0.25f);
            MoveItTool.POProcessing--;
        }

        public void Delete(PO_Object obj)
        {
            var poList = tPOLogic.GetField("proceduralObjects", flags).GetValue(POLogic);
            var poSelList = tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic);

            poList.GetType().GetMethod("Remove", flags, null, new Type[] { tPO }, null).Invoke(poList, new object[] { obj.GetProceduralObject() });
            poSelList.GetType().GetMethod("Remove", flags, null, new Type[] { tPO }, null).Invoke(poSelList, new object[] { obj.GetProceduralObject() });
            if (tPOLogic.GetField("activeIds", flags) != null)
            {
                var activeIds = tPOLogic.GetField("activeIds", flags).GetValue(POLogic);
                activeIds.GetType().GetMethod("Remove", flags, null, new Type[] { typeof(int) }, null).Invoke(activeIds, new object[] { obj.ProcId });
            }
        }

        /// <param name="id">The NetLane id</param>
        internal PO_Object GetPOById(uint id)
        {
            foreach (PO_Object po in Objects)
            {
                if (po.Id == id)
                {
                    return po;
                }
            }
            return null;
        }

        /// <param name="id">The NetLane id</param>
        internal PO_Object GetPOByIdUnfiltered(uint id)
        {
            foreach (PO_Object po in AllObjects)
            {
                if (po.Id == id)
                {
                    return po;
                }
            }
            return null;
        }

        //public void InitGroups()
        //{
        //    Groups = new List<PO_Group>();

        //    if (!POHasGroups)
        //    {
        //        Log.Debug($"PO Groups feature not found!");
        //        return;
        //    }

        //    Log.Debug($"AAG01");
        //    object groupList = tPOLogic.GetField("groups", flags).GetValue(POLogic);
        //    if (groupList == null)
        //    {
        //        Log.Debug($"PO Groups is null!");
        //        return;
        //    }
        //    Log.Debug($"AAG02");
        //    int count = (int)groupList.GetType().GetProperty("Count").GetValue(groupList, null);
        //    Log.Debug($"AAG03");

        //    for (int i = 0; i < count; i++)
        //    {
        //        Log.Debug($"AAG04.1");
        //        var v = groupList.GetType().GetMethod("get_Item").Invoke(groupList, new object[] { i });
        //        Log.Debug($"AAG04.2");
        //        Groups.Add(new PO_Group(v));
        //        Log.Debug($"AAG04.3");
        //    }
        //    Log.Debug($"AAG05");
        //}

        private object AvailableProcInfos
        {
            get => tPOLogic.GetField("availableProceduralInfos").GetValue(POLogic);
        }

        private object GetPInfo(PrefabInfo prefab)
        {
            // Most code adapted from PO ProceduralObjectsLogic.ConvertToProcedural, by Simon Ryr

            object procInfo = null;
            int count, i;

            if (AvailableProcInfos == null)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));
            if ((int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null) == 0)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));

            string field = "buildingPrefab";
            if (prefab is PropInfo)
                field = "propPrefab";

            try
            {
                count = (int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null);
                for (i = 0; i < count; i++)
                {
                    var info = AvailableProcInfos.GetType().GetMethod("get_Item").Invoke(AvailableProcInfos, new object[] { i });
                    if (info == null) continue;

                    if ((PrefabInfo)info.GetType().GetField(field).GetValue(info) == prefab)
                    {
                        procInfo = info;
                        break;
                    }
                }
                
                if (procInfo is null)
                {
                    throw new NullReferenceException($"procInfo for {prefab.name} is null!");
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return procInfo;
        }

        public PO_Object ConvertToPO(Instance instance)
        {
            // Most code adapted from PO ProceduralObjectsLogic.ConvertToProcedural, by Simon Ryr

            try
            {
                object procInfo = GetPInfo(instance.Info.Prefab);

                if ((bool)procInfo.GetType().GetField("isBasicShape").GetValue(procInfo) &&
                    (int)tPOLogic.GetField("basicTextures").GetValue(POLogic).GetType().GetProperty("Count").GetValue(tPOLogic.GetField("basicTextures").GetValue(POLogic), null) > 0)
                {
                    tPOLogic.GetField("currentlyEditingObject").SetValue(POLogic, null);
                    tPOLogic.GetField("chosenProceduralInfo").SetValue(POLogic, procInfo);
                }
                else
                {
                    tPOLogic.GetMethod("SpawnObject", new Type[] { tPInfo, typeof(Texture2D) }).Invoke(POLogic, new[] { procInfo, null });
                    var v = tVertex.GetMethod("CreateVertexList", new Type[] { tPO }).Invoke(null, new[] { tPOLogic.GetField("currentlyEditingObject").GetValue(POLogic) });
                    tPOLogic.GetField("tempVerticesBuffer").SetValue(POLogic, v);
                }

                object poObj = tPOLogic.GetField("currentlyEditingObject").GetValue(POLogic);
                tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic).GetType().GetMethod("Add", new Type[] { tPO }).Invoke(tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic), new[] { poObj });
                if (tPO.GetField("halfOverlayDiam") != null) // Set the overlay half-width, if available
                {
                    Mesh mesh = (Mesh)tPO.GetField("m_mesh").GetValue(poObj);
                    tPO.GetField("halfOverlayDiam").SetValue(poObj, Math.Max(Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z), 2f));
                }
                return new PO_Object(poObj);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static string getVersion()
        {
            try
            {
                Assembly poAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Length >= 17 && assembly.FullName.Substring(0, 17) == "ProceduralObjects")
                    {
                        poAssembly = assembly;
                        break;
                    }
                }
                if (poAssembly == null)
                {
                    return "(Failed [PO-F1])";
                }

                Type tPO = poAssembly.GetType("ProceduralObjects.ProceduralObjectsMod");
                object version = tPO.GetField("VERSION", BindingFlags.Public | BindingFlags.Static).GetValue(null);

                return version.ToString();
            }
            catch (Exception e)
            {
                Log.Error($"PO INTERATION FAILED\n" + e);
            }

            return "(Failed [PO-F2])";
        }
    }


    public class Info_POEnabled : IInfo
    {
        private PrefabInfo _Prefab = null;

        public Info_POEnabled(PrefabInfo info)
        {
            _Prefab = info;
        }

        public string Name => _Prefab is null ? "NULL" : _Prefab.name;

        public PrefabInfo Prefab
        {
            get => _Prefab;
            set => _Prefab = value;
        }
    }
}
