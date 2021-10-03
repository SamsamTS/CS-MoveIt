using ColossalFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    public class StepOver
    {
        private Vector3 MousePosition = Vector3.zero;
        public bool active = false;
        public List<InstanceID> buffer = new List<InstanceID>();

        Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

        public StepOver()
        {
            buffer.Clear();
        }


        public bool isValidB(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = default;
            instance.Building = id;
            return _isValid(instance);
        }

        public bool isValidP(uint id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            return _isValid(PropLayer.Manager.StepOver(id));
            //InstanceID instance = default;
            //instance.Prop = id;
            //return _isValid(instance);
        }

        public bool isValidPO(uint id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = default;
            instance.NetLane = id;
            return _isValid(instance);
        }

        public bool isValidT(uint id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = default;
            instance.Tree = id;
            return _isValid(instance);
        }

        public bool isValidN(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = default;
            instance.NetNode = id;
            return _isValid(instance);
        }

        public bool isValidS(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = default;
            instance.NetSegment = id;
            return _isValid(instance);
        }

        private bool _isValid(InstanceID id)
    {
            if (Input.mousePosition != MousePosition)
            {
                active = false;
                buffer.Clear();
                return true;
            }
            if (buffer.Contains(id))
            {
                return false;
            }
            return true;
        }


        public void Add(InstanceID id, int depth = 0)
        {
            if (id.Equals(InstanceID.Empty))
            {
                return;
            }
            if (buffer.Contains(id))
            {
                return;
            }
            MousePosition = Input.mousePosition;
            buffer.Add(id);

            if (id.Type == InstanceType.Building && buildingBuffer[id.Building].m_subBuilding > 0)
            {
                InstanceID subId = default;
                subId.Building = buildingBuffer[id.Building].m_subBuilding;
                Add(subId, depth++);
            }

            if (depth > 1000)
            {
                throw new Exception("Step-over reached depth of > 1000");
            }
        }
    }
}
