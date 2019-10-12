using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Network Skins wrapper, supports 2.0

namespace MoveIt
{
    internal class NS_Manager
    {
        internal bool Enabled = false;
        //internal static readonly string[] VersionNames = { "2" };
        internal readonly Type tNS, tNSM, tNSModifier, tListSkins, tListMods, tDictMods;
        internal readonly object NSM;
        internal readonly Assembly Assembly;

        internal NS_Manager()
        {
            if (isModInstalled())
            {
                Enabled = true;

                Assembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {

                    if (assembly.FullName.Length >= 12 && assembly.FullName.Substring(0, 12) == "NetworkSkins")
                    {
                        Assembly = assembly;
                        break;
                    }
                }

                if (Assembly == null)
                {
                    throw new Exception("Assembly not found (Failed [NS-F1])");
                }

                tNS = Assembly.GetType("NetworkSkins.Skins.NetworkSkin");
                tNSM = Assembly.GetType("NetworkSkins.Skins.NetworkSkinManager");
                tNSModifier = Assembly.GetType("NetworkSkins.Skins.NetworkSkinModifier");
                tListSkins = typeof(List<>).MakeGenericType(new Type[] { tNS });
                tListMods = typeof(List<>).MakeGenericType(new Type[] { tNSModifier });
                tDictMods = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(NetInfo), tListMods });

                NSM = tNSM.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
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
            object modifiers = state.NS_Modifiers;
            if (modifiers == null)
            {
                return;
            }

            object modDict = Activator.CreateInstance(tDictMods);
            tDictMods.GetMethod("Add", f, null, new Type[] { typeof(NetInfo), tListMods }, null).Invoke(modDict, new[] { (NetInfo)state.Info.Prefab, modifiers });

            tNSM.GetMethod("SetActiveModifiers", f, null, new Type[] { tDictMods }, null).Invoke(NSM, new[] { modDict });
            tNSM.GetMethod("OnSegmentPlaced", f, null, new Type[] { typeof(ushort) }, null).Invoke(NSM, new object[] { id });
        }

        public object GetSegmentModifiers(ushort id)
        {
            if (!Enabled) return null;

            object skin = _GetSegmentSkin(id);
            if (skin == null)
            {
                return null;
            }

            return tNS.GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skin);
        }

        private object _GetSegmentSkin(ushort id)
        {
            if (!Enabled) return null;

            object[] SegmentSkinsArray = (object[])tNSM.GetField("SegmentSkins").GetValue(NSM);
            return SegmentSkinsArray[id];
        }

        internal static bool isModInstalled()
        {
            //string msg = "";
            //foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            //{
            //    msg += $"\n{pi.publishedFileID.AsUInt64} - {pi.name} ({pi.isEnabled})" +
            //        $"\n - {pi.modPath}";
            //}
            //Debug.Log(msg);

            if (!PluginManager.instance.GetPluginsInfo().Any(mod => (
                    mod.publishedFileID.AsUInt64 == 1758376843uL ||
                    mod.name.Contains("NetworkSkins2") ||
                    mod.name.Contains("1758376843")
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
                return "Network Skins 2 found, integration enabled!\n ";
            }

            return "Network Skins 2 not found, integration disabled.\n ";
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
