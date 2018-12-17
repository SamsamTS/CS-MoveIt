using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    public class StepOver
    {
        private Vector3 MousePosition = Vector3.zero;
        public bool active = false;
        public List<InstanceID> buffer = new List<InstanceID>();


        public StepOver()
        {
            buffer.Clear();
        }


        public bool isValidB(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = new InstanceID();
            instance.Building = id;
            return _isValid(instance);
        }

        public bool isValidP(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = new InstanceID();
            instance.Prop = id;
            return _isValid(instance);
        }

        public bool isValidT(uint id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = new InstanceID();
            instance.Tree = id;
            return _isValid(instance);
        }

        public bool isValidN(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = new InstanceID();
            instance.NetNode = id;
            return _isValid(instance);
        }

        public bool isValidS(ushort id)
        {
            if (id == 0) return true;
            if (buffer.Count == 0) return true;
            InstanceID instance = new InstanceID();
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


        public void Add(InstanceID id)
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

            //Debug.Log($"Adding ({id.Building},{id.Prop},{id.Tree},{id.NetNode},{id.NetSegment})");
            //string msg = $"Id:{id.Building},{id.Prop},{id.Tree},{id.NetNode},{id.NetSegment}, Buffer ({buffer.Count} elements):\n";
            //foreach (InstanceID b in buffer)
            //{
            //    msg += $"{b.Building},{b.Prop},{b.Tree},{b.NetNode},{b.NetSegment}\n";
            //}
            //msg += $"Contains:{buffer.Contains(id)}";
            //Debug.Log(msg);
        }
    }
}
