using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Serializers
{
    public interface ISerializableData
    {
        string Type { get; }
    }
}
