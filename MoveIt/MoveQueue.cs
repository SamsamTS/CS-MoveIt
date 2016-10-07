using ICities;
using UnityEngine;

using System;
using System.Diagnostics;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;

namespace MoveIt
{
    internal class MoveQueue
    {
        public enum StepType
        {
            Invalid,
            Selection,
            Move
        }

        public class Step
        {
            public List<Moveable> instances = new List<Moveable>();
            public Vector3 center;

            public bool isSelection = true;
        }

        public class MoveStep : Step
        {
            public Vector3 moveDelta;
            public ushort angleDelta;

            public bool hasMoved
            {
                get
                {
                    return moveDelta != Vector3.zero || angleDelta != 0;
                }
            }
        }

        private Step[] m_moves = new Step[50];
        private int m_moveCurrent = 0;
        private int m_moveHead = 0;
        private int m_moveTail = 0;

        public Step Push(StepType type, bool copySelection = false)
        {
            if(type == StepType.Invalid)
            {
                return null;
            }

            int previous = -1;
            if(currentType != StepType.Invalid)
            {
                previous = m_moveCurrent;
            }

            m_moveCurrent = (m_moveCurrent + 1) % m_moves.Length;
            m_moveHead = m_moveCurrent;
            if (m_moveTail == m_moveHead)
            {
                m_moveTail = (m_moveTail + 1) % m_moves.Length;
            }

            if (type == StepType.Selection)
            {
                m_moves[m_moveCurrent] = new Step();
            }
            else
            {
                m_moves[m_moveCurrent] = new MoveStep();
            }

            if (copySelection && previous != -1)
            {
                if (m_moves[previous] is MoveStep)
                {
                    foreach (Moveable instance in m_moves[previous].instances)
                    {
                        m_moves[m_moveCurrent].instances.Add(new Moveable(instance.id));
                    }
                }
                else
                {
                    m_moves[m_moveCurrent].instances = m_moves[previous].instances;
                    m_moves[m_moveCurrent].center = m_moves[previous].center;
                }
            }

            return m_moves[m_moveCurrent];
        }

        public bool Next()
        {
            if(m_moveCurrent == m_moveHead)
            {
                return false;
            }

            m_moveCurrent = (m_moveCurrent + 1) % m_moves.Length;

            return true;
        }

        public bool Previous()
        {
            if (m_moveCurrent == m_moveTail)
            {
                return false;
            }

            if (--m_moveCurrent < 0)
            {
                m_moveCurrent = m_moves.Length - 1;
            }

            return true;
        }

        public bool hasSelection
        {
            get
            {
                if (m_moveCurrent == m_moveTail || m_moves[m_moveCurrent] == null)
                {
                    return false;
                }

                return m_moves[m_moveCurrent].isSelection && m_moves[m_moveCurrent].instances.Count > 0;
            }
        }

        public int selectionCount
        {
            get
            {
                if (m_moveCurrent == m_moveTail || m_moves[m_moveCurrent] == null)
                {
                    return 0;
                }

                return m_moves[m_moveCurrent].instances.Count;
            }
        }

        public Step current
        {
            get
            {
                if (currentType == StepType.Invalid)
                {
                    return null;
                }

                return m_moves[m_moveCurrent];
            }
        }

        public StepType currentType
        {
            get
            {
                if (m_moveCurrent == m_moveTail || m_moves[m_moveCurrent] == null)
                {
                    return StepType.Invalid;
                }

                if(m_moves[m_moveCurrent] is MoveStep)
                {
                    return StepType.Move;
                }

                return StepType.Selection;
            }
        }
    }
}
