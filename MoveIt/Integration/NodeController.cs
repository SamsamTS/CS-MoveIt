using ColossalFramework.Plugins;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// Network Skins wrapper, supports 2.0

namespace MoveIt
{
    internal class NodeController_Manager
    {
        internal bool Enabled = false;
        //internal static readonly string[] VersionNames = { "2" };
        internal readonly Type tNodeManager;
        internal readonly MethodInfo mCopy, mPaste;
        internal readonly Assembly Assembly;
        internal const UInt64 ID = 2085403475ul;
        internal const string NAME = "NodeController";

        internal NodeController_Manager()
        {
            if (isModInstalled())
            {
                Enabled = true;

                Assembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {

                    if (assembly.FullName.StartsWith(NAME))
                    {
                        Assembly = assembly;
                        break;
                    }
                }
                if (Assembly == null) throw new Exception("Assembly not found (Failed [NS-F1])");

                tNodeManager = Assembly.GetType("NodeController.NodeManager")
                    ?? throw new Exception("Type NodeManager not found (Failed [NC-F2])");
                BindingFlags f = BindingFlags.Public | BindingFlags.Static;

                mCopy = tNodeManager.GetMethod("CopyNodeData", f)
                    ?? throw new Exception("NodeController.NodeManager.CopyNodeData() not found (Failed [NC-F3])");
                
                mPaste = tNodeManager.GetMethod("PasteNodeData", f)
                    ?? throw new Exception("NodeController.NodeManager.PasteNodeData() not found (Failed [NC-F4])");
            }
            else
            {
                Enabled = false;
            }
        }

        public void PasteNode(ushort nodeID, NodeState state)
        {
            if (!Enabled) return;
            byte[] data = state.NodeControllerData;
            mPaste.Invoke(null, new object[] {nodeID, data});
        }

        public byte[] CopyNode(ushort nodeID)
        {
            if (!Enabled) return null;
            return mCopy.Invoke(null, new object[] {nodeID}) as byte[];
        }

        internal string Encode64(byte[] data)
        {
            if (!Enabled || data == null || data.Length == 0) return null;
            return Convert.ToBase64String(data);
        }
        internal byte[] Decode64(string base64Data)
        {
            if (!Enabled || base64Data == null || base64Data.Length == 0) return null;
            return Convert.FromBase64String(base64Data);
        }

        internal static bool isModInstalled()
        {
            return PluginManager.instance.GetPluginsInfo().Any( mod =>{
                bool found = mod.publishedFileID.AsUInt64 == ID || mod.name.Contains(NAME) || mod.name.Contains(ID.ToString());
                return found && mod.isEnabled;
            }); 
        }

        internal static string getVersionText()
        {
            if (isModInstalled())
                return "Node Controller found, integration enabled!\n ";
            else
                return "Node Controller not found, integration disabled.\n ";
        }
    }
}
