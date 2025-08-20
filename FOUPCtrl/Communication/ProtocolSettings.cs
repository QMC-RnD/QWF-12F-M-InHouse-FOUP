using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.Communication
{
    public class ProtocolSettings
    {
        public bool EnableCRC16Protocol { get; set; } = true;
        public byte StationCode { get; set; } = 0x30;
        public byte Address { get; set; } = 0x30;
        public bool EnableLogging { get; set; } = true;
        public bool ValidateChecksum { get; set; } = true;

        public static ProtocolSettings Default => new ProtocolSettings();
    }
}
