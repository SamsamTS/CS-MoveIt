using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// TMPE wrapper, supports 2.0

namespace MoveIt
{
    internal class TMPE_Manager
    {
        internal bool Enabled = false;
        internal readonly Assembly Assembly;

        internal readonly Type tRecordable, tNodeRecord, tSegmentRecord, tSegmentEndRecord;
        internal MethodInfo mRecord, mTransfer;
        internal MethodInfo mRecord, mTransfer;

        internal TMPE_Manager()
        {
            if (isModInstalled())
            {
                Enabled = true;

                Assembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Length >= 12 && assembly.FullName.Substring(0, 12) == "TrafficManager")
                    {
                        Assembly = assembly;
                        break;
                    }
                }

                if (Assembly == null) throw new Exception("Assembly not found (Failed [TMPE-F1])");

                tRecordable = Assembly.GetType("TrafficManager.Util.Record.IRecordable")
                    ?? throw new Exception("Type TrafficManager.Util.Record.IRecordable not found (Failed [TMPE-F2])");

                tNodeRecord = Assembly.GetType("TrafficManager.Util.Record.NodeRecord")
                    ?? throw new Exception("Type TrafficManager.Util.Record.NodeRecord not found (Failed [TMPE-F3])");

                tSegmentRecord = Assembly.GetType("TrafficManager.Util.Record.SegmentRecord")
                    ?? throw new Exception("Type TrafficManager.Util.Record.SegmentRecord not found (Failed [TMPE-F4])");

                tSegmentEndRecord = Assembly.GetType("TrafficManager.Util.Record.SegmentEndRecord")
                    ?? throw new Exception("Type TrafficManager.Util.Record.SegmentEndRecord not found (Failed [TMPE-F5])");
            }
            else
            {
                Enabled = false;
            }
        }


        public void SetSegmentModifiers(ushort id, SegmentState state)
        {
            if (!Enabled) return;

            BindingFlags f = BindingFlags.Public | BindingFlags.Instance;
            object modifiers = state.TMPE_Modifiers;
            if (modifiers == null)
            {
                return;
            }

            object modDict = Activator.CreateInstance(tDictMods);
            tDictMods.GetMethod("Add", f, null, new Type[] { typeof(NetInfo), tListMods }, null).Invoke(modDict, new[] { (NetInfo)state.Info.Prefab, modifiers });

            tTMPEM.GetMethod("SetActiveModifiers", f, null, new Type[] { tDictMods }, null).Invoke(TMPEM, new[] { modDict });
            tTMPEM.GetMethod("OnSegmentPlaced", f, null, new Type[] { typeof(ushort) }, null).Invoke(TMPEM, new object[] { id });
        }

        public object GetSegmentModifiers(ushort id)
        {
            if (!Enabled) return null;

            object skin = _GetSegmentSkin(id);
            if (skin == null)
            {
                return null;
            }

            return tTMPE.GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skin);
        }

        private object _GetSegmentSkin(ushort id)
        {
            if (!Enabled) return null;

            object[] SegmentSkinsArray = (object[])tTMPEM.GetField("SegmentSkins").GetValue(TMPEM);
            return SegmentSkinsArray[id];
        }

        internal static bool isModInstalled()
        {
            if (!PluginManager.instance.GetPluginsInfo().Any(mod => (
                    mod.publishedFileID.AsUInt64 == 1758376843uL ||
                    mod.name.Contains("NetworkSkins2") ||
                    mod.name.Contains("1758376843")
            ) && mod.isEnabled))
            {
                return false;
            }

            if (PluginManager.instance.GetPluginsInfo().Any(mod => 
                    mod.publishedFileID.AsUInt64 == 543722850uL ||
                    (mod.name.Contains("NetworkSkins") && !mod.name.Contains("NetworkSkins2")) ||
                    mod.name.Contains("543722850")
            ))
            {
                return false;
            }

            return true;
        }

        internal static string getVersionText()
        {
            if (isModInstalled())
            {
                return "Network Skins 2 found, integration enabled!\n ";
            }

            return "Network Skins 2 not found, or TMPE1 and TMPE2 both subscribed, integration disabled.\n ";
        }

        public string EncodeModifiers(object obj)
        {
            if (!Enabled) return null;
            if (obj == null) return null;

            Type t = Assembly.GetType("NetworkSkins.Skins.Serialization.ModifierDataSerializer");

            var bytes = (byte[])t.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static, null, new Type[] { tListMods }, null).Invoke(null, new[] { obj });
            var base64 = Convert.ToBase64String(bytes);

            return base64;
        }

        public object DecodeModifiers(string base64String)
        {
            if (!Enabled) return null;

            Type t = Assembly.GetType("NetworkSkins.Skins.Serialization.ModifierDataSerializer");

            var bytes = Convert.FromBase64String(base64String);
            var modifiers = t.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(byte[]) }, null).Invoke(null, new[] { bytes });

            return modifiers;
        }
    }
}
