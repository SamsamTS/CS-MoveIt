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
        internal static readonly string[] VersionNames = { "2.0" };
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

        public void SetSegmentSkin(ushort id, SegmentState state)
        {
            if (!Enabled) return;

            BindingFlags f = BindingFlags.Public | BindingFlags.Instance;
            object modifiers = state.NS_Modifiers;

            object modDict = Activator.CreateInstance(tDictMods);
            tDictMods.GetMethod("Add", f, null, new Type[] { typeof(NetInfo), tListMods }, null).Invoke(modDict, new[] { (NetInfo)state.Info.Prefab, modifiers });
            //Debug.Log($"modList:{modDict} (length:{tDictMods.GetProperty("Count").GetValue(modDict, null)})");

            tNSM.GetMethod("SetActiveModifiers", f, null, new Type[] { tDictMods }, null).Invoke(NSM, new[] { modDict });
            tNSM.GetMethod("OnSegmentPlaced", f, null, new Type[] { typeof(ushort) }, null).Invoke(NSM, new object[] { id });
        }

        public object GetSegmentStateSkin(ushort id)
        {
            if (!Enabled) return null;

            object skin = _GetSegmentSkin(id);
            if (skin == null)
            {
                return null;
            }
            //Debug.Log($"Modifiers:{tNS.GetProperty("Modifiers", BindingFlags.Public | BindingFlags.Instance).GetValue(skin, null)}");

            return tNS.GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skin);
        }

        private object _GetSegmentSkin(ushort id)
        {
            if (!Enabled) return null;

            object[] SegmentSkinsArray = (object[])tNSM.GetField("SegmentSkins").GetValue(NSM);
            return SegmentSkinsArray[id];
        }

        public object GetNodeStateSkin(ushort id)
        {
            if (!Enabled) return null;

            object skin = _GetNodeSkin(id);
            if (skin == null)
            {
                return null;
            }
            //Debug.Log($"Modifiers:{tNS.GetProperty("Modifiers", BindingFlags.Public | BindingFlags.Instance).GetValue(skin, null)}");

            return tNS.GetField("_modifiers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skin);
        }

        private object _GetNodeSkin(ushort id)
        {
            if (!Enabled) return null;

            object[] NodeSkinsArray = (object[])tNSM.GetField("NodeSkins").GetValue(NSM);
            return NodeSkinsArray[id];
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
                    mod.name.Contains("NetworkSkins") ||
                    mod.name.Contains("Network Skins") ||
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
                if (VersionNames.Contains(getVersion()))
                {
                    return $"Network Skins version {getVersion()} found, integration enabled!\n ";
                }
                else
                {
                    return $"NS integration failed - found version {getVersion()} (required: 2.0)\n ";
                }
            }

            return "Network Skins is not available. To use these options please quit Cities Skylines and subscribe to the latest Network Skins.\n ";
        }

        public static string getVersion()
        {
            try
            {
                Assembly nsAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {

                    if (assembly.FullName.Length >= 12 && assembly.FullName.Substring(0, 12) == "NetworkSkins")
                    {
                        nsAssembly = assembly;
                        break;
                    }
                }

                if (nsAssembly == null)
                {
                    return "(Failed [NS-F1])";
                }

                if (!isModInstalled())
                {
                    return "(Failed [NS-F2])";
                }

                return "2.0";
            }
            catch (Exception e)
            {
                Debug.Log($"NS INTERATION FAILED\n" + e);
            }

            return "(Failed [NS-F3])";
        }


        public string EncodeModifiers(object obj)
        {
            if (!Enabled) return null;

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
