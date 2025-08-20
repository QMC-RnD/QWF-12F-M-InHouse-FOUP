using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FOUPCtrl.HardwareManager.Utilities;

namespace FOUPCtrl.HardwareManager.Serializers
{
    public class DataSerializer : IDataSerializer
    {
        public void Serialize(ISerializableData serializableData, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                var settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    DataContractSerializer serializer = new DataContractSerializer(serializableData.GetType());
                    serializer.WriteObject(writer, serializableData);
                }
            }
        }

        public void Deserialize(ISerializableData serializableData, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    DataContractSerializer serializer = new DataContractSerializer(serializableData.GetType());
                    object deserializedObject = serializer.ReadObject(reader, true);

                    if (deserializedObject.ToString() != serializableData.ToString())
                    {
                        throw new Exception($"Object Id:{serializableData} does not match Id:{deserializedObject} in configuration file {path}.");
                    }

                    deserializedObject.Copy(serializableData, typeof(DataMemberAttribute));
                }
            }
        }

        public List<ISerializableData> DeserializeObjects(string path)
        {
            List<ISerializableData> objects = new List<ISerializableData>();

            foreach (var filePath in Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories))
            {
                string typeString = default(string);
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        reader.ReadToDescendant(nameof(ISerializableData.Type));
                        typeString = reader.ReadElementContentAsString();
                    }
                }

                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(Type.GetType(typeString));
                        var deserializedObject = (ISerializableData)serializer.ReadObject(reader, true);
                        objects.Add(deserializedObject);
                    }
                }
            }

            return objects;
        }
    }
}
