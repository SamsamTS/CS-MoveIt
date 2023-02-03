using ColossalFramework;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace MoveIt
{
    internal static class EncodeUtil
    {
        internal static BinaryFormatter GetBinaryFormatter =>
                new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        internal static object Deserialize(byte[] data)
        {
            if (data == null)
                return null;

            var memoryStream = new MemoryStream();
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            return GetBinaryFormatter.Deserialize(memoryStream);
        }

        internal static byte[] Serialize(object obj)
        {
            var memoryStream = new MemoryStream();
            GetBinaryFormatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0; // redundant ?
            return memoryStream.ToArray();
        }

        internal static object Decode64(string base64Data)
        {
            if (base64Data == null || base64Data == "")
                return null;
            byte[] bytes = Convert.FromBase64String(base64Data);
            return Deserialize(bytes);
        }

        internal static string Encode64(object obj)
        {
            if (obj == null)
                return null;
            var bytes = Serialize(obj);
            return Convert.ToBase64String(bytes);
        }

        internal static string XML2String(object obj)
        {
            if (obj == null) return null;
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
            StringWriter sw = new StringWriter();
            XmlWriter writer = XmlWriter.Create(sw);
            xmlSerializer.Serialize(writer, obj);
            return sw.ToString(); // Your xml
        }

        internal static object String2XML(string data, Type dataType)
        {
            if (data == null || data == "") return null;
            XmlSerializer xmlSerializer = new XmlSerializer(dataType);
            StringReader sr = new StringReader(data);
            XmlReader reader = XmlReader.Create(sr);
            return xmlSerializer.Deserialize(reader);
        }

    }
}
