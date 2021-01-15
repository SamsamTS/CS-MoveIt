using ColossalFramework;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace MoveIt
{
    public class DebugUtils
    {
        public const string modPrefix = "[MoveIt " + ModInfo.version + "] ";

        public static SavedBool hideDebugMessages = new SavedBool("hideDebugMessages", MoveItTool.settingsFileName, true, true);

        public static void Log(string message)
        {
            if (hideDebugMessages.value) return;

            if (message == m_lastLog)
            {
                m_duplicates++;
            }
            else if (m_duplicates > 0)
            {
                MoveIt.Log.Debug(modPrefix + m_lastLog + "(x" + (m_duplicates + 1) + ")");
                MoveIt.Log.Debug(modPrefix + message);
                m_duplicates = 0;
            }
            else
            {
                MoveIt.Log.Debug(modPrefix + message);
            }
            m_lastLog = message;
        }

        public static void Warning(string message)
        {
            if (message != m_lastWarning)
            {
                MoveIt.Log.Error(modPrefix + "Warning: " + message);
            }
            m_lastWarning = message;
        }

        public static void LogException(Exception e)
        {
            MoveIt.Log.Error(modPrefix + "Intercepted exception (not game breaking):" + Environment.NewLine + $"{e.Message}");
            Debug.LogException(e);
        }

        public static void AssertEq(object lhs, object rhs, string m)
        {

            if (!lhs.Equals(rhs))
            {
#if DEBUG
                throw new AssertionException($"expected {lhs} == {rhs}", m);
#else
                MoveIt.Log.Error($"Error - Assertion failed: expected {lhs} == {rhs}\n" + m);
#endif
            }

        }

        public static void AssertNeq(object lhs, object rhs, string m)
        {
            if (lhs.Equals(rhs))
            {
#if DEBUG
                throw new AssertionException($"expected {lhs} != {rhs}", m);
#else
                MoveIt.Log.Error($"Error - Assertion failed: expected {lhs} != {rhs}\n" + m);
#endif
            }
        }

        private static string m_lastWarning;
        private static string m_lastLog;
        private static int m_duplicates = 0;
    }
}
