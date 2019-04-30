using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// High level PO wrapper, always available

namespace MoveIt
{
    internal class PO_Manager
    {
        private IPO_Logic Logic;

        private HashSet<uint> visibleIds = new HashSet<uint>();
        private HashSet<uint> selectedIds = new HashSet<uint>();
        internal Dictionary<uint, IPO_Object> visibleObjects = new Dictionary<uint, IPO_Object>();

        internal List<IPO_Object> Objects => new List<IPO_Object>(visibleObjects.Values);
        internal IPO_Object GetProcObj(uint id) => visibleObjects[id];

        internal const string VersionName = "1.6-beta 3";

        internal bool Enabled = false;
        private bool _active = false;
        public bool Active
        {
            get
            {
                if (!Enabled)
                    return false;
                return _active;
            }
            set
            {
                if (!Enabled)
                    _active = false;
                _active = value;
            }
        }

        internal PO_Manager()
        {
            try
            {
                InitialiseLogic();
            }
            catch (TypeLoadException)
            {
                Enabled = false;
                Logic = new PO_LogicDisabled();
            }
        }

        private void InitialiseLogic()
        {
            if (isPOEnabled())
            {
                Enabled = true;
                Logic = new PO_LogicEnabled();
            }
            else
            {
                Enabled = false;
                Logic = new PO_LogicDisabled();
            }
        }

        internal uint Clone(uint originalId, Vector3 position)
        {
            return Logic.Clone(originalId, position);
        }

        internal bool ToolEnabled()
        {
            Dictionary<uint, IPO_Object> newVisible = new Dictionary<uint, IPO_Object>();
            HashSet<uint> newIds = new HashSet<uint>();

            foreach (IPO_Object obj in Logic.Objects)
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
            MoveItTool.m_debugPanel.Update();

            //Debug.Log($"Visible from:{visibleObjects.Count} to:{newVisible.Count}\nSelected from:{selectedIds.Count} to:{newSelectedIds.Count}");

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

        internal void Delete(IPO_Object obj)
        {
            Logic.Delete(obj);
        }

        internal IPO_Object ConvertToPO(Instance instance)
        {
            return Logic.ConvertToPO(instance);
        }

        internal static bool isPOEnabled()
        {
            if (!isPOInstalled())
            {
                return false;
            }
            if (getVersion() != VersionName)
            {
                return false;
            }
            return true;
        }

        internal static bool isPOInstalled()
        {
            //string msg = "";
            //foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            //{
            //    msg += $"\n{pi.publishedFileID.AsUInt64} - {pi.name} ({pi.isEnabled})" +
            //        $"\n - {pi.modPath}";
            //}
            //Debug.Log(msg);

            if (!PluginManager.instance.GetPluginsInfo().Any(mod => (
                    mod.publishedFileID.AsUInt64 == 1094334744uL || 
                    mod.name.Contains("ProceduralObjects") || 
                    mod.name.Contains("Procedural Objects") ||
                    mod.name.Contains("1094334744")
            ) && mod.isEnabled))
            {
                return false;
            }
            return true;
        }

        internal static string getVersionText()
        {
            if (isPOInstalled())
            {
                if (PO_LogicEnabled.getVersion() == VersionName)
                {
                    return $"PO version {PO_LogicEnabled.getVersion()} found, integration enabled!\n ";
                }
                else
                {
                    return $"PO integration failed - found version {PO_LogicEnabled.getVersion()} (required: {VersionName})\n ";
                }
            }

            return "PO is not available. To use these options please subscribe to PO and enable it, then \nrestart Cities Skylines.\n ";
        }

        internal static string getVersion()
        {
            try
            {
                return _getVersion();
            }
            catch (TypeLoadException)
            {
                return "";
            }
        }

        private static string _getVersion()
        {
            return PO_LogicEnabled.getVersion();
        }
    }


    // PO Logic
    internal interface IPO_Logic
    {
        List<IPO_Object> Objects { get; }
        uint Clone(uint originalId, Vector3 position);
        IPO_Object ConvertToPO(Instance instance);
        void Delete(IPO_Object obj);
    }

    internal class PO_LogicDisabled : IPO_Logic
    {
        public List<IPO_Object> Objects
        {
            get
            {
                return new List<IPO_Object>();
            }
        }

        public uint Clone(uint originalId, Vector3 position)
        {
            throw new NotImplementedException($"Trying to clone {originalId} despite no PO!");
        }

        public IPO_Object ConvertToPO(Instance instance)
        {
            throw new NotImplementedException($"Trying to convert {instance} despite no PO!");
        }

        public void Delete(IPO_Object obj)
        {
            throw new NotImplementedException($"Trying to delete {obj} despite no PO!");
        }
    }


    // PO Object
    internal interface IPO_Object
    {
        bool Selected { get; set; }
        uint Id { get; set; } // The InstanceID.NetLane value
        Vector3 Position { get; set; }
        float Angle { get; set; }
        IInfo Info { get; set; }
        object GetProceduralObject();
        void SetPositionY(float h);
        float GetDistance(Vector3 location);
        void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color);
        string DebugQuaternion();
    }

    internal class PO_ObjectDisabled : IPO_Object
    {
        public uint Id { get; set; } // The InstanceID.NetLane value

        public Vector3 Position
        {
            get => Vector3.zero;
            set { }
        }

        public float Angle
        {
            get => 0f;
            set { }
        }

        public bool Selected
        {
            get => false;
            set { }
        }

        private Info_PODisabled _info = new Info_PODisabled();
        public IInfo Info
        {
            get => _info;
            set => _info = (Info_PODisabled)value;
        }

        public void SetPositionY(float h)
        { }

        public object GetProceduralObject()
        {
            return null;
        }

        public float GetDistance(Vector3 location) => 0f;

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color)
        { }


        public string DebugQuaternion()
        {
            return "";
        }
    }

    public class Info_PODisabled : IInfo
    {
        public string Name => "";
        public PrefabInfo Prefab { get; set; } = null;
    }
}
