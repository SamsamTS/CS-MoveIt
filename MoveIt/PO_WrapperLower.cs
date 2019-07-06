using ColossalFramework.UI;
using ProceduralObjects;
using ProceduralObjects.Classes;
using ColossalFramework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Low level PO wrapper, only accessed by high level

namespace MoveIt
{
    internal class PO_LogicEnabled : IPO_Logic
    {
        //private ProceduralObjectsLogic logic = null;
        //public ProceduralObjectsLogic Logic
        //{
        //    get
        //    {
        //        if (logic == null)
        //            logic = UnityEngine.Object.FindObjectOfType<ProceduralObjectsLogic>();
        //        return logic;
        //    }
        //}

        internal static Assembly POAssembly = null;
        internal Type tPOLogic = null, tPOMod = null, tPO = null, tPInfo = null, tPUtils = null, tVertex = null;
        internal object POLogic = null;

        private readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        internal PO_LogicEnabled()
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
            //POLogic = (object)tPOMod.GetField("gameLogicObject", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            POLogic = UnityEngine.Object.FindObjectOfType(tPOLogic);
            //Debug.Log($"POLogic:{(POLogic == null ? "null" : $"{POLogic} <{POLogic.GetType()}>")}");
            //Debug.Log($"tPO:{(tPO == null ? "null" : $"{tPO} <{tPO.GetType()}>")}");
        }

        protected Type _tPOLogic = null;

        public List<IPO_Object> Objects
        {
            get
            {
                List<IPO_Object> objects = new List<IPO_Object>();

                var objectList = tPOLogic.GetField("proceduralObjects", flags).GetValue(POLogic);
                if (MoveItTool.POOnlySelectedAreVisible)
                {
                    objectList = tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic);
                }

                int count = (int)objectList.GetType().GetProperty("Count").GetValue(objectList, null);
                for (int i = 0; i < count; i++)
                {
                    var v = objectList.GetType().GetMethod("get_Item").Invoke(objectList, new object[] { i });
                    IPO_Object o = new PO_ObjectEnabled(v);
                    objects.Add(o);
                }

                return objects;

                //List<ProceduralObject> objectList = Logic.proceduralObjects;
                //if (MoveItTool.POOnlySelectedAreVisible)
                //{
                //    objectList = Logic.pObjSelection;
                //}
                //List<IPO_Object> objects = new List<IPO_Object>();
                //foreach (ProceduralObject obj in objectList)
                //{
                //    IPO_Object o = new PO_ObjectEnabled(obj);
                //    objects.Add(o);
                //}
                //return objects;
            }
        }

        public uint Clone(uint originalId, Vector3 position)
        {
            //int id = (int)originalId - 1;
            //var cache = new CacheProceduralObject(Logic.proceduralObjects[id]);

            //int newId = Logic.proceduralObjects.GetNextUnusedId();
            //Debug.Log($"Cloning {originalId - 1} to {newId}(?), {position}\n{cache.baseInfoType}: {cache}");

            ////PropInfo propInfo = Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault((PropInfo info) => info.name == cache.basePrefabName);
            ////Debug.Log($"{propInfo.m_material.color}, {propInfo.m_material.mainTexture}, {propInfo.m_material.shader}");

            //var obj = Logic.PlaceCacheObject(cache, false);
            ////ProceduralObject obj = new ProceduralObject(cache, newId, position);
            ////Logic.proceduralObjects.Add(obj);

            //return (uint)obj.id + 1;
            return 0;
        }

        public void Delete(IPO_Object obj)
        {
            var poList = tPOLogic.GetField("proceduralObjects", flags).GetValue(POLogic);
            var poSelList = tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic);

            poList.GetType().GetMethod("Remove", flags, null, new Type[] { tPO }, null).Invoke(poList, new object[] { obj.GetProceduralObject() });
            poSelList.GetType().GetMethod("Remove", flags, null, new Type[] { tPO }, null).Invoke(poSelList, new object[] { obj.GetProceduralObject() });

            //Logic.proceduralObjects.Remove((ProceduralObject)obj.GetProceduralObject());
            //Logic.pObjSelection.Remove((ProceduralObject)obj.GetProceduralObject());
        }

        private object AvailableProcInfos
        {
            get => tPOLogic.GetField("availableProceduralInfos").GetValue(POLogic);
        }

        public IPO_Object ConvertToPO(Instance instance)
        {

            if (AvailableProcInfos == null)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));
            if ((int)AvailableProcInfos.GetType().GetProperty("Count").GetValue(AvailableProcInfos, null) == 0)
                tPOLogic.GetField("availableProceduralInfos").SetValue(POLogic, tPUtils.GetMethod("CreateProceduralInfosList").Invoke(null, null));

            // Most code lifted from PO

            //if (Logic.availableProceduralInfos == null)
            //    Logic.availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            //if (Logic.availableProceduralInfos.Count == 0)
            //    Logic.availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();

            //Debug.Log($"ConvertToPO:\n{instance.Info.Prefab.name} <{instance.Info.Prefab.GetType()}>");

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
                        Debug.Log("FFF");
                        tPOLogic.GetField("currentlyEditingObject").SetValue(POLogic, null);
                        tPOLogic.GetField("chosenProceduralInfo").SetValue(POLogic, procInfo);
                    }
                    else
                    {
                        //string msg = $"{tPInfo}\n";
                        //foreach (MethodInfo mi in tPOLogic.GetMethods(flags))
                        //{
                        //    msg += $"{mi} - {mi.Name} <{mi.ReturnType}>\n";
                        //}
                        //Debug.Log(msg);

                        tPOLogic.GetMethod("SpawnObject", new Type[] { tPInfo, typeof(Texture2D) }).Invoke(POLogic, new[] { procInfo, null });
                        var v = tVertex.GetMethod("CreateVertexList", new Type[] { tPO }).Invoke(null, new[] { tPOLogic.GetField("currentlyEditingObject").GetValue(POLogic) });
                        tPOLogic.GetField("tempVerticesBuffer").SetValue(POLogic, v);
                    }

                    //ProceduralInfo info = Logic.availableProceduralInfos.Where(pInf => pInf.propPrefab != null).FirstOrDefault(pInf => pInf.propPrefab == (PropInfo)instance.Info.Prefab);
                    //if (info.isBasicShape && Logic.basicTextures.Count > 0)
                    //{
                    //    Logic.currentlyEditingObject = null;
                    //    Logic.chosenProceduralInfo = info;
                    //}
                    //else
                    //{
                    //    Logic.SpawnObject(info);
                    //    // PO 1.5: Logic.temp_storageVertex = Vertex.CreateVertexList(Logic.currentlyEditingObject);
                    //    Logic.tempVerticesBuffer = Vertex.CreateVertexList(Logic.currentlyEditingObject);
                    //}

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


                    //ProceduralInfo info = Logic.availableProceduralInfos.Where(pInf => pInf.buildingPrefab != null).FirstOrDefault(pInf => pInf.buildingPrefab == (BuildingInfo)instance.Info.Prefab);
                    //if (info.isBasicShape && Logic.basicTextures.Count > 0)
                    //{
                    //    Logic.chosenProceduralInfo = info;
                    //}
                    //else
                    //{
                    //    Logic.SpawnObject(info);
                    //    Logic.tempVerticesBuffer = Vertex.CreateVertexList(Logic.currentlyEditingObject);
                    //}
                }

                object poObj = tPOLogic.GetField("currentlyEditingObject").GetValue(POLogic);
                tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic).GetType().GetMethod("Add", new Type[] { tPO }).Invoke(tPOLogic.GetField("pObjSelection", flags).GetValue(POLogic), new[] { poObj });
                return new PO_ObjectEnabled(poObj);

                //ProceduralObject poObj = Logic.currentlyEditingObject;
                //Logic.pObjSelection.Add(poObj);
                //return new PO_ObjectEnabled(poObj);
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


    internal class PO_ObjectEnabled : IPO_Object
    {
        private object procObj;

        public uint Id { get; set; } // The InstanceID.NetLane value
        public bool Selected { get; set; }
        private int ProcId { get => (int)Id - 1; set => Id = (uint)value + 1; }

        internal Type tPOLogic = null, tPOMod = null, tPO = null;

        public Vector3 Position
        {
            get => (Vector3)tPO.GetField("m_position").GetValue(procObj);
            set => tPO.GetField("m_position").SetValue(procObj, value);

        }
        private Quaternion Rotation
        {
            get => (Quaternion)tPO.GetField("m_rotation").GetValue(procObj);
            set => tPO.GetField("m_rotation").SetValue(procObj, value);
        }

        public string Name
        {
            get
            {
                string name = (string)tPO.GetField("basePrefabName").GetValue(procObj);
                if (name.Length < 35)
                    return "[PO]" + name;
                return "[PO]" + name.Substring(0, 35);
            }
            set { }
        }

        public float Angle
        {
            get
            {
                float a = -Rotation.eulerAngles.y % 360f;
                if (a < 0) a += 360f;
                return a * Mathf.Deg2Rad;
            }

            set
            {
                float a = -(value * Mathf.Rad2Deg) % 360f;
                if (a < 0) a += 360f;
                //tPO.GetField("m_rotation").GetValue(procObj).GetType().GetProperty("eulerAngles").SetValue(tPO.GetField("m_rotation").GetValue(procObj), new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z), null);

                Quaternion q = Rotation;
                q.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
                Rotation = q;

                //procObj.m_rotation.eulerAngles = new Vector3(Rotation.eulerAngles.x, a, Rotation.eulerAngles.z);
            }
        }

        private Info_POEnabled _info = null;
        public IInfo Info
        {
            get
            {
                if (_info == null)
                    _info = new Info_POEnabled(this);

                return _info;
            }
            set => _info = (Info_POEnabled)value;
        }

        public object GetProceduralObject()
        {
            return procObj;
        }

        public PO_ObjectEnabled(object obj)
        {
            tPOLogic = PO_LogicEnabled.POAssembly.GetType("ProceduralObjects.ProceduralObjectsLogic");
            tPOMod = PO_LogicEnabled.POAssembly.GetType("ProceduralObjects.ProceduralObjectsMod");
            tPO = PO_LogicEnabled.POAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");

            procObj = obj;
            ProcId = (int)tPO.GetField("id").GetValue(procObj);
        }

        public void SetPositionY(float h)
        {
            Position = new Vector3(Position.x, h, Position.z);
        }

        public float GetDistance(Vector3 location)
        {
            return Vector3.Distance(Position, location);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        {
            float size = 4f;
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, Position, size, Position.y - 100f, Position.y + 100f, renderLimits: false, alphaBlend: true);
        }

        public string DebugQuaternion()
        {
            return $"{Id}:{Rotation.w},{Rotation.x},{Rotation.y},{Rotation.z}";
        }
    }


    public class Info_POEnabled : IInfo
    {
        private PO_ObjectEnabled _obj = null;

        public Info_POEnabled(object i)
        {
            _obj = (PO_ObjectEnabled)i;
        }

        public string Name => _obj.Name;

        public PrefabInfo Prefab { get; set; } = null;
    }
}
