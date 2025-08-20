using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FOUPCtrl.HardwareManager.Serializers;
using FOUPCtrl.HardwareManager.Drivers;

namespace FOUPCtrl.HardwareManager.Controllers
{
    public interface IController : ISerializableData
    {
        string Id { get; }

        string DriverId { get; }

        void SetDriver(IDriver driver);
    }
}
