using FOUPCtrl.HardwareManager.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Drivers
{
    public interface IDriver : ISerializableData
    {
        string Id { get; }

        bool Connected { get; }

        void Connect();

        void Disconnect();
    }
}
