using System.Runtime.Serialization;
using FOUPCtrl.HardwareManager.Drivers;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace FOUPCtrl.HardwareManager.Controllers.IOs
{
    [DataContract(Namespace = "")]
    public abstract class IOController : Controller, IIOController
    {
        public IIODriver IODriver { get; private set; }

        [DataMember]
        public IOMode Mode { get; set; }

        [DataMember]
        public bool IsReadOnly { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public int Bit { get; set; }

        [DataMember]
        public int DelayAfterWrite { get; set; }

        [DataMember]
        public bool Invert { get; set; }

        public override void SetDriver(IDriver driver)
        {
            IODriver = ((IIODriver)driver) ?? new FakeIODriver();
            base.SetDriver(IODriver);
        }

        public abstract int AnalogRead();

        public abstract void AnalogWrite(int value);

        public async Task AnalogWriteAsync(int value, CancellationToken token)
        {
            AnalogWrite(value);
            await Task.Delay(DelayAfterWrite, token).ConfigureAwait(false);
        }

        public abstract bool DigitalRead(bool invert = false);

        public abstract void DigitalWrite(bool state, bool invert = false);

        public Task DigitalWriteAsync(bool state, CancellationToken token)
        {
            return DigitalWriteAsync(state, false, token);
        }

        public async Task DigitalWriteAsync(bool state, bool invert, CancellationToken token)
        {
            DigitalWrite(state, invert);
            await Task.Delay(DelayAfterWrite, token).ConfigureAwait(false);
        }

        public async Task<bool> DigitalReadAsync(bool state, bool invert, CancellationToken token)
        {
            await Task.Delay(DelayAfterWrite, token).ConfigureAwait(false);
            if (IODriver is FakeIODriver)
            {
                return state;
            }
            return DigitalRead(invert);
        }
    }
}
