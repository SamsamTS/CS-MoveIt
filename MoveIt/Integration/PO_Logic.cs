using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoveIt
{
    internal class PO_Logic : MonoBehaviour
    {
        internal static Assembly POAssembly = null;
        protected Type _tPOLogic = null;
        internal Type tPOLogic = null, tPOMod = null, tPO = null, tPInfo = null, tPUtils = null, tVertex = null, tPOMoveIt = null;
        internal object POLogic = null;

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
            POLogic = FindObjectOfType(tPOLogic);
        }

        public List<PO_Object> Objects
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
                        Debug.Log($"PO Object #{o.Id} (PO:{o.ProcId}) has duplicate.");
                    }
                    else
                    {
                        objects.Add(o);
                        activeIds.Add(o.ProcId);
                    }
                }

                //string msg = $"activeIds ({activeIds.Count}):\n";
                //foreach (int a in activeIds)
                //{
                //    msg += $"{a},";
                //}
                //Debug.Log(msg);

                return objects;
            }
        }

        public void Clone(MoveableProc original, Vector3 position, float angle, Action action)
        {
            MoveItTool.POProcessing++;
            tPOMoveIt.GetMethod("CallPOCloning", new Type[] { tPO }).Invoke(null, new[] { original.m_procObj.GetProceduralObject() });
            StartCoroutine(RetrieveClone(original, position, angle, action));
        }

        public IEnumerator<object> RetrieveClone(MoveableProc original, Vector3 position, float angle, Action action)
        {
            const uint MaxAttempts = 1000_000;
            CloneAction ca = (CloneAction)action;

            Type[] types = new Type[] { tPO, tPO.MakeByRefType(), typeof(uint).MakeByRefType() };
            object[] paramList = new[] { original.m_procObj.GetProceduralObject(), null, null };
            MethodInfo retrieve = tPOMoveIt.GetMethod("TryRetrieveClone", BindingFlags.Public | BindingFlags.Static, null, types, null );

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

            PO_Object clone = new PO_Object(paramList[1])
            {
                POColor = original.m_procObj.POColor
            };

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

            yield return new WaitForSeconds(0.25f);
            Debug.Log($"Cloned PO {original.m_procObj.Id} to #{clone.Id}");
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
                    return po;
            }

            return null;
        }

        private object AvailableProcInfos
        {
            get => tPOLogic.GetField("availableProceduralInfos").GetValue(POLogic);
        }

        public PO_Object ConvertToPO(Instance instance)
        {
            // Most code adapted from PO ProceduralObjectsLogic.ConvertToProcedural, by Simon Ryr

            if (AvailableProcInfos == null)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));
            if ((int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null) == 0)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));

            try
            {
                object procInfo = null;
                int count, i;

                if (instance is MoveableProp mp)
                {
                    count = (int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null);
                    for (i = 0; i < count; i++)
                    {
                        var info = AvailableProcInfos.GetType().GetMethod("get_Item").Invoke(AvailableProcInfos, new object[] { i });
                        if (info == null) continue;

                        if ((PropInfo)info.GetType().GetField("propPrefab").GetValue(info) == (PropInfo)instance.Info.Prefab)
                        {
                            procInfo = info;
                            break;
                        }
                    }

                    if (procInfo == null) throw new NullReferenceException("procInfo is null when converting to PO");

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
                }
                else if (instance is MoveableBuilding mb)
                {
                    count = (int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null);
                    for (i = 0; i < count; i++)
                    {
                        var info = AvailableProcInfos.GetType().GetMethod("get_Item").Invoke(AvailableProcInfos, new object[] { i });
                        if (info == null) continue;

                        if ((BuildingInfo)info.GetType().GetField("buildingPrefab").GetValue(info) == (BuildingInfo)instance.Info.Prefab)
                        {
                            procInfo = info;
                            break;
                        }
                    }

                    if (procInfo == null) throw new NullReferenceException("procInfo is null when converting to PO");

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
                }

                object poObj = tPOLogic.GetField("currentlyEditingObject").GetValue(POLogic);
                tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic).GetType().GetMethod("Add", new Type[] { tPO }).Invoke(tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic), new[] { poObj });
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
                Debug.Log($"PO INTERATION FAILED\n" + e);
            }

            return "(Failed [PO-F2])";
        }
    }


    public class Info_POEnabled : IInfo
    {
        private PO_Object _obj = null;
        private PrefabInfo _Prefab = null;

        public Info_POEnabled(object i)
        {
            _obj = (PO_Object)i;
        }

        public string Name => _obj.Name;

        public PrefabInfo Prefab
        {
            get
            {
                _Prefab = (PrefabInfo)_obj.tPO.GetField("_baseBuilding").GetValue(_obj.procObj);
                if (_Prefab == null)
                {
                    _Prefab = (PrefabInfo)_obj.tPO.GetField("_baseProp").GetValue(_obj.procObj);
                }
                return _Prefab;
            }
            set
            {
                _Prefab = value;
            }
        }
    }
}
