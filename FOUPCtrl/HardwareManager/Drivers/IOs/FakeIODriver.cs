using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Drivers.IOs
{
    public sealed class FakeIODriver : Driver, IIODriver
    {
        public int AnalogValue { get; set; } = 255;

        public int BitsState { get; set; } = 0xFF;

        public IReadOnlyList<object> Mutexes { get; set; } = new List<object>()
        {
            new object(),
            new object(),
            new object(),
            new object(),
        }
        .AsReadOnly();

        public FakeIODriver()
        {
            Connected = true;
        }

        public override void Connect()
        {
        }

        public override void Disconnect()
        {
        }

        public int AnalogRead(int pin)
        {
            return AnalogValue;
        }

        public void AnalogWrite(int pin, int value)
        {
        }

        public int DigitalRead(int portId)
        {

            return BitsState;
        }

        public void DigitalWrite(int portId, int state)
        {
            BitsState = state;
        }
    }
}
