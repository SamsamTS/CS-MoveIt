using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using EManagersLib.API;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    static class PropLayer
    {
        public static IPropsWrapper Manager;

        private static bool EML = false;

        public static void Initialise()
        {
            EML = isEMLInstalled();
            Log.Debug($"EML: {EML}");

            if (EML)
            {
                Manager = new EPropsManager();
            }
            else
            {
                Manager = new PropsManager();
            }
        }

        internal static bool isEMLInstalled()
        {
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in pluginInfo.GetAssemblies())
                {
                    if (assembly.GetName().Name.ToLower().Equals("emanagerslib"))
                    {
                        return pluginInfo.isEnabled;
                    }
                }
            }

            return false;
        }

        internal static string getVersionText()
        {
            if (isEMLInstalled())
            {
                return "Extended Managers Library: Found";
            }

            return "Extended Managers Library: Not Found";
        }
    }

    public interface IPropsWrapper
    {
        IInfo GetInfo(InstanceID id);
        IProp Buffer(uint id);
        IProp Buffer(InstanceID id);
        uint GetId(InstanceID id);
        InstanceID SetProp(InstanceID id, uint i);
        void UpdateProps(float minX, float minZ, float maxX, float maxZ);
        void UpdateProp(ushort id);
        bool CreateProp(out uint clone, PropInfo info, Vector3 position, float angle, bool single);
        InstanceID StepOver(uint id);
        void RaycastHoverInstance(ref int i, ref int j, ref StepOver stepOver, ref Segment3 ray, ref float smallestDist, ref InstanceID id);
        void GetMarqueeList(ref int i, ref int j, ref InstanceID id, ref Quad3 m_selection, ref HashSet<Instance> list);
    }

    class PropsManager : IPropsWrapper
    {
        private readonly PropInstance[] propBuffer;

        public PropsManager()
        {
            propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;
        }

        public IInfo GetInfo(InstanceID id)
        {
            return new Info_Prefab(PropManager.instance.m_props.m_buffer[id.Prop].Info);
        }

        public IProp Buffer(uint id)
        {
            return new PropWrapper((ushort)id);
        }

        public IProp Buffer(InstanceID id)
        {
            return new PropWrapper(id.Prop);
        }

        public uint GetId(InstanceID id)
        {
            return id.Prop;
        }

        public InstanceID SetProp(InstanceID id, uint i)
        {
            id.Prop = (ushort)i;
            return id;
        }

        public void UpdateProps(float minX, float minZ, float maxX, float maxZ)
        {
            Singleton<PropManager>.instance.UpdateProps(minX, minZ, maxX, maxZ);
        }

        public void UpdateProp(ushort id)
        {
            PropManager.instance.UpdateProp(id);
        }

        public bool CreateProp(out uint clone, PropInfo info, Vector3 position, float angle, bool single)
        {
            ushort tempId;
            bool result = PropManager.instance.CreateProp(out tempId, ref SimulationManager.instance.m_randomizer, info, position, angle, single);
            clone = tempId;
            return result;
        }

        public InstanceID StepOver(uint id)
        {
            InstanceID instance = default;
            instance.Prop = (ushort)id;
            return instance;
        }

        public void RaycastHoverInstance(ref int i, ref int j, ref StepOver stepOver, ref Segment3 ray, ref float smallestDist, ref InstanceID id)
        {
            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
            int count = 0;
            while (prop != 0u)
            {
                if (stepOver.isValidP(prop) && Filters.Filter(propBuffer[prop].Info))
                {
                    if (propBuffer[prop].RayCast(prop, ray, out float t, out float targetSqr) && t < smallestDist)
                    {
                        id.Prop = prop;
                        smallestDist = t;
                    }
                }

                prop = propBuffer[prop].m_nextGridProp;

                if (++count > 65536)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Props: Invalid list detected!\n" + Environment.StackTrace);
                }
            }
        }

        public void GetMarqueeList(ref int i, ref int j, ref InstanceID id, ref Quad3 m_selection, ref HashSet<Instance> list)
        {
            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
            int count = 0;
            while (prop != 0u)
            {
                if (Filters.Filter(propBuffer[prop].Info))
                {
                    if (MoveItTool.instance.PointInRectangle(m_selection, propBuffer[prop].Position))
                    {
                        id.Prop = prop;
                        list.Add(id);
                    }
                }

                prop = propBuffer[prop].m_nextGridProp;

                if (++count > 65536)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Prop: Invalid list detected!\n" + Environment.StackTrace);
                }
            }
        }
    }

    // Extended Managers Library support
    class EPropsManager : IPropsWrapper
    {
        private readonly EPropInstance[] propBuffer;

        public EPropsManager()
        {
            propBuffer = PropAPI.GetPropBuffer();
        }

        public IInfo GetInfo(InstanceID id)
        {
            return new Info_Prefab(EPropManager.m_props.m_buffer[id.GetProp32()].Info);
        }

        public IProp Buffer(uint id)
        {
            return new EPropWrapper(id);
        }

        public IProp Buffer(InstanceID id)
        {
            return new EPropWrapper(id.GetProp32());
        }

        public uint GetId(InstanceID id)
        {
            return id.GetProp32();
        }

        public InstanceID SetProp(InstanceID id, uint i)
        {
            id.SetProp32(i);
            return id;
        }

        public void UpdateProps(float minX, float minZ, float maxX, float maxZ)
        {
            EPropManager.UpdateProps(Singleton<PropManager>.instance, minX, minZ, maxX, maxZ);
        }

        public void UpdateProp(ushort id)
        { }

        public bool CreateProp(out uint clone, PropInfo info, Vector3 position, float angle, bool single)
        {
            return PropManager.instance.CreateProp(out clone, ref SimulationManager.instance.m_randomizer, info, position, angle, single);
        }

        public InstanceID StepOver(uint id)
        {
            InstanceID instance = default;
            instance.SetProp32(id);
            return instance;
        }

        public void RaycastHoverInstance(ref int i, ref int j, ref StepOver stepOver, ref Segment3 ray, ref float smallestDist, ref InstanceID id)
        {

            uint[] propGrid = EPropManager.m_propGrid;
            uint prop = propGrid[i * 270 + j];
            while (prop != 0u)
            {
                if (stepOver.isValidP(prop) && Filters.Filter(propBuffer[prop].Info))
                {
                    if (propBuffer[prop].RayCast(prop, ray, out float t, out float targetSqr) && t < smallestDist)
                    {
                        id.SetProp32(prop);
                        smallestDist = t;
                    }
                }
                prop = propBuffer[prop].m_nextGridProp;
            }
        }

        public void GetMarqueeList(ref int i, ref int j, ref InstanceID id, ref Quad3 m_selection, ref HashSet<Instance> list)
        {
            uint[] propGrid = EPropManager.m_propGrid;
            uint prop = propGrid[i * 270 + j];
            while (prop != 0u)
            {
                if (Filters.Filter(propBuffer[prop].Info))
                {
                    if (MoveItTool.instance.PointInRectangle(m_selection, propBuffer[prop].Position))
                    {
                        id.SetProp32(prop);
                        list.Add(id);
                    }
                }
                prop = propBuffer[prop].m_nextGridProp;
            }
        }
    }
}
