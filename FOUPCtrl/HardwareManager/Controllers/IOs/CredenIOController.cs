using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using FOUPCtrl.Utilities;

namespace FOUPCtrl.HardwareManager.Controllers.IOs
{
    [DataContract(Namespace = "")]
    public class CredenIOController : IOController
    {
        public CredenIOController(string id)
        {
            Id = id;
            Mode = IOMode.Digital;
        }

        public override int AnalogRead()
        {
            if (Mode == IOMode.Digital)
            {
                throw new InvalidOperationException(
                    $"{Id} cannot AnalogRead because it is a {Mode} signal type.");
            }

            return IODriver.AnalogRead(Bit);
        }

        public override void AnalogWrite(int value)
        {
            if (Mode == IOMode.Digital)
            {
                throw new InvalidOperationException(
                    $"{Id} cannot AnalogWrite because it is a {Mode} signal type.");
            }

            IODriver.AnalogWrite(Bit, value);
        }

        public override bool DigitalRead(bool invert = false)
        {
            if (Mode == IOMode.Analog)
            {
                throw new InvalidOperationException(
                    $"{Id} cannot DigitalRead because it is an {Mode} signal type.");
            }



            IntBits portBits = ReadPortIdBits(Port);
            bool state = portBits[Bit];
            return Invert ? !state : state;
        }

        public override void DigitalWrite(bool state, bool invert = false)
        {
            if (Mode == IOMode.Analog)
            {
                throw new InvalidOperationException(
                    $"{Id} cannot DigitalWrite because it is an {Mode} type.");
            }

            IODriver.Mutexes = IODriver.Mutexes ?? new List<object>()
            {
                new object(),
                new object(),
                new object(),
                new object(),
            }
            .AsReadOnly();

            lock (IODriver.Mutexes[Port])
            {
                IntBits portBits = ReadPortIdBits(Port);

                if (Invert)
                {
                    state = !state;
                }

                if (portBits[Bit] != state)
                {
                    portBits[Bit] = state;
                    IODriver.DigitalWrite(Port, portBits.Bits);
                }
            }
        }

        private IntBits ReadPortIdBits(int portId)
        {
            int portValue = IODriver.DigitalRead(portId);
            return new IntBits(portValue);
        }
    }
}
