using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Drivers
{
    [DataContract(Namespace = "")]
    public abstract class Driver : IDriver
    {
        [DataMember]
        public string Id { get; protected set; }

        [DataMember]
        public string Type { get; private set; }

        public virtual bool Connected { get; protected set; }

        public Driver()
        {
            Id = GetType().Name;
            Type = GetType().FullName;
            Connected = false;
        }

        public abstract void Connect();

        public abstract void Disconnect();
    }
}
