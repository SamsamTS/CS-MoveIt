using ColossalFramework;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

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
            //Log.Debug($"SerializationUtil.Deserialize(data): data.Length={data?.Length}");

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
                return "";
            var bytes = Serialize(obj);
            return Convert.ToBase64String(bytes);
        }

    }
}
