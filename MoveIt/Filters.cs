using MoveIt;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    class Filters
    {
        static readonly string[] SurfaceMeshNames = new string[]
        {
            "ploppablegravel",
            "ploppablecliffgrass",
            "ploppableasphalt-prop"
        };


        static readonly string[] SurfaceExtraBuildingNames = new string[]
        {
            "1136492728.R69 Docks", "999653286.Ploppable"
        };
        static readonly string[] SurfaceExtraPropNames = new string[]
        {
            "999653286.Ploppable"
        };
        static readonly string[] SurfaceExtraDecalNames = new string[]
        {
            "ploppableasphalt-decal"
        };
        static readonly string[] SurfaceBrushNames = new string[]
        {
            "418194210.", "416836974.", "416837329.", "416837674.", "416916315.", "416924392.", // Agriculture
            "418187161.", "416103351.", "423541340.", "416107948.", "416109689.", "416916560.", "416924629.", // Concrete
            "416106438.", "416108293.", "416110102.", "416917279.", "416924830.", "416924830.", // Gravel
            "418188094.", "416107568.", "422323255.", "416109334.", "416111243.", "416917700.", "416925008.", // Ruined
            "418188427.", "418188861.", "418415108.", // Tiled
            "418187791.", "418188652.", "418414886." // Marble
        };
        static readonly string[] PillarClassNames = new string[]
        {
            "Highway", "Pedestrian Path", "Train Track", "Monorail Track", "CableCar Facility"
        };
        static readonly Type[] PylonAITypes = new Type[]
        {
            typeof(PowerPoleAI)
        };


        public static Dictionary<string, NetworkFilter> NetworkFilters = new Dictionary<string, NetworkFilter>
        {
            { "Roads", new NetworkFilter(true, new List<Type> { typeof(RoadBaseAI) }, new List<string> { "Pedestrian Path", "Beautification Item" } ) },
            { "Tracks", new NetworkFilter(true, new List<Type> { typeof(TrainTrackBaseAI), typeof(MonorailTrackAI) } ) },
            { "Paths", new NetworkFilter(true, new List<Type> { typeof(PedestrianPathAI), typeof(PedestrianTunnelAI), typeof(PedestrianBridgeAI), typeof(PedestrianWayAI) } ) },
            { "Fences", new NetworkFilter(true, new List<Type> { typeof(DecorationWallAI) } ) },
            { "Powerlines", new NetworkFilter(true, new List<Type> { typeof(PowerLineAI) } ) },
            { "Others", new NetworkFilter(true) }
        };


        public static void SetAnyFilter(string name, bool active)
        {
            if (NetworkFilters.ContainsKey(name))
            {
                SetNetworkFilter(name, active);
            }
            else
            {
                SetFilter(name, active);
            }
        }


        public static void SetFilter(string name, bool active)
        {
            switch (name)
            {
                case "Buildings":
                    MoveItTool.filterBuildings = active;
                    break;
                case "Props":
                    MoveItTool.filterProps = active;
                    break;
                case "Decals":
                    MoveItTool.filterDecals = active;
                    break;
                case "Surfaces":
                    MoveItTool.filterSurfaces = active;
                    break;
                case "Trees":
                    MoveItTool.filterTrees = active;
                    break;
                case "PO":
                    MoveItTool.filterProcs = active;
                    break;
                case "Nodes":
                    MoveItTool.filterNodes = active;
                    break;
                case "Segments":
                    MoveItTool.filterSegments = active;
                    break;

                default:
                    throw new Exception();
            }

            //UI.RefreshFilters();
        }

        public static void SetNetworkFilter(string name, bool active)
        {
            NetworkFilters[name].enabled = active;
            //UI.RefreshFilters();
        }

        public static void ToggleFilter(string name)
        {
            switch (name)
            {
                case "Buildings":
                    MoveItTool.filterBuildings = !MoveItTool.filterBuildings;
                    break;
                case "Props":
                    MoveItTool.filterProps = !MoveItTool.filterProps;
                    break;
                case "Decals":
                    MoveItTool.filterDecals = !MoveItTool.filterDecals;
                    break;
                case "Surfaces":
                    MoveItTool.filterSurfaces = !MoveItTool.filterSurfaces;
                    break;
                case "Trees":
                    MoveItTool.filterTrees = !MoveItTool.filterTrees;
                    break;
                case "PO":
                    MoveItTool.filterProcs = !MoveItTool.filterProcs;
                    break;
                case "Nodes":
                    MoveItTool.filterNodes = !MoveItTool.filterNodes;
                    break;
                case "Segments":
                    MoveItTool.filterSegments = !MoveItTool.filterSegments;
                    break;

                default:
                    throw new Exception();
            }
        }

        public static void ToggleNetworkFilter(string name)
        {
            NetworkFilters[name].enabled = !NetworkFilters[name].enabled;
        }


        public static bool IsSurface(BuildingInfo info)
        {
            //if (MoveItTool.extraAsSurfaces)
            //{
                foreach (string subname in SurfaceExtraBuildingNames)
                {
                    if (subname.Length > info.name.Length) continue;
                    if (subname == info.name.Substring(0, subname.Length))
                    {
                        return true;
                    }
                }
            //}

            //if (MoveItTool.brushesAsSurfaces)
            //{
                foreach (string subname in SurfaceBrushNames)
                {
                    if (subname.Length > info.name.Length) continue;
                    if (subname == info.name.Substring(0, subname.Length))
                    {
                        return true;
                    }
                }
            //}

            return false;
        }

        public static bool IsSurface(PropInfo info)
        {
            if (Array.Exists(SurfaceMeshNames, s => s.Equals(info.m_mesh.name)))
            {
                return true;
            }

            //if (MoveItTool.decalsAsSurfaces)
            //{
                if (info.m_isDecal)
                {
                    if (Array.Exists(SurfaceExtraDecalNames, s => s.Equals(info.m_mesh.name)))
                    {
                        return true;
                    }
                }
                else
                {
                    foreach (string subname in SurfaceExtraPropNames)
                    {
                        if (subname.Length > info.name.Length) continue;
                        if (subname == info.name.Substring(0, subname.Length))
                        {
                            return true;
                        }
                    }
                }
            //}

            return false;
        }


        public static bool Filter(BuildingInfo info, bool isHover = false)
        {
            if (isHover)
            {
                //Select P&P on hover with Alt
                if (MoveItTool.altSelectNodeBuildings)
                {
                    //Debug.Log($"SINGLE-Pi m_class.name:{info.m_class.name}");
                    if (Array.Exists(PillarClassNames, s => s.Equals(info.m_class.name)))
                    {
                        if (Event.current.alt)
                        {
                            //Debug.Log("Alt");
                            return true;
                        }
                        return false;
                    }

                    //Debug.Log($"SINGLE-Py AI type:{info.GetAI().GetType()}");
                    if (Array.Exists(PylonAITypes, s => s.Equals(info.GetAI().GetType())))
                    {
                        if (Event.current.alt)
                        {
                            //Debug.Log("Alt");
                            return true;
                        }
                        return false;
                    }
                }

                if (!MoveItTool.marqueeSelection)
                {
                    return true;
                }
            }

            if (IsSurface(info))
            {
                if (MoveItTool.filterSurfaces) return true;
                else return false;
            }

            if (MoveItTool.filterBuildings)
            {
                //Filter pillars and pylons out of select
                if (MoveItTool.altSelectNodeBuildings)
                {
                    //Debug.Log($"MARQUEE m_class.name:{info.m_class.name}");
                    if (Array.Exists(PillarClassNames, s => s.Equals(info.m_class.name)))
                    {
                        return false;
                    }

                    if (Array.Exists(PylonAITypes, s => s.Equals(info.GetAI().GetType())))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool Filter(PropInfo info)
        {
            if (!MoveItTool.marqueeSelection) return true;

            if (info.m_isDecal)
            {
                //if (MoveItTool.extraAsSurfaces)
                //{
                    if (MoveItTool.filterSurfaces && IsSurface(info))
                    {
                        return true;
                    }
                    if (MoveItTool.filterDecals && !IsSurface(info))
                    {
                        return true;
                    }
                //}
                //else
                //{
                //    if (MoveItTool.filterDecals)
                //    {
                //        return true;
                //    }
                //}
                return false;
            }

            if (IsSurface(info))
            {
                if (MoveItTool.filterSurfaces)
                {
                    return true;
                }
                return false;
            }

            if (MoveItTool.filterProps)
            {
                return true;
            }
            return false;
        }

        public static bool Filter(NetNode node)
        {
            if (MoveItTool.instance.AlignMode == MoveItTool.AlignModes.Group || MoveItTool.instance.AlignMode == MoveItTool.AlignModes.Inplace)
            {
                return false;
            }
            return _networkFilter(node.Info);
        }

        public static bool Filter(NetSegment segment)
        {
            return _networkFilter(segment.Info);
        }

        private static bool _networkFilter(NetInfo info)
        {
            //Debug.Log($"{info.name}");
            if (!MoveItTool.marqueeSelection) return true;
            if (!MoveItTool.filterNetworks) return true;

            NetworkFilter nf = NetworkFilter.GetNetworkFilter(info);
            return nf.enabled;
        }
    }


    public class NetworkFilter
    {
        public bool enabled;
        public List<Type> aiTypes;
        public List<string> excludeClasses;

        public NetworkFilter(bool e, List<Type> a = null, List<string> ex = null)
        {
            enabled = e;
            aiTypes = a;
            excludeClasses = ex;
        }

        public static void SetNetworkFilter(string name, bool e)
        {
            Filters.NetworkFilters[name].enabled = e;
        }

        public static NetworkFilter GetNetworkFilter(NetInfo info)
        {
            foreach (NetworkFilter nf in Filters.NetworkFilters.Values)
            {
                //Debug.Log($"ai:{ai}, count:{(nf.aiType == null ? 0 : nf.aiType.Count)}");
                if (nf.aiTypes != null)
                {
                    foreach (Type t in nf.aiTypes)
                    {
                        if (info.GetAI().GetType() == t || info.GetAI().GetType().IsSubclassOf(t))
                        {
                            if (nf.excludeClasses == null)
                            {
                                return nf;
                            }
                            if (!nf.excludeClasses.Contains(info.m_class.name))
                            {
                                return nf;
                            }
                        }
                    }
                }
            }
            return Filters.NetworkFilters["Others"];
        }
    }
}
