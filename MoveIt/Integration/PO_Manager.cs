using ColossalFramework.Plugins;
using MoveIt.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveIt
{
    internal class PO_Manager
    {
        private PO_Logic Logic;
        private static GameObject gameObject;

        private HashSet<uint> visibleIds = new HashSet<uint>();
        internal Dictionary<uint, PO_Object> visibleObjects = new Dictionary<uint, PO_Object>();

        internal List<PO_Object> Objects => new List<PO_Object>(visibleObjects.Values);
        internal PO_Object GetProcObj(uint id) => visibleObjects[id];

        internal static readonly string[] VersionNames = { "1.6" };

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
            }
        }

        private void InitialiseLogic()
        {
            if (isModEnabled())
            {
                Enabled = true;

                gameObject = new GameObject("MIT_POLogic");
                gameObject.AddComponent<PO_Logic>();
                Logic = gameObject.GetComponent<PO_Logic>();
            }
            else
            {
                Enabled = false;
            }
        }

        internal void Clone(MoveableProc original, Vector3 position, float angle, Action action)
        {
            if (!Enabled) return;

            Logic.Clone(original, position, angle, action);
        }

        internal void StartConvertAction()
        {
            if (InitialiseTool(true))
            {
                ConvertToPOAction convertAction = new ConvertToPOAction();
                ActionQueue.instance.Push(convertAction);
                ActionQueue.instance.Do();
            }
        }

        internal void InitialiseTool()
        {
            InitialiseTool(!MoveItTool.PO.Active);
        }

        internal bool InitialiseTool(bool enable)
        {
            if (MoveItTool.PO.Active == enable)
            {
                return true;
            }

            try
            {
                MoveItTool.PO.Active = enable;
                if (MoveItTool.PO.Active)
                {
                    if (MoveItTool.ToolState == MoveItTool.ToolStates.Cloning)
                    {
                        MoveItTool.instance.StopCloning();
                    }

                    MoveItTool.PO.ToolEnabled();
                    UIToolOptionPanel.instance.PO_button.activeStateIndex = 1;
                    ActionQueue.instance.Push(new TransformAction());
                }
                else
                {
                    UIToolOptionPanel.instance.PO_button.activeStateIndex = 0;
                    Action.ClearPOFromSelection();
                }
                UIFilters.POToggled();
            }
            catch (ArgumentException e)
            {
                Log.Error($"PO Integration failed:\n{e}");
                if (MoveItTool.PO.Active)
                {
                    MoveItTool.PO.Active = false;
                    UIToolOptionPanel.instance.PO_button.activeStateIndex = 0;
                }
                return false;
            }
            return true;
        }

        /// <returns>Bool - whether any PO changed since MIT was disabled</returns>
        internal bool ToolEnabled()
        {
            Dictionary<uint, PO_Object> newVisible = new Dictionary<uint, PO_Object>();
            HashSet<uint> newIds = new HashSet<uint>();

            foreach (PO_Object obj in Logic.Objects)
            {
                newVisible.Add(obj.Id, obj);
                newIds.Add(obj.Id);
            }

            HashSet<uint> removed = new HashSet<uint>(visibleIds);
            removed.ExceptWith(newIds);
            HashSet<uint> added = new HashSet<uint>(newIds);
            added.ExceptWith(visibleIds);

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
            MoveItTool.m_debugPanel.UpdatePanel();

            //Log.Debug($"Visible from:{visibleObjects.Count} to:{newVisible.Count}\nSelected from:{selectedIds.Count} to:{newSelectedIds.Count}");

            visibleObjects = newVisible;
            visibleIds = newIds;

            if (added.Count > 0 || removed.Count > 0)
                return true;

            return false;
        }

        internal void Delete(PO_Object obj)
        {
            if (!Enabled) return;

            Logic.Delete(obj);
        }

        internal PO_Object ConvertToPO(Instance instance)
        {
            if (!Enabled) return null;

            return Logic.ConvertToPO(instance);
        }

        internal static bool isModEnabled()
        {
            if (!isModInstalled())
            {
                return false;
            }

            return true;
        }

        internal static bool isModInstalled()
        {
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
            if (isModInstalled())
            {
                if (VersionNames.Contains(PO_Logic.getVersion().Substring(0, 3)))
                {
                    return String.Format(Str.integration_PO_Found, PO_Logic.getVersion().Substring(0, 3));
                }
                else
                {
                    return String.Format(Str.integration_PO_WrongVersion, PO_Logic.getVersion().Substring(0, 3));
                }
            }

            return Str.integration_PO_Notfound;
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
            return PO_Logic.getVersion();
        }
    }
}
