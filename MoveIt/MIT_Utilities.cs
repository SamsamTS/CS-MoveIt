using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using UnifiedUI.Helpers;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        #region Debug Overlays
        internal struct DebugOverlay
        {
            internal Quad3 quad;
            internal Color32 color;

            public DebugOverlay(Quad3 q, Color32 c)
            {
                quad = q;
                color = c;
            }
        }
        private static List<DebugOverlay> DebugBoxes = new List<DebugOverlay>();
        private static List<Vector3> DebugPoints = new List<Vector3>();
        internal static void AddDebugBox(Bounds b, Color32? c = null)
        {
            if (c == null)
            {
                c = new Color32(255, 255, 255, 63);
            }
            Quad3 q = default;
            q.a = new Vector3(b.min.x, b.min.y, b.min.z);
            q.b = new Vector3(b.min.x, b.min.y, b.max.z);
            q.c = new Vector3(b.max.x, b.min.y, b.max.z);
            q.d = new Vector3(b.max.x, b.min.y, b.min.z);
            DebugOverlay d = new DebugOverlay(q, (Color32)c);
            DebugBoxes.Add(d);
            //Log.Debug($"\nBounds:{b}");
        }
        internal static void AddDebugPoint(Vector3 v)
        {
            DebugPoints.Add(v);
            Log.Debug($"\nPoint:{v}");
        }
        internal void ClearDebugOverlays()
        {
            DebugBoxes.Clear();
            DebugPoints.Clear();
        }
        internal static Color32 GetRandomDebugColor()
        {
            return new Color32(RandomByte(100, 255), RandomByte(100, 255), RandomByte(100, 255), 63);
        }
        #endregion

        internal void EnableUUI()
        {
            if (!useUUI)
            {
                return;
            }

            try
            {
                var hotkeys = new UUIHotKeys { ActivationKey = OptionsKeymapping.toggleTool };
                foreach (var item in OptionsKeymapping.InToolKeysForSelection) {
                    hotkeys.AddInToolKey(item, Action.HasSelection);
                }
                foreach (var item in OptionsKeymapping.InToolKeysAlways) {
                    hotkeys.AddInToolKey(item);
                }
                Texture2D texture = ResourceLoader.loadTextureFromAssembly("MoveIt.Icons.MoveIt.png");

                UUIButton = UUIHelpers.RegisterToolButton(
                    name: "MoveItTool",
                    groupName: null,
                    tooltip: "Move It",
                    tool: this,
                    icon: texture,
                    hotkeys: hotkeys
                    );
                m_button.Hide();
            }
            catch (Exception e)
            {
                Log.Error(e);
                UIView.ForwardException(e);
            }
        }

        internal void DisableUUI()
        {
            m_button?.Show();
            UUIButton?.Destroy();
            UUIButton = null;
        }

        internal static Color GetSelectorColor(Color c)
        {
            if (!m_showSelectors)
            {
                c.a = 0f;
            }
            return c;
        }

        internal static void UpdatePillarMap()
        {
            if (!advancedPillarControl) return;

            //Log.Debug("UPM Start");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            m_pillarMap = new Dictionary<ushort, ushort>();
            //string msg = "UPM Nodes: ";
            //int c = 0;
            for (ushort i = 0; i < nodeBuffer.Length; i++)
            {
                NetNode n = nodeBuffer[i];
                if ((n.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created)
                {
                    if (n.m_building > 0)
                    {
                        //msg += $"{i} ({(((buildingBuffer[n.m_building].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden) ? "Visible" : "Hidden")}), ";
                        //c++;
                        //if (c % 20 == 0)
                        //{
                        //    Log.Debug(msg);
                        //    msg = "";
                        //}

                        if ((buildingBuffer[n.m_building].m_flags & Building.Flags.Hidden) != Building.Flags.Hidden)
                        {
                            if (!m_pillarMap.ContainsKey(n.m_building))
                            {
                                try
                                {
                                    m_pillarMap.Add(n.m_building, i);
                                }
                                catch (Exception e)
                                {
                                    string msg2 = $"BuildingID: #{n.m_building} {buildingBuffer[n.m_building].Info?.name}, Count:{m_pillarMap.Count}" + Environment.NewLine;
                                    foreach (var kvp in m_pillarMap)
                                    {
                                        msg2 += $"{kvp.Key}->{kvp.Value}, ";
                                    }
                                    Log.Error(msg2 + e, true);
                                    //DebugUtils.LogException(e);
                                }
                            }
                        }
                    }
                }
            }
            watch.Stop();
            Log.Info($"Move It found {m_pillarMap.Count} attached pillar/pylons in {watch.ElapsedMilliseconds} ms.");
        }

        public static bool IsExportSelectionValid()
        {
            return CloneActionBase.GetCleanSelection(out Vector3 center).Count > 0;
        }

        public bool Export(string filename)
        {
            string path = Path.Combine(saveFolder, filename + ".xml");

            try
            {
                HashSet<Instance> selection = CloneActionBase.GetCleanSelection(out Vector3 center);

                if (selection.Count == 0) return false;

                bool includesPO = false;
                //foreach (Instance ins in selection)
                //{
                //    if (ins is MoveableProc)
                //    {
                //        includesPO = true;
                //        break;
                //    }
                //}

                Selection selectionState = new Selection
                {
                    version = ModInfo.version,
                    center = center,
                    includesPO = includesPO,
                    states = new InstanceState[selection.Count]
                };

                int i = 0;
                foreach (Instance instance in selection)
                {
                    selectionState.states[i++] = instance.SaveToState();
                }

                Directory.CreateDirectory(saveFolder);

                //Log.Debug($"selectionState:{selectionState.states.Length}\n" + ObjectDumper.Dump(selectionState));

                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Selection));
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    xmlSerializer.Serialize(stream, selectionState, ns);
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("Couldn't export selection");
                DebugUtils.LogException(e);

                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Export failed", "The selection couldn't be exported to '" + path + "'\n\n" + e.Message, true);
                return false;
            }

            return true;
        }

        public void Import(string filename)
        {
            ImportImpl(filename, false);
        }

        public void Restore(string filename)
        {
            ImportImpl(filename, true);
        }

        private void ImportImpl(string filename, bool restore)
        {
            lock (ActionQueue.instance)
            {
                StopCloning();
                StopTool();

                //bool activatePO = true;
                //if (!PO.Active)
                //{
                //    activatePO = false;
                //    PO.InitialiseTool(true);
                //}

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Selection));
                Selection selectionState;

                string path = Path.Combine(saveFolder, filename + ".xml");

                try
                {
                    // Trying to Deserialize the file
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        selectionState = xmlSerializer.Deserialize(stream) as Selection;
                    }
                }
                catch (Exception e)
                {
                    // Couldn't Deserialize (XML malformed?)
                    DebugUtils.Log("Couldn't load file");
                    DebugUtils.LogException(e);

                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Import failed", "Couldn't load '" + path + "'\n\n" + e.Message, true);
                    return;
                }

                //if (!activatePO && !selectionState.includesPO)
                //{
                //    PO.InitialiseTool(false);
                //}

                ImportFromArray(selectionState, restore);
            }
        }

        internal void ImportFromArray(Selection selectionState, bool restore = false)
        {
            if (selectionState != null && selectionState.states != null && selectionState.states.Length > 0)
            {
                HashSet<string> missingPrefabs = new HashSet<string>();

                foreach (InstanceState state in selectionState.states)
                {
                    if (state.Info.Prefab == null)
                    {
                        missingPrefabs.Add(state.prefabName);
                    }
                }

                if (missingPrefabs.Count > 0)
                {
                    DebugUtils.Warning("Missing prefabs: " + string.Join(", ", missingPrefabs.ToArray()));

                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Assets missing", "The following assets are missing and will be ignored:\n\n" + string.Join("\n", missingPrefabs.ToArray()), false);
                }

                // Set props to fixed-height if in asset editor
                if ((ToolManager.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None)
                {
                    foreach (InstanceState state in selectionState.states)
                    {
                        if (state is PropState ps)
                        {
                            ps.fixedHeight = true;
                            ps.position.y = ps.position.y - ps.terrainHeight + 60f; // 60 is editor's terrain height
                        }
                    }
                }

                CloneActionBase action = new CloneActionImport(selectionState.states, selectionState.center);

                if (action.Count > 0)
                {
                    ActionQueue.instance.Push(action);

                    if (restore)
                    {
                        SimulationManager.instance.AddAction(() => { ActionQueue.instance.Do(); });
                        //ActionQueue.instance.Do(); // For restore to position
                    }
                    else
                    {
                        SetToolState(ToolStates.Cloning); // For clone
                    }

                    UIToolOptionPanel.RefreshCloneButton();
                    UIToolOptionPanel.RefreshAlignHeightButton();
                }
            }
        }

        public void Delete(string filename)
        {
            try
            {
                string path = Path.Combine(saveFolder, filename + ".xml");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                DebugUtils.Log("Couldn't delete file");
                DebugUtils.LogException(ex);

                return;
            }
        }

        internal static bool isTreeAnarchyEnabled()
        {
            if (GetAssembly("tamod", "treeanarchy") == null)
            {
                Log.Debug($"TreeAnarchy not found");
                return false;
            }

            Log.Debug($"TreeAnarchy found");
            return true;
        }

        internal static bool isTreeSnappingEnabled()
        {
            if (GetAssembly("mod", "treesnapping") == null)
            {
                Log.Debug($"TreeSnapping not found");
                return false;
            }

            Log.Debug($"TreeSnapping found");
            return true;
        }

        internal static Assembly GetAssembly(string modName, string assName, string assNameExcept = "", bool onlyEnabled = true)
        {
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                try
                {
                    if (pluginInfo.userModInstance?.GetType().Name.ToLower() == modName && (!onlyEnabled || pluginInfo.isEnabled))
                    {
                        if (assNameExcept.Length > 0)
                        {
                            if (pluginInfo.GetAssemblies().Any(mod => mod.GetName().Name.ToLower() == assNameExcept))
                            {
                                break;
                            }
                        }
                        foreach (Assembly assembly in pluginInfo.GetAssemblies())
                        {
                            if (assembly.GetName().Name.ToLower() == assName)
                            {
                                return assembly;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                } // If the plugin fails to process, move on to next plugin
            }

            return null;
        }

        internal static void FixTreeFixedHeightFlag()
        {
            if (!MoveItLoader.IsGameLoaded)
            {
                ExceptionPanel notLoaded = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                notLoaded.SetMessage("Not In-Game", "If Tree Snapping is causing tree heights to be off, click this straight after loading your city to fix the issue.", false);
                return;
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            string message;
            int count = 0, total = 0;

            for (uint treeId = 0; treeId < Singleton<TreeManager>.instance.m_trees.m_buffer.Length; treeId++)
            {
                TreeInstance tree = Singleton<TreeManager>.instance.m_trees.m_buffer[treeId];
                if ((tree.m_flags & (ushort)TreeInstance.Flags.Created) == (ushort)TreeInstance.Flags.None) continue;
                total++;

                if (tree.FixedHeight) continue;

                // check if it should have FixedHeight flag
                float terrainHeight = TerrainManager.instance.SampleDetailHeight(tree.Position);
                if (tree.Position.y < terrainHeight - 0.075f || tree.Position.y > terrainHeight + 0.075f)
                {
                    Singleton<TreeManager>.instance.m_trees.m_buffer[treeId].FixedHeight = true;
                    count++;
                }

                if (treeId > 100_000_000)
                {
                    throw new Exception("Scanning too many trees, aborting");
                }
            }
            if (count > 0)
            {
                ActionQueue.instance.Clear();
                message = $"Adjusted {count} tree{(count == 1 ? "" : "s")} out of {total}!";
            }
            else
            {
                message = $"No unflagged floating trees out of {total}, nothing has been changed.";
            }
            panel.SetMessage("Flagging Trees", message, false);

        }

        internal static void CleanGhostNodes()
        {
            if (!MoveItLoader.IsGameLoaded)
            {
                ExceptionPanel notLoaded = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                notLoaded.SetMessage("Not In-Game", "Use this button when in-game to remove ghost nodes (nodes with no segments attached, which were previously created by Move It)", false);
                return;
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            string message;
            int count = 0;

            for (ushort nodeId = 0; nodeId < Singleton<NetManager>.instance.m_nodes.m_buffer.Length; nodeId++)
            {
                NetNode node = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None) continue;
                if ((node.m_flags & NetNode.Flags.Untouchable) != NetNode.Flags.None) continue;
                bool hasSegments = false;

                for (int i = 0; i < 8; i++)
                {
                    if (node.GetSegment(i) > 0)
                    {
                        hasSegments = true;
                        break;
                    }
                }

                if (!hasSegments)
                {
                    count++;
                    Singleton<NetManager>.instance.ReleaseNode(nodeId);
                }
            }
            if (count > 0)
            {
                ActionQueue.instance.Clear();
                message = $"Removed {count} ghost node{(count == 1 ? "" : "s")}!";
            }
            else
            {
                message = "No ghost nodes found, nothing has been changed.";
            }
            panel.SetMessage("Removing Ghost Nodes", message, false);
        }

        internal static void NagOldLSM()
        {
            if (!lsmHasNagged)// && FoundOldLSM()) // check LSM
            {
                m_lsmWarningPanel = UILSMWarning.Open(typeof(UILSMWarning));
                lsmHasNagged = true;
            }
        }

        /// <summary>
        /// Check for old versions of Loading Screen Mod, to nag the player to update
        /// </summary>
        /// <returns>Was an old version found?</returns>
        internal static bool FoundOldLSM()
        {
            if (GetAssembly("mod", "loadingscreenmodklyte", "", false) != null)
            {
                return true;
            }

            if (GetAssembly("mod", "loadingscreenmod", "loadingscreenmodrevisited", false) != null)
            {
                return true;
            }

            return false;
        }

        internal static byte RandomByte(byte min, byte max)
        {
            return (byte)Rand.Next(min, max);
        }

        protected static void ReleaseSegmentBlock(ushort segment, ref ushort segmentBlock)
        {
            if (segmentBlock != 0)
            {
                ZoneManager.instance.ReleaseBlock(segmentBlock);
                segmentBlock = 0;
            }
        }

        public static string InstanceIDDebug(Instance instance)
        {
            if (instance == null) return "(null instance)";
            return $"(B:{instance.id.Building},P:{instance.id.Prop},T:{instance.id.Tree},N:{instance.id.NetNode},S:{instance.id.NetSegment},L:{instance.id.NetLane})";
        }

        internal static HashSet<Bounds> MergeBounds(HashSet<Bounds> outerList)
        {
            HashSet<Bounds> innerList = new HashSet<Bounds>();
            HashSet<Bounds> newList = new HashSet<Bounds>();

            int c = 0;

            do
            {
                foreach (Bounds outer in outerList)
                {
                    //Color32 color = GetRandomDebugColor();
                    //AddDebugBox(outer, color);

                    bool merged = false;

                    float outerVolume = outer.size.x * outer.size.y * outer.size.z;
                    foreach (Bounds inner in innerList)
                    {
                        float separateVolume = (inner.size.x * inner.size.y * inner.size.z) + outerVolume;

                        Bounds encapsulated = inner;
                        encapsulated.Encapsulate(outer);
                        float encapsulateVolume = encapsulated.size.x * encapsulated.size.y * encapsulated.size.z;

                        if (!merged && encapsulateVolume < separateVolume)
                        {
                            newList.Add(encapsulated);
                            merged = true;
                        }
                        else
                        {
                            newList.Add(inner);
                        }
                    }
                    if (!merged)
                    {
                        newList.Add(outer);
                    }

                    innerList = new HashSet<Bounds>(newList);
                    newList.Clear();
                }

                if (outerList.Count <= innerList.Count)
                {
                    break;
                }
                outerList = new HashSet<Bounds>(innerList);
                innerList.Clear();

                if (c > 1000)
                {
                    Log.Error($"Looped bounds-merge a thousand times");
                    break;
                }

                c++;
            }
            while (true);

            //foreach (Bounds b in innerList)
            //{
            //    b.Expand(4f);
            //    AddDebugBox(b, new Color32(255, 0, 0, 200));
            //}
            //Log.Debug($"\nStart:{originalList.Count}\nInner:{innerList.Count}");
            return innerList;
        }

        internal void ProcessMirror(AlignMirrorAction action)
        {
            StartCoroutine(ProcessMirrorIterate(action));
        }

        internal IEnumerator<object> ProcessMirrorIterate(AlignMirrorAction action)
        {
            const uint MaxAttempts = 1000_000;

            uint c = 0;
            while (c < MaxAttempts && POProcessing > 0)
            {
                c++;
                yield return new WaitForSeconds(0.05f);
            }

            if (c == MaxAttempts)
            {
                throw new Exception($"Failed to mirror PO");
            }

            action.DoMirrorProcess();
        }
    }
}
