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
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    public interface IProp
    {
        Vector3 Position { get; set; }
        float Angle { get; set; }
        bool FixedHeight { get; set; }
        bool Single { get; set; }
        ushort m_flags { get; set; }
        PropInfo Info { get; }
        uint Index { get; }

        void MoveProp(Vector3 position);
        void UpdatePropRenderer(bool updateGroup);
        void ReleaseProp();
    }

    class PropWrapper : IProp
    {
        private readonly ushort index;
        private readonly PropInstance[] Buffer = PropManager.instance.m_props.m_buffer;

        public void MoveProp(Vector3 position)
        {
            Singleton<SimulationManager>.instance.AddAction(() => PropManager.instance.MoveProp(index, position));
        }

        public void UpdatePropRenderer(bool updateGroup)
        {
            Singleton<SimulationManager>.instance.AddAction(() => PropManager.instance.UpdatePropRenderer(index, updateGroup));
        }

        public void ReleaseProp()
        {
            Singleton<SimulationManager>.instance.AddAction(() => PropManager.instance.ReleaseProp(index));
        }

        public uint Index => index;


        public PropWrapper(ushort i)
        {
            index = i;
        }

        public Vector3 Position
        {
            get => Buffer[index].Position;
            set => Buffer[index].Position = value;
        }

        public float Angle
        {
            get => Buffer[index].Angle;
            set => Buffer[index].Angle = value;
        }

        public bool FixedHeight
        {
            get => Buffer[index].FixedHeight;
            set => Buffer[index].FixedHeight = value;
        }

        public bool Single
        {
            get => Buffer[index].Single;
            set => Buffer[index].Single = value;
        }

        public ushort m_flags
        {
            get => Buffer[index].m_flags;
            set => Buffer[index].m_flags = value;
        }

        public PropInfo Info
        {
            get => Buffer[index].Info;
        }
    }

    // Extended Managers Library support
    class EPropWrapper : IProp
    {
        private readonly uint index;
        private readonly EPropInstance[] Buffer = EPropManager.m_props.m_buffer;

        public void MoveProp(Vector3 position)
        {
            PropManager.instance.MoveProp(index, position);
        }

        public void UpdatePropRenderer(bool updateGroup)
        {
            PropManager.instance.UpdatePropRenderer(index, updateGroup);
        }

        public void ReleaseProp()
        {
            PropManager.instance.ReleaseProp(index);
        }

        public uint Index => index;


        public EPropWrapper(uint i)
        {
            index = i;
        }

        public Vector3 Position
        {
            get => Buffer[index].Position;
            set => Buffer[index].Position = value;
        }

        public float Angle
        {
            get => Buffer[index].Angle;
            set => Buffer[index].Angle = value;
        }

        public bool FixedHeight
        {
            get => Buffer[index].FixedHeight;
            set => Buffer[index].FixedHeight = value;
        }

        public bool Single
        {
            get => Buffer[index].Single;
            set => Buffer[index].Single = value;
        }

        public ushort m_flags
        {
            get => Buffer[index].m_flags;
            set => Buffer[index].m_flags = value;
        }

        public PropInfo Info
        {
            get => Buffer[index].Info;
        }
    }
}
