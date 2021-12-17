using ColossalFramework;
using ColossalFramework.Plugins;
using MoveIt.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Network Skins wrapper, supports 2.0

namespace MoveIt
{
    internal class NS_Manager
    {
        internal bool Enabled = false;
        internal readonly Type tNS, tNSM, tNSModifier, tListSkins, tListMods, tDictMods;
        internal readonly object NSM;
        internal readonly Assembly Assembly;

        internal NS_Manager()
        {
            Enabled = false;

            Assembly = MoveItTool.GetAssembly("networkskinsmod", "networkskins", "cimtools");
            if (Assembly != null)
            {
                tNS = Assembly.GetType("NetworkSkins.Skins.NetworkSkin");
                if (tNS == null) throw new Exception("Type NetworkSkins not found (Failed [NS-F2])");
                tNSM = Assembly.GetType("NetworkSkins.Skins.NetworkSkinManager");
                if (tNSM == null) throw new Exception("Type NetworkSkinManager not found (Failed [NS-F3])");
                tNSModifier = Assembly.GetType("NetworkSkins.Skins.NetworkSkinModifier");
                if (tNSModifier == null) throw new Exception("Type NetworkSkinModifier not found (Failed [NS-F4])");

                tListSkins = typeof(List<>).MakeGenericType(new Type[] { tNS });
                tListMods = typeof(List<>).MakeGenericType(new Type[] { tNSModifier });
                tDictMods = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeof(NetInfo), tListMods });

                NSM = tNSM.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                if (NSM == null) throw new Exception("Object NetworkSkinManager not found (Failed [NS-F5])");

                Enabled = true;
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

        //internal static Assembly GetNSAssembly()
        //{
        //    Log.Debug($"AAB01 GetNSAssembly");
        //    try
        //    {
        //        foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
        //        {
        //            try
        //            {
        //                Log.Debug($"AAB02 GetNSAssembly");
        //                Log.Debug($"AAB02.1 {pluginInfo}");
        //                Log.Debug($"AAB02.2 {pluginInfo.userModInstance ?? "<null>"}");
        //                Log.Debug($"AAB02.3 {pluginInfo.userModInstance?.GetType()}");
        //                Log.Debug($"AAB02.4 {pluginInfo.userModInstance?.GetType().Name}");
        //                if (pluginInfo.userModInstance?.GetType().Name.ToLower() == "networkskinsmod" && pluginInfo.isEnabled)
        //                {
        //                    Log.Debug($"AAB03 GetNSAssembly");
        //                    // Network Skins 1 - unsupported - uses CimTools
        //                    if (pluginInfo.GetAssemblies().Any(mod => mod.GetName().Name.ToLower() == "cimtools"))
        //                    {
        //                        Log.Debug($"AAB04 GetNSAssembly");
        //                        break;
        //                    }

        //                    Log.Debug($"AAB05 GetNSAssembly");
        //                    foreach (Assembly assembly in pluginInfo.GetAssemblies())
        //                    {
        //                        Log.Debug($"AAB06 GetNSAssembly");
        //                        if (assembly.GetName().Name.ToLower() == "networkskins")
        //                        {
        //                            Log.Debug($"AAB07 GetNSAssembly");
        //                            return assembly;
        //                        }
        //                    }
        //                }
        //            }
        //            catch (ReflectionTypeLoadException)
        //            {
        //                Log.Debug($"AAB09 ReflectionTypeLoadException");
        //            } // If the plugin parsing fails, go to next plugin
        //            catch (NullReferenceException)
        //            {
        //                Log.Debug($"AAB10 NullReferenceException");
        //            } // If the plugin parsing fails, go to next plugin
        //        }
        //        Log.Debug($"AAB08 GetNSAssembly");
        //    }
        //    catch (ReflectionTypeLoadException)
        //    {
        //        Log.Debug($"AAB11 ReflectionTypeLoadException");
        //    } // If the plugin parsing fails, go to next plugin
        //    catch (NullReferenceException)
        //    {
        //        Log.Debug($"AAB12 NullReferenceException");
        //    } // If the plugin parsing fails, go to next plugin

        //    return null;
        //}

        internal static string getVersionText()
        {
            if (MoveItTool.GetAssembly("networkskinsmod", "networkskins", "cimtools") != null)
            {
                return Str.integration_NS2_Found;
            }

            return Str.integration_NS2_Notfound;
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
