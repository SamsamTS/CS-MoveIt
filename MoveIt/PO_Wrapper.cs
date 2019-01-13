using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// High level PO wrapper, always available

namespace MoveIt
{
    public class PO_Logic
    {
        private PO_WrapperBase Lower;

        private HashSet<uint> visibleIds = new HashSet<uint>();
        private HashSet<uint> selectedIds = new HashSet<uint>();
        private Dictionary<uint, PO_ObjectBase> visibleObjects = new Dictionary<uint, PO_ObjectBase>();

        public List<PO_ObjectBase> Objects => new List<PO_ObjectBase>(visibleObjects.Values);
        public PO_ObjectBase GetProcObj(uint id) => visibleObjects[id];

        //public Type tPOLogic, tPOObject;

        public PO_Logic()
        {
            if (isPOEnabled())
            {
                Lower = new PO.PO_WrapperEnabled();
            }
            else
            {
                Lower = new PO_WrapperDisabled();
            }
        }

        public bool ToolEnabled()
        {
            Dictionary<uint, PO_ObjectBase> newVisible = new Dictionary<uint, PO_ObjectBase>();
            HashSet<uint> newIds = new HashSet<uint>();

            foreach (PO_ObjectBase obj in Lower.Objects)
            {
                newVisible.Add(obj.Id, obj);
                newIds.Add(obj.Id);
            }

            HashSet<uint> removed = new HashSet<uint>(visibleIds);
            removed.ExceptWith(newIds);
            HashSet<uint> added = new HashSet<uint>(newIds);
            added.ExceptWith(visibleIds);
            HashSet<uint> newSelectedIds = new HashSet<uint>(selectedIds);
            newSelectedIds.IntersectWith(newIds);

            List<Instance> toRemove = new List<Instance>();
            foreach (Instance instance in Action.selection)
            {
                Debug.Log(instance);
                uint id = instance.id.NetLane;
                if (id > 0)
                {
                    if (removed.Contains(id))
                    {
                        toRemove.Add(instance);
                    }
                }
            }
            foreach (Instance instance in toRemove)
            {
                Action.selection.Remove(instance);
            }

            Debug.Log($"Visible from:{visibleObjects.Count} to:{newVisible.Count}\n" +
                $"Selected from:{selectedIds.Count} to:{newSelectedIds.Count}");

            visibleObjects = newVisible;
            visibleIds = newIds;
            selectedIds = newSelectedIds;

            // Has anything changed?
            if (added.Count > 0 || removed.Count > 0)
                return true;

            return false;
        }

        public void SelectionAdd(HashSet<Instance> instances)
        {
            foreach (Instance i in instances)
            {
                SelectionAdd(i);
            }
        }

        public void SelectionAdd(Instance instance)
        {
            if (instance.id.NetLane <= 0) return;

            selectedIds.Add(instance.id.NetLane);
        }

        public void SelectionRemove(HashSet<Instance> instances)
        {
            foreach (Instance i in instances)
            {
                SelectionRemove(i);
            }
        }

        public void SelectionRemove(Instance instance)
        {
            if (instance.id.NetLane <= 0) return;

            selectedIds.Remove(instance.id.NetLane);
        }

        public void SelectionClear()
        {
            selectedIds.Clear();
        }


        public static bool isPOEnabled()
        {
            //string msg = "\n";
            //foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            //{
            //    msg += $"{pi.name} #{pi.publishedFileID}\n";
            //}
            //ModInfo.DebugLine(msg);

            return PluginManager.instance.GetPluginsInfo().Any(mod => (mod.publishedFileID.AsUInt64 == 1094334744uL || mod.name.Contains("ProceduralObjects") || mod.name.Contains("Procedural Objects")) && mod.isEnabled);
        }


        //private bool Initialise()
        //{
        //    try
        //    {
        //        Assembly poAssembly = null;
        //        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        //        {
        //            ModInfo.DebugLine(assembly.FullName);
        //            if (assembly.FullName.Length >= 12 && assembly.FullName.Substring(0, 12) == "ProceduralOb")
        //            {
        //                poAssembly = assembly;
        //                break;
        //            }
        //        }
        //        ModInfo.DebugLine(poAssembly.ToString());
        //        if (poAssembly == null)
        //        {
        //            ModInfo.DebugLine("Assemble is NULL");
        //            return false;
        //        }

        //        tPOLogic = poAssembly.GetType("ProceduralObjects.ProceduralObjectsLogic");
        //        tPOObject = poAssembly.GetType("ProceduralObjects.Classes.ProceduralObject");

        //        ModInfo.DebugLine(tPOLogic.ToString());
        //        ModInfo.DebugLine(tPOObject.ToString());
        //    }
        //    catch (ReflectionTypeLoadException)
        //    {
        //        ModInfo.DebugLine($"MoveIt failed to integrate PO (ReflectionTypeLoadException)");
        //        return false;
        //    }
        //    catch (NullReferenceException)
        //    {
        //        ModInfo.DebugLine($"MoveIt failed to integrate PO (NullReferenceException)");
        //        return false;
        //    }

        //    return true;
        //}
    }

    //public class PO_ProcInfo : PrefabInfo
    //{
    //    new public string name;
    //}

    //static class PO_Utils
    //{
    //    public static ProceduralObjects.Classes.ProceduralObject GetProcWithId(this List<ProceduralObjects.Classes.ProceduralObject> list, int id)
    //    {
    //        if (list.Any(po => po.id == id))
    //        {
    //            return list.FirstOrDefault(po => po.id == id);
    //        }
    //        return null;
    //    }
    //}

    // PO Logic
    public abstract class PO_WrapperBase
    {
        public virtual List<PO_ObjectBase> Objects => new List<PO_ObjectBase>();
    }

    public class PO_WrapperDisabled : PO_WrapperBase
    {
        public override List<PO_ObjectBase> Objects
        {
            get
            {
                Debug.Log($"Inactive");
                return new List<PO_ObjectBase>();
            }
        }
    }


    // PO Object
    public abstract class PO_ObjectBase
    {
        public uint Id { get; set; } // The InstanceID.NetLane value
        public abstract Vector3 Position { get; set; }
        public abstract float Angle { get; set; }
        public abstract void SetPositionY(float h);
        public abstract float GetDistance(Vector3 location);
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color);
        public abstract string DebugQuaternion();
    }

    public class PO_ObjectInactive : PO_ObjectBase
    {
        public override Vector3 Position
        {
            get => Vector3.zero;
            set { }
        }

        public override float Angle
        {
            get => 0f;
            set { }
        }

        public override void SetPositionY(float h)
        {
            return;
        }

        public override float GetDistance(Vector3 location) => 0f;

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        { }

        public override string DebugQuaternion()
        {
            return "";
        }
    }
}

