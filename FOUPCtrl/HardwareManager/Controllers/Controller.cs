using FOUPCtrl.HardwareManager.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Controllers
{
    [DataContract(Namespace = "")]
    public abstract class Controller : IController
    {
        [DataMember]
        public string Id { get; protected set; }

        [DataMember]
        public string Type { get; private set; }

        [DataMember]
        public string DriverId { get; set; }

        public Controller()
        {
            Id = GetType().Name;
            Type = GetType().Name;
        }

        public virtual void SetDriver(IDriver driver)
        {
            DriverId = driver?.Id;
        }
    }
}
