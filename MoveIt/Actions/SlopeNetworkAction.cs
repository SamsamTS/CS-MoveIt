//using ColossalFramework;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace MoveIt
//{
//    class SlopeNetworkAction : Action
//    {
//        protected static NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
//        protected static NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

//        internal List<MoveableSegment> pathSegments = new List<MoveableSegment>();
//        internal List<MoveableNode> pathNodes = new List<MoveableNode>();

//        public HashSet<InstanceState> m_states = new HashSet<InstanceState>();

//        private MoveableNode[] keyInstance = new MoveableNode[2];

//        public MoveableNode PointA
//        {
//            get
//            {
//                return keyInstance[0];
//            }
//            set
//            {
//                keyInstance[0] = value;
//            }
//        }
//        public MoveableNode PointB
//        {
//            get
//            {
//                return keyInstance[1];
//            }
//            set
//            {
//                keyInstance[1] = value;
//            }
//        }

//        public SlopeNetworkAction()
//        {
//            foreach (Instance instance in selection)
//            {
//                if (instance.isValid)
//                {
//                    m_states.Add(instance.SaveToState());
//                }
//            }
//        }

//        public override void Do()
//        {
//            throw new NotImplementedException();
//        }

//        public override void Undo()
//        {
//            foreach (InstanceState state in m_states)
//            {
//                state.instance.LoadFromState(state);
//            }

//            UpdateArea(GetTotalBounds(false));
//        }

//        public override void ReplaceInstances(Dictionary<Instance, Instance> toReplace)
//        {
//            foreach (InstanceState state in m_states)
//            {
//                if (toReplace.ContainsKey(state.instance))
//                {
//                    DebugUtils.Log("SlopeNetworkAction Replacing: " + state.instance.id.RawData + " -> " + toReplace[state.instance].id.RawData);
//                    state.ReplaceInstance(toReplace[state.instance]);
//                }
//            }
//        }

//        internal override void Overlays(RenderManager.CameraInfo cameraInfo, Color toolColor, Color despawnColor)
//        {
//            foreach (MoveableSegment ms in pathSegments)
//            {
//                if (ms.isValid && ms != MoveItTool.instance.m_hoverInstance)
//                {
//                    ms.RenderOverlay(cameraInfo, toolColor, despawnColor);
//                }
//            }
//            foreach (MoveableNode mn in pathNodes)
//            {
//                if (mn.isValid && mn != MoveItTool.instance.m_hoverInstance)
//                {
//                    mn.RenderOverlay(cameraInfo, toolColor, despawnColor);
//                }
//            }
//        }
//    }
//}
