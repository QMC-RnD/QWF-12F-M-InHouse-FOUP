using FOUPCtrl.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FOUPCtrl.HardwareManager.Drivers.IOs;
using FOUPCtrl.HardwareManager.Controllers;

namespace FOUPCtrl.HardwareManager.Controllers.IOs
{
    public interface IIOController : IController
    {
        IIODriver IODriver { get; }

        IOMode Mode { get; }

        bool IsReadOnly { get; }

        int Port { get; }

        int Bit { get; }

        int DelayAfterWrite { get; }

        bool Invert { get; }

        int AnalogRead();

        void AnalogWrite(int value);

        Task AnalogWriteAsync(int value, CancellationToken token);

        bool DigitalRead(bool invert = false);
        Task<bool> DigitalReadAsync(bool state, bool invert, CancellationToken token);
        void DigitalWrite(bool state, bool invert = false);

        Task DigitalWriteAsync(bool state, CancellationToken token);

        Task DigitalWriteAsync(bool state, bool invert, CancellationToken token);
    }
}
