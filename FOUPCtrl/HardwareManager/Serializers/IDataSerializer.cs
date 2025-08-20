using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Serializers
{
    public interface IDataSerializer
    {
        void Serialize(ISerializableData serializableData, string path);

        void Deserialize(ISerializableData serializableData, string path);

        List<ISerializableData> DeserializeObjects(string path);
    }
}
