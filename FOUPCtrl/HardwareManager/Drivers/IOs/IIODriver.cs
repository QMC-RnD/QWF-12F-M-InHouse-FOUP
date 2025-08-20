using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Drivers.IOs
{
    public interface IIODriver : IDriver
    {
        IReadOnlyList<object> Mutexes { get; set; }

        int AnalogRead(int pin);

        void AnalogWrite(int pin, int value);

        int DigitalRead(int portId);

        void DigitalWrite(int portId, int state);
    }
}
