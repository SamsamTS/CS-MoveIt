using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// High level PO wrapper, always available

namespace MoveIt
{
    internal class PO_Logic
    {
        internal static IPO_Wrapper Lower;

        private HashSet<uint> visibleIds = new HashSet<uint>();
        private HashSet<uint> selectedIds = new HashSet<uint>();
        private Dictionary<uint, PO_ObjectBase> visibleObjects = new Dictionary<uint, PO_ObjectBase>();

        internal List<PO_ObjectBase> Objects => new List<PO_ObjectBase>(visibleObjects.Values);
        internal PO_ObjectBase GetProcObj(uint id) => visibleObjects[id];

        //public Type tPOLogic, tPOObject;

        internal PO_Logic()
        {
            PO_LayerChooser.LowerLayer();
            //try
            //{
            //    InitialiseLogic();
            //}
            //catch (TypeLoadException ex)
            //{
            //    ModInfo.DebugLine(ex.ToString());
            //    Lower = new PO_WrapperDisabled();
            //}
        }


        //private void InitialiseLogic()
        //{ 
        //    bool x = false;
        //    if (x)
        //    {
        //        Debug.Log($"ENABLED!");
        //        Lower = new PO_WrapperEnabled();
        //    }
        //    else
        //    {
        //        Debug.Log($"DISABLED!");
        //        Lower = new PO_WrapperDisabled();
        //    }
        //}

        internal bool ToolEnabled()
        {
            Dictionary<uint, PO_ObjectBase> newVisible = new Dictionary<uint, PO_ObjectBase>();
            HashSet<uint> newIds = new HashSet<uint>();

            Debug.Log($"A");
            foreach (PO_ObjectBase obj in Lower.Objects)
            {
                newVisible.Add(obj.Id, obj);
                newIds.Add(obj.Id);
            }

            Debug.Log($"B");
            HashSet<uint> removed = new HashSet<uint>(visibleIds);
            removed.ExceptWith(newIds);
            HashSet<uint> added = new HashSet<uint>(newIds);
            added.ExceptWith(visibleIds);
            HashSet<uint> newSelectedIds = new HashSet<uint>(selectedIds);
            newSelectedIds.IntersectWith(newIds);

            Debug.Log($"C");
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
            Debug.Log($"D");
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

        internal void SelectionAdd(HashSet<Instance> instances)
        {
            foreach (Instance i in instances)
            {
                SelectionAdd(i);
            }
        }

        internal void SelectionAdd(Instance instance)
        {
            if (instance.id.NetLane <= 0) return;

            selectedIds.Add(instance.id.NetLane);
        }

        internal void SelectionRemove(HashSet<Instance> instances)
        {
            foreach (Instance i in instances)
            {
                SelectionRemove(i);
            }
        }

        internal void SelectionRemove(Instance instance)
        {
            if (instance.id.NetLane <= 0) return;

            selectedIds.Remove(instance.id.NetLane);
        }

        internal void SelectionClear()
        {
            selectedIds.Clear();
        }
    }


    internal static class PO_LayerChooser
    {

        internal static void LowerLayer()
        {
            if (isPOEnabled())
            {
                Debug.Log($"ENABLED!");
                PO_Logic.Lower = new PO_WrapperEnabled();
            }
            else
            {
                Debug.Log($"DISABLED!");
                PO_Logic.Lower = new PO_WrapperDisabled();
            }
        }


        internal static bool isPOEnabled()
        {
            Debug.Log(PluginManager.instance.GetPluginsInfo().Any(mod => (mod.publishedFileID.AsUInt64 == 1094334744uL || mod.name.Contains("ProceduralObjects") || mod.name.Contains("Procedural Objects")) && mod.isEnabled).ToString());

            string msg = "\n";
            foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            {
                msg += $"{pi.name} #{pi.publishedFileID}\n";
            }
            ModInfo.DebugLine(msg);

            ModInfo.DebugLine(PluginManager.instance.GetPluginsInfo().Any(mod => (mod.publishedFileID.AsUInt64 == 1094334744uL || mod.name.Contains("ProceduralObjects") || mod.name.Contains("Procedural Objects")) && mod.isEnabled).ToString());

            return PluginManager.instance.GetPluginsInfo().Any(mod => (mod.publishedFileID.AsUInt64 == 1094334744uL || mod.name.Contains("ProceduralObjects") || mod.name.Contains("Procedural Objects")) && mod.isEnabled);
        }
    }

    // PO Logic
    internal interface IPO_Wrapper
    {
        List<PO_ObjectBase> Objects { get; }
    }

    //internal abstract class PO_WrapperBase
    //{
    //    public virtual List<PO_ObjectBase> Objects => new List<PO_ObjectBase>();
    //}

    internal class PO_WrapperDisabled : IPO_Wrapper
    {
        public List<PO_ObjectBase> Objects
        {
            get
            {
                Debug.Log($"PO List: Inactive");
                return new List<PO_ObjectBase>();
            }
        }
    }


    // PO Object
    internal abstract class PO_ObjectBase
    {
        public uint Id { get; set; } // The InstanceID.NetLane value
        public abstract Vector3 Position { get; set; }
        public abstract float Angle { get; set; }
        public abstract void SetPositionY(float h);
        public abstract float GetDistance(Vector3 location);
        public abstract void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color);
        public abstract string DebugQuaternion();
    }

    internal class PO_ObjectInactive : PO_ObjectBase
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