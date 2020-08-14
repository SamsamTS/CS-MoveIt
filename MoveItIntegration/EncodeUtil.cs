using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace MoveItIntegration
{
    public static class EncodeUtil
    {
        internal static BinaryFormatter GetBinaryFormatter => new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        internal static object Deserialize(byte[] data)
        {
            if (data == null) return null;

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

        /// <summary>
        /// Converts Base64 string to object
        /// </summary>
        public static object BinaryDecode64(string base64Data)
        {
            if (base64Data == null || base64Data == "") return null;
            byte[] bytes = Convert.FromBase64String(base64Data);
            return Deserialize(bytes);
        }

        /// <summary>
        /// Converts object to Base64 string
        /// </summary>
        public static string BinaryEncode64(object obj)
        {
            if (obj == null) return null;
            var bytes = Serialize(obj);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Converts string to XML
        /// </summary>
        /// <paramref name="data">the string to convert</param>
        /// <param name="dataType">the type of the returned XML object</param>
        public static object XMLEncode(string data, Type dataType)
        {
            if (data == null || data == "") return null;
            XmlSerializer xmlSerializer = new XmlSerializer(dataType);
            StringReader sr = new StringReader(data);
            XmlReader reader = XmlReader.Create(sr);
            return xmlSerializer.Deserialize(reader);
        }

        /// <summary>
        /// Converts XML to string
        /// </summary>
        public static string XMLDecode(object obj)
        {
            if (obj == null) return null;
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
            StringWriter sw = new StringWriter();
            XmlWriter writer = XmlWriter.Create(sw);
            xmlSerializer.Serialize(writer, obj);
            return sw.ToString(); // Your xml
        }
    }
}
