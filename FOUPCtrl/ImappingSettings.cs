using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl
{
    public interface IMappingSettings
    {
        double MapStartPositionMm { get; }
        double MapEndPositionMm { get; }
        double MmPerPulse { get; }
        int SensorType { get; }
    }
}
