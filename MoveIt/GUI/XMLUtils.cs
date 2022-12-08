using MoveIt.GUI;
using System;

namespace MoveIt
{
    public class XMLUtils
    {
        public static string SortType(XMLWindow.SortTypes type)
        {
            return type.ToString();
        }
        public static XMLWindow.SortTypes SortType(string type)
        {
            return (XMLWindow.SortTypes)Enum.Parse(typeof(XMLWindow.SortTypes), type);
        }

        public static string SortOrders(XMLWindow.SortOrders type)
        {
            return type.ToString();
        }
        public static XMLWindow.SortOrders SortOrders(string type)
        {
            return (XMLWindow.SortOrders)Enum.Parse(typeof(XMLWindow.SortOrders), type);
        }
    }

    /// <summary>
    /// Data for each entry in the file list
    /// </summary>
    public struct FileData
    {
        public string m_name;
        public DateTime m_date;
        public long m_size;

        public string GetName()
        {
            return m_name;
        }

        public string GetDate()
        {
            return m_date.ToShortDateString();
        }

        public string GetDateExtended()
        {
            return m_date.ToLongTimeString() + ", " + m_date.ToLongDateString();
        }

        public string GetSize()
        {
            float s = m_size;
            if (s > 1024)
            {
                s /= 1024;
                if (s > 1024)
                {
                    s /= 1024;
                    return String.Format("{0:0.##}MB", s);
                }
                return String.Format("{0:0.##}KB", s);
            }
            return String.Format("{0:0.##}B", s);
        }
    }
}
