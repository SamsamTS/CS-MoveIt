using ColossalFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
            MoveIt.Log.Error(modPrefix + "Intercepted exception (not game breaking):" + Environment.NewLine + $"{e.GetType()} - {e.Message}");
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


    public class ObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;

        private ObjectDumper(int indentSize)
        {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static string Dump(object element)
        {
            return Dump(element, 2);
        }

        public static string Dump(object element, int indentSize)
        {
            var instance = new ObjectDumper(indentSize);
            return instance.DumpElement(element);
        }

        private string DumpElement(object element, int depth = 0)
        {
            depth++;
            if (depth > 10)
                return "";

            try
            {
                if (element == null || element is ValueType || element is string)
                {
                    Write(FormatValue(element));
                }
                else if (element is Instance ins)
                {
                    Write($"Instance {ins.id.Debug()}:{ins.Info.Prefab}");
                }
                else if (element is IInfo info)
                {
                    Write($"IInfo {info.Name}");
                }
                else
                {
                    var objectType = element.GetType();
                    if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                    {
                        Write("{{{0}}}", objectType.FullName);
                        _hashListOfFoundElements.Add(element.GetHashCode());
                        _level++;
                    }

                    IEnumerable enumerableElement = element as IEnumerable;
                    if (enumerableElement != null)
                    {
                        foreach (object item in enumerableElement)
                        {
                            if (item is IEnumerable && !(item is string))
                            {
                                _level++;
                                DumpElement(item, depth);
                                _level--;
                            }
                            else
                            {
                                if (!AlreadyTouched(item))
                                    DumpElement(item, depth);
                                else
                                    Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                            }
                        }
                    }
                    else
                    {
                        MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var memberInfo in members)
                        {
                            var fieldInfo = memberInfo as FieldInfo;
                            var propertyInfo = memberInfo as PropertyInfo;

                            if (fieldInfo == null && propertyInfo == null)
                                continue;

                            var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                            object value = fieldInfo != null
                                               ? fieldInfo.GetValue(element)
                                               : propertyInfo.GetValue(element, null);

                            if (type.IsValueType || type == typeof(string))
                            {
                                Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                            }
                            else
                            {
                                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                                Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                                var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                                _level++;
                                if (!alreadyTouched)
                                    DumpElement(value, depth);
                                else
                                    Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                                _level--;
                            }
                        }
                    }

                    if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                    {
                        _level--;
                    }
                }
            }
            catch (TargetParameterCountException e)
            {
                Log.Error($"ObjectDumper failed - TargetParameterCountException\n{e}");
            }

            return _stringBuilder.ToString();
        }

        private bool AlreadyTouched(object value)
        {
            if (value == null)
                return false;

            var hash = value.GetHashCode();
            for (var i = 0; i < _hashListOfFoundElements.Count; i++)
            {
                if (_hashListOfFoundElements[i] == hash)
                    return true;
            }
            return false;
        }

        private void Write(string value, params object[] args)
        {
            var space = new string(' ', _level * _indentSize);

            if (args != null)
                value = string.Format(value, args);

            _stringBuilder.AppendLine(space + value);
        }

        private string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime)
                return (((DateTime)o).ToShortDateString());

            if (o is string)
                return string.Format("\"{0}\"", o);

            if (o is char && (char)o == '\0')
                return string.Empty;

            if (o is ValueType)
                return (o.ToString());

            if (o is IEnumerable)
                return ("...");

            return ("{ }");
        }
    }
}
