using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MarketingServer
{
    public class Serialization
    {
        public static string Serialize(object content)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, content);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static T Deserialize<T>(string content)
        {
            byte[] bytes = Convert.FromBase64String(content);
            using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return (T)new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}