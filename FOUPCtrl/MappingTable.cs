using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl
{
    public class MappingTable
    {
        public string Name { get; set; } = "Default";
        public int SensorType { get; set; } = 0;
        public int SlotCount { get; set; } = 0;
        public double SlotPitchMm { get; set; } = 0;
        public double PositionRangeMm { get; set; } = 0;
        public double PositionRangeUpperPercent { get; set; } = 0;  
        public double PositionRangeLowerPercent { get; set; } = 0;
        public double WaferThicknessMm { get; set; } = 0;
        public double ThicknessRangeMm { get; set; } = 0;
        public double OffsetMm { get; set; } = 0;
        public double FirstSlotPositionMm { get; set; } = 0;
        public MappingTable() { }

        public MappingTable(string name)
        {
            Name = name;
        }
    }
}
