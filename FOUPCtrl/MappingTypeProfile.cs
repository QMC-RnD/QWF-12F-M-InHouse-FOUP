// FOUPCtrl/MappingTypeProfile.cs
namespace FOUPCtrl
{
    public class MappingTypeProfile
    {
        public string Name { get; set; }

        // References to separate tables
        public int PositionTableNo { get; set; } = 1; // 1-based index
        public int MappingTableNo { get; set; } = 1;  // 1-based index
        public int FOUPTypeIndex { get; set; } = 0;

        // These properties are now accessed through referenced tables
        public double MapStartPositionMm { get; set; } // For backward compatibility
        public double MapEndPositionMm { get; set; }   // For backward compatibility
        public double MmPerPulse { get; set; }         // For backward compatibility
        public int SensorType { get; set; }            // For backward compatibility
        public double SlotPitchMm { get; set; }        // For backward compatibility
        public double WaferThicknessMm { get; set; }   // For backward compatibility

        public MappingTypeProfile()
        {
            // Default constructor
        }

        public MappingTypeProfile(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} (PosTable: {PositionTableNo}, MapTable: {MappingTableNo})";
        }
    }
}
