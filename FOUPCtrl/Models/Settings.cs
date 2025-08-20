using FOUPCtrl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FOUPCtrl.Utilities;

namespace FOUPCtrl.Models
{
    public sealed class Settings : IMappingSettings
    {
        #region Constants and Section Names
        // Section names for the INI file
        public const string GeneralSection = "General";
        public const string MappingSection = "Mapping";
        public const string HardwareSection = "Hardware";
        public const string SystemSection = "System";

        // Constants for section format strings
        private const string MappingTypeFormat = "Mapping_Type{0}";
        private const string FOUPTypeFormat = "FOUP_Type_{0}";
        private const string PositionTableFormat = "Position_Table_{0}";
        private const string MappingTableFormat = "Mapping_Table_{0}";
        #endregion

        #region File Paths
        // File paths for settings
        public static readonly string BaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QWF-12F-M");
        public static readonly string BaseSettingsDirectory = Path.Combine(BaseDirectory, "Settings");
        public static readonly string SettingsDirectory = Path.Combine(BaseSettingsDirectory, "Settings.ini");
        public static readonly string OEMDirectory = Path.Combine(BaseSettingsDirectory, "Settings_OEMini");

        public static readonly string BaseDirectoryLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QWF-12F-M");
        public static readonly string BaseSettingsDirectoryLocal = Path.Combine(BaseDirectoryLocal, "Settings");
        public static readonly string OEMDirectoryLocal = Path.Combine(BaseSettingsDirectoryLocal, "Settings_OEM.ini");
        #endregion

        #region Singleton Implementation
        static Settings() { }

        private Settings()
        {
            InitializeDefaults();
        }

        public static Settings Instance { get; private set; } = new Settings();

        public void ResetInstance()
        {
            Instance = new Settings();
        }
        #endregion

        #region Properties
        public IniDocument IniDocument { get; set; } = new IniDocument();

        // General settings
        public string MachineId { get; set; }

        // Global mapping parameters (now used only as defaults)
        public int SlotCount { get; set; }
        public double PositionRangeMm { get; set; }
        public double PositionRangeUpperPercent { get; set; }
        public double PositionRangeLowerPercent { get; set; }
        public double ThicknessRangeMm { get; set; }
        public double OffsetMm { get; set; }

        // Hardware parameters
        public double MmPerPulse
        {
            get { return 0.18; }
            set { /* Optional: You can still allow setting but ignore it */ }
        }
        public double PulsePerRevolution { get; set; }
        public double ScrewLeadMm { get; set; }

        // System parameters
        public int LogLevel { get; set; }
        public bool SaveMappingData { get; set; }
        public string DataExportPath { get; set; }
        public bool AutoMapOnLoad { get; set; }

        // Legacy mapping parameters for backward compatibility
        public double MapStartPositionMm { get; set; }
        public double MapEndPositionMm { get; set; }
        public int SensorType { get; set; }
        public double SlotPitchMm { get; set; }
        public double WaferThicknessMm { get; set; }

        // CRC16 Protocol settings
        public bool EnableCRC16Protocol { get; set; } = false;
        public byte ProtocolStationCode { get; set; } = 0x30;
        public byte ProtocolAddress { get; set; } = 0x30;
        public bool EnableProtocolLogging { get; set; } = true;
        #endregion

        #region FOUP Type Profiles
        public int ActiveMappingType { get; set; } = 1;

        private List<MappingTypeProfile> _mappingTypeProfiles;
        public List<MappingTypeProfile> MappingTypeProfiles
        {
            get
            {
                if (_mappingTypeProfiles == null)
                {
                    _mappingTypeProfiles = new List<MappingTypeProfile>();
                    for (int i = 1; i <= 5; i++)
                    {
                        _mappingTypeProfiles.Add(new MappingTypeProfile($"Type {i}"));
                    }
                }
                return _mappingTypeProfiles;
            }
        }

        // Current active profile - for backward compatibility
        public MappingTypeProfile CurrentProfile => MappingTypeProfiles[ActiveMappingType - 1];
        #endregion

        #region Position Tables
        private List<PositionTable> _positionTables;
        public List<PositionTable> PositionTables
        {
            get
            {
                if (_positionTables == null)
                {
                    _positionTables = new List<PositionTable>();
                    for (int i = 1; i <= 5; i++)
                    {
                        _positionTables.Add(new PositionTable($"Position Table {i}"));
                    }
                }
                return _positionTables;
            }
        }

        // Get position table by 1-based number
        public PositionTable GetPositionTableByNumber(int tableNo)
        {
            int index = Math.Max(0, Math.Min(PositionTables.Count - 1, tableNo - 1));
            return PositionTables[index];
        }

        // Current active position table (based on current profile)
        public PositionTable CurrentPositionTable => GetPositionTableByNumber(CurrentProfile.PositionTableNo);
        #endregion

        #region Mapping Tables
        private List<MappingTable> _mappingTables;
        public List<MappingTable> MappingTables
        {
            get
            {
                if (_mappingTables == null)
                {
                    _mappingTables = new List<MappingTable>();
                    for (int i = 1; i <= 5; i++)
                    {
                        _mappingTables.Add(new MappingTable($"Mapping Table {i}"));
                    }
                }
                return _mappingTables;
            }
        }

        // Get mapping table by 1-based number
        public MappingTable GetMappingTableByNumber(int tableNo)
        {
            if (tableNo < 1 || tableNo > MappingTables.Count)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Invalid mapping table number: {tableNo}. Using table 1 instead.");
                tableNo = 1;
            }

            return MappingTables[tableNo - 1];
        }

        // Current active mapping table (based on current profile)
        public MappingTable CurrentMappingTable => GetMappingTableByNumber(CurrentProfile.MappingTableNo);
        #endregion

        #region IMappingSettings Interface Implementation
        // IMappingSettings interface implementation now delegating to position/mapping tables
        double IMappingSettings.MapStartPositionMm => CurrentPositionTable.MapStartPositionMm;
        double IMappingSettings.MapEndPositionMm => CurrentPositionTable.MapEndPositionMm;
        double IMappingSettings.MmPerPulse => MmPerPulse; // Still from hardware section
        int IMappingSettings.SensorType => CurrentMappingTable.SensorType;

        #endregion

        #region Initialization
        public void InitializeDefaults()
        {
            // General defaults - all set to 0 or basic default
            MachineId = "0";

            // Global mapping defaults - all set to 0
            SlotCount = 0;
            PositionRangeMm = 0;
            PositionRangeUpperPercent = 0;
            PositionRangeLowerPercent = 0;
            ThicknessRangeMm = 0;
            OffsetMm = 0;

            // Hardware defaults - all set to 0, I think can be remove
            MmPerPulse = 0;
            PulsePerRevolution = 0;
            ScrewLeadMm = 0;

            // System defaults
            LogLevel = 0;
            SaveMappingData = false;
            DataExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MappingData");
            AutoMapOnLoad = false;

            // Initialize position tables with default values (all 0)
            for (int i = 0; i < 5; i++)
            {
                PositionTable posTable = PositionTables[i];
                posTable.Name = $"Position Table {i + 1}";
                posTable.MapStartPositionMm = -5;
                posTable.MapEndPositionMm = -1620;
            }

            // Initialize mapping tables with default values (all 0)
            for (int i = 0; i < 5; i++)
            {
                MappingTable mapTable = MappingTables[i];
                mapTable.Name = $"Mapping Table {i + 1}";
                mapTable.SensorType = 0;
                mapTable.SlotCount = 25;
                mapTable.SlotPitchMm = 10;
                mapTable.PositionRangeMm = 2;
                mapTable.PositionRangeUpperPercent = 50;
                mapTable.PositionRangeLowerPercent = 50;
                mapTable.WaferThicknessMm = 0.775;
                mapTable.ThicknessRangeMm = 0.2;
                mapTable.OffsetMm = 0;
                mapTable.FirstSlotPositionMm = -50;
            }

            // Initialize FOUP type profiles with default values (all 0 except names and indexes)
            for (int i = 0; i < 5; i++)
            {
                MappingTypeProfile profile = MappingTypeProfiles[i];
                profile.Name = $"Type {i + 1}";
                profile.FOUPTypeIndex = 0;
                profile.PositionTableNo = i + 1; // Assign each profile its own position table
                profile.MappingTableNo = i + 1;  // Assign each profile its own mapping table

                // For backward compatibility, set the direct properties too
                profile.MapStartPositionMm = -5;
                profile.MapEndPositionMm = -1620;
                profile.MmPerPulse = 0;
                profile.SensorType = 0;
                profile.SlotPitchMm = 0;
                profile.WaferThicknessMm = 0;
            }

            // Default active type is 1
            ActiveMappingType = 1;

            // Legacy parameters for backward compatibility
            MapStartPositionMm = -5;
            MapEndPositionMm = -1620;
            SensorType = 0;
            SlotPitchMm = 0;
            WaferThicknessMm = 0;
        }
        #endregion

        #region File Operations
        public void LoadFromFile()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(BaseSettingsDirectory);

                // First initialize with defaults (all 0)
                InitializeDefaults();

                // Only try to load if file exists, otherwise keep defaults
                if (File.Exists(SettingsDirectory))
                {
                    IniDocument.Load(SettingsDirectory);

                    // Load all sections if they exist
                    LoadGeneralFromFile(IniDocument);
                    LoadMappingFromFile(IniDocument);
                    LoadHardwareFromFile(IniDocument);
                    LoadSystemFromFile(IniDocument);

                    // Load tables first so they have their values when referenced by FOUP types
                    LoadPositionTablesFromFile(IniDocument);
                    LoadMappingTablesFromFile(IniDocument);
                    LoadFOUPTypesFromFile(IniDocument);
                }
                else
                {
                    // Create the file with defaults (all 0)
                    SaveToFile();
                }

                // Always sync values for backward compatibility
                SyncValuesForBackwardCompatibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Keep using defaults if loading fails
            }
        }

        public void SaveToFile()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(BaseSettingsDirectory);

                IniDocument.ClearIni();

                // Make sure to update global settings from active mapping type before saving
                UpdateGlobalSettingsFromCurrentProfile();

                // Save all sections
                SaveGeneralToFile(IniDocument);
                SaveMappingToFile(IniDocument);
                SaveHardwareToFile(IniDocument);
                SaveSystemToFile(IniDocument);
                SavePositionTablesToFile(IniDocument);
                SaveMappingTablesToFile(IniDocument);
                SaveFOUPTypesToFile(IniDocument);

                IniDocument.Save(SettingsDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        #endregion

        #region Load & Save Helper Methods - General Sections
        private void LoadGeneralFromFile(IniDocument document)
        {
            MachineId = document.ReadString(GeneralSection, nameof(MachineId), MachineId);
        }

        private void SaveGeneralToFile(IniDocument document)
        {
            document.WriteString(GeneralSection, nameof(MachineId), MachineId);
        }

        private void LoadMappingFromFile(IniDocument document)
        {
            // Global mapping parameters
            SlotCount = document.ReadInt(MappingSection, nameof(SlotCount), SlotCount);
            PositionRangeMm = document.ReadDouble(MappingSection, nameof(PositionRangeMm), PositionRangeMm);
            PositionRangeUpperPercent = document.ReadDouble(MappingSection, nameof(PositionRangeUpperPercent), PositionRangeUpperPercent);
            PositionRangeLowerPercent = document.ReadDouble(MappingSection, nameof(PositionRangeLowerPercent), PositionRangeLowerPercent);
            ThicknessRangeMm = document.ReadDouble(MappingSection, nameof(ThicknessRangeMm), ThicknessRangeMm);
            OffsetMm = document.ReadDouble(MappingSection, nameof(OffsetMm), OffsetMm);
            ActiveMappingType = document.ReadInt(MappingSection, nameof(ActiveMappingType), ActiveMappingType);

            // Legacy parameters for backward compatibility
            MapStartPositionMm = document.ReadDouble(MappingSection, nameof(MapStartPositionMm), MapStartPositionMm);
            MapEndPositionMm = document.ReadDouble(MappingSection, nameof(MapEndPositionMm), MapEndPositionMm);
            SensorType = document.ReadInt(MappingSection, nameof(SensorType), SensorType);
            SlotPitchMm = document.ReadDouble(MappingSection, nameof(SlotPitchMm), SlotPitchMm);
            WaferThicknessMm = document.ReadDouble(MappingSection, nameof(WaferThicknessMm), WaferThicknessMm);
        }

        private void SaveMappingToFile(IniDocument document)
        {
            // Only save the ActiveMappingType in the global section
            // Other parameters will be saved in their respective mapping table sections
            document.WriteInt(MappingSection, nameof(ActiveMappingType), ActiveMappingType);

            // Save legacy parameters for backward compatibility
            document.WriteDouble(MappingSection, nameof(MapStartPositionMm), MapStartPositionMm);
            document.WriteDouble(MappingSection, nameof(MapEndPositionMm), MapEndPositionMm);
            document.WriteInt(MappingSection, nameof(SensorType), SensorType);
            document.WriteDouble(MappingSection, nameof(SlotPitchMm), SlotPitchMm);
            document.WriteDouble(MappingSection, nameof(WaferThicknessMm), WaferThicknessMm);

            // Also save the current global parameters for backward compatibility
            document.WriteInt(MappingSection, nameof(SlotCount), SlotCount);
            document.WriteDouble(MappingSection, nameof(PositionRangeMm), PositionRangeMm);
            document.WriteDouble(MappingSection, nameof(PositionRangeUpperPercent), PositionRangeUpperPercent);
            document.WriteDouble(MappingSection, nameof(PositionRangeLowerPercent), PositionRangeLowerPercent);
            document.WriteDouble(MappingSection, nameof(ThicknessRangeMm), ThicknessRangeMm);
            document.WriteDouble(MappingSection, nameof(OffsetMm), OffsetMm);
        }

        private void LoadHardwareFromFile(IniDocument document)
        {
            MmPerPulse = document.ReadDouble(HardwareSection, nameof(MmPerPulse), MmPerPulse);
            PulsePerRevolution = document.ReadDouble(HardwareSection, nameof(PulsePerRevolution), PulsePerRevolution);
            ScrewLeadMm = document.ReadDouble(HardwareSection, nameof(ScrewLeadMm), ScrewLeadMm);
        }

        private void SaveHardwareToFile(IniDocument document)
        {
            document.WriteDouble(HardwareSection, nameof(MmPerPulse), MmPerPulse);
            document.WriteDouble(HardwareSection, nameof(PulsePerRevolution), PulsePerRevolution);
            document.WriteDouble(HardwareSection, nameof(ScrewLeadMm), ScrewLeadMm);
        }

        private void LoadSystemFromFile(IniDocument document)
        {
            LogLevel = document.ReadInt(SystemSection, nameof(LogLevel), LogLevel);
            SaveMappingData = document.ReadBool(SystemSection, nameof(SaveMappingData), SaveMappingData);
            DataExportPath = document.ReadString(SystemSection, nameof(DataExportPath), DataExportPath);
            AutoMapOnLoad = document.ReadBool(SystemSection, nameof(AutoMapOnLoad), AutoMapOnLoad);
        }

        private void SaveSystemToFile(IniDocument document)
        {
            document.WriteInt(SystemSection, nameof(LogLevel), LogLevel);
            document.WriteBool(SystemSection, nameof(SaveMappingData), SaveMappingData);
            document.WriteString(SystemSection, nameof(DataExportPath), DataExportPath);
            document.WriteBool(SystemSection, nameof(AutoMapOnLoad), AutoMapOnLoad);
        }
        #endregion

        #region Load & Save Helper Methods - Position Tables
        // Helper method to get section name for a position table
        private string GetPositionTableSectionName(int tableNumber)
        {
            return string.Format(PositionTableFormat, tableNumber);
        }

        // Load all position tables
        private void LoadPositionTablesFromFile(IniDocument document)
        {
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetPositionTableSectionName(i);
                LoadPositionTableFromFile(document, sectionName, PositionTables[i - 1]);
            }
        }

        // Load a single position table
        private void LoadPositionTableFromFile(IniDocument document, string section, PositionTable table)
        {
            // Only read values if section exists
            if (document.Sections.ContainsKey(section))
            {
                table.Name = document.ReadString(section, "Name", table.Name);

                // Read values but enforce constraints
                double startPos = document.ReadDouble(section, "MapStartPositionMm", table.MapStartPositionMm);
                double endPos = document.ReadDouble(section, "MapEndPositionMm", table.MapEndPositionMm);

                // Apply constraints
                // MapStartPositionMm cannot be greater than -5
                table.MapStartPositionMm = Math.Min(startPos, -5);

                // MapEndPositionMm cannot be less than -1620
                table.MapEndPositionMm = Math.Max(endPos, -1620);
            }
        }

        // Save all position tables
        private void SavePositionTablesToFile(IniDocument document)
        {
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetPositionTableSectionName(i);
                SavePositionTableToFile(document, sectionName, PositionTables[i - 1]);
            }
        }

        // Save a single position table
        // Save a single position table
        private void SavePositionTableToFile(IniDocument document, string section, PositionTable table)
        {
            // Make sure to create a section if it doesn't exist
            if (!document.Sections.ContainsKey(section))
            {
                document.Sections[section] = new Dictionary<string, string>();
            }

            document.WriteString(section, "Name", table.Name);

            // Apply constraints before saving to ensure INI file reflects actual constrained values
            double constrainedStart = Math.Min(table.MapStartPositionMm, -5);
            double constrainedEnd = Math.Max(table.MapEndPositionMm, -1620);

            document.WriteDouble(section, "MapStartPositionMm", constrainedStart);
            document.WriteDouble(section, "MapEndPositionMm", constrainedEnd);
        }

        #endregion

        #region Load & Save Helper Methods - Mapping Tables
        // Helper method to get section name for a mapping table
        private string GetMappingTableSectionName(int tableNumber)
        {
            return string.Format(MappingTableFormat, tableNumber);
        }

        private void LoadMappingTablesFromFile(IniDocument document)
        {
            // Skip setting mapping tables from global settings since they're zeros
            // Just load individual table settings which will preserve defaults if not in INI
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetMappingTableSectionName(i);
                LoadMappingTableFromFile(document, sectionName, MappingTables[i - 1]);
            }
        }

        // Load a single mapping table
        private void LoadMappingTableFromFile(IniDocument document, string section, MappingTable table)
        {
            // Only read values if section exists
            if (document.Sections.ContainsKey(section))
            {
                // For text values, use the default if not found
                table.Name = document.ReadString(section, "Name", table.Name);

                // For numeric values, only use the INI value if it's not zero
                int sensorType = document.ReadInt(section, "SensorType", -1);
                if (sensorType >= 0) table.SensorType = sensorType;

                int slotCount = document.ReadInt(section, "SlotCount", -1);
                if (slotCount > 0) table.SlotCount = slotCount;

                double slotPitch = document.ReadDouble(section, "SlotPitchMm", -1);
                if (slotPitch > 0) table.SlotPitchMm = slotPitch;

                double positionRange = document.ReadDouble(section, "PositionRangeMm", -1);
                if (positionRange > 0) table.PositionRangeMm = positionRange;

                double posUpperPercent = document.ReadDouble(section, "PositionRangeUpperPercent", -1);
                if (posUpperPercent > 0) table.PositionRangeUpperPercent = posUpperPercent;

                double posLowerPercent = document.ReadDouble(section, "PositionRangeLowerPercent", -1);
                if (posLowerPercent > 0) table.PositionRangeLowerPercent = posLowerPercent;

                double waferThickness = document.ReadDouble(section, "WaferThicknessMm", -1);
                if (waferThickness > 0) table.WaferThicknessMm = waferThickness;

                double thicknessRange = document.ReadDouble(section, "ThicknessRangeMm", -1);
                if (thicknessRange > 0) table.ThicknessRangeMm = thicknessRange;

                // Offset can legitimately be 0, so always read it
                table.OffsetMm = document.ReadDouble(section, "OffsetMm", table.OffsetMm);

                // FirstSlotPositionMm can be negative, so always read it
                table.FirstSlotPositionMm = document.ReadDouble(section, "FirstSlotPositionMm", table.FirstSlotPositionMm);
            }
        }

        // Save all mapping tables
        private void SaveMappingTablesToFile(IniDocument document)
        {
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetMappingTableSectionName(i);
                SaveMappingTableToFile(document, sectionName, MappingTables[i - 1]);
            }
        }

        // Save a single mapping table
        private void SaveMappingTableToFile(IniDocument document, string section, MappingTable table)
        {
            // Make sure to create a section if it doesn't exist
            if (!document.Sections.ContainsKey(section))
            {
                document.Sections[section] = new Dictionary<string, string>();
            }

            // Save ALL mapping table properties
            document.WriteString(section, "Name", table.Name);
            document.WriteInt(section, "SensorType", table.SensorType);
            document.WriteInt(section, "SlotCount", table.SlotCount);
            document.WriteDouble(section, "SlotPitchMm", table.SlotPitchMm);
            document.WriteDouble(section, "PositionRangeMm", table.PositionRangeMm);
            document.WriteDouble(section, "PositionRangeUpperPercent", table.PositionRangeUpperPercent);
            document.WriteDouble(section, "PositionRangeLowerPercent", table.PositionRangeLowerPercent);
            document.WriteDouble(section, "WaferThicknessMm", table.WaferThicknessMm);
            document.WriteDouble(section, "ThicknessRangeMm", table.ThicknessRangeMm);
            document.WriteDouble(section, "OffsetMm", table.OffsetMm);
            document.WriteDouble(section, "FirstSlotPositionMm", table.FirstSlotPositionMm); // Save new parameter
        }
        #endregion

        #region Load & Save Helper Methods - FOUP Types
        // Helper method to get section name for a FOUP type profile
        private string GetFOUPTypeSectionName(int typeNumber)
        {
            return string.Format(FOUPTypeFormat, typeNumber);
        }

        // Load FOUP types
        private void LoadFOUPTypesFromFile(IniDocument document)
        {
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetFOUPTypeSectionName(i);
                LoadFOUPTypeFromFile(document, sectionName, MappingTypeProfiles[i - 1]);
            }
        }

        // Load a single FOUP type profile
        private void LoadFOUPTypeFromFile(IniDocument document, string section, MappingTypeProfile profile)
        {
            // Only read values if section exists
            if (document.Sections.ContainsKey(section))
            {
                profile.Name = document.ReadString(section, "Name", profile.Name);
                profile.FOUPTypeIndex = document.ReadInt(section, "FOUPTypeIndex", profile.FOUPTypeIndex);
                profile.PositionTableNo = document.ReadInt(section, "PositionTableNo", profile.PositionTableNo);
                profile.MappingTableNo = document.ReadInt(section, "MappingTableNo", profile.MappingTableNo);
            }
        }

        // Save FOUP types
        private void SaveFOUPTypesToFile(IniDocument document)
        {
            for (int i = 1; i <= 5; i++)
            {
                string sectionName = GetFOUPTypeSectionName(i);
                SaveFOUPTypeToFile(document, sectionName, MappingTypeProfiles[i - 1]);
            }
        }

        // Save a single FOUP type profile
        private void SaveFOUPTypeToFile(IniDocument document, string section, MappingTypeProfile profile)
        {
            // Make sure to create a section if it doesn't exist
            if (!document.Sections.ContainsKey(section))
            {
                document.Sections[section] = new Dictionary<string, string>();
            }

            document.WriteString(section, "Name", profile.Name);
            document.WriteInt(section, "FOUPTypeIndex", profile.FOUPTypeIndex);
            document.WriteInt(section, "PositionTableNo", profile.PositionTableNo);
            document.WriteInt(section, "MappingTableNo", profile.MappingTableNo);
        }
        #endregion

        #region Migration and Compatibility
        // Update global settings from current mapping profile
        // This ensures the legacy global settings match the current active profile
        private void UpdateGlobalSettingsFromCurrentProfile()
        {
            var currentMapTable = CurrentMappingTable;

            // Update global settings from current mapping table
            SlotCount = currentMapTable.SlotCount;
            PositionRangeMm = currentMapTable.PositionRangeMm;
            PositionRangeUpperPercent = currentMapTable.PositionRangeUpperPercent;
            PositionRangeLowerPercent = currentMapTable.PositionRangeLowerPercent;
            ThicknessRangeMm = currentMapTable.ThicknessRangeMm;
            OffsetMm = currentMapTable.OffsetMm;

            // Update legacy properties
            var currentPosTable = CurrentPositionTable;
            MapStartPositionMm = currentPosTable.MapStartPositionMm;
            MapEndPositionMm = currentPosTable.MapEndPositionMm;
            SensorType = currentMapTable.SensorType;
            SlotPitchMm = currentMapTable.SlotPitchMm;
            WaferThicknessMm = currentMapTable.WaferThicknessMm;
        }

        private void SyncValuesForBackwardCompatibility()
        {
            // For each profile, sync the direct properties from the referenced tables
            foreach (var profile in MappingTypeProfiles)
            {
                // Get the referenced tables
                var posTable = GetPositionTableByNumber(profile.PositionTableNo);
                var mapTable = GetMappingTableByNumber(profile.MappingTableNo);

                // Update the profile's direct properties for backward compatibility
                profile.MapStartPositionMm = posTable.MapStartPositionMm;
                profile.MapEndPositionMm = posTable.MapEndPositionMm;
                profile.MmPerPulse = MmPerPulse;
                profile.SensorType = mapTable.SensorType;
                profile.SlotPitchMm = mapTable.SlotPitchMm;
                profile.WaferThicknessMm = mapTable.WaferThicknessMm;
            }

            // Update global settings from current profile
            UpdateGlobalSettingsFromCurrentProfile();
        }
        #endregion

        #region Debugging & Diagnostics
        // Add a method to dump all settings to console for diagnostics
        public void DumpSettings()
        {
            System.Diagnostics.Debug.WriteLine("======= CURRENT SETTINGS DUMP =======");
            System.Diagnostics.Debug.WriteLine($"Active Mapping Type: {ActiveMappingType} ({CurrentProfile.Name})");

            System.Diagnostics.Debug.WriteLine("\n--- Global Parameters ---");
            System.Diagnostics.Debug.WriteLine($"SlotCount: {SlotCount}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeMm: {PositionRangeMm}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeUpperPercent: {PositionRangeUpperPercent}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeLowerPercent: {PositionRangeLowerPercent}");
            System.Diagnostics.Debug.WriteLine($"ThicknessRangeMm: {ThicknessRangeMm}");
            System.Diagnostics.Debug.WriteLine($"OffsetMm: {OffsetMm}");

            System.Diagnostics.Debug.WriteLine("\n--- Current Position Table ---");
            System.Diagnostics.Debug.WriteLine($"Table #{CurrentProfile.PositionTableNo}: {CurrentPositionTable.Name}");
            System.Diagnostics.Debug.WriteLine($"MapStartPositionMm: {CurrentPositionTable.MapStartPositionMm}");
            System.Diagnostics.Debug.WriteLine($"MapEndPositionMm: {CurrentPositionTable.MapEndPositionMm}");

            System.Diagnostics.Debug.WriteLine("\n--- Current Mapping Table ---");
            System.Diagnostics.Debug.WriteLine($"Table #{CurrentProfile.MappingTableNo}: {CurrentMappingTable.Name}");
            System.Diagnostics.Debug.WriteLine($"SensorType: {CurrentMappingTable.SensorType}");
            System.Diagnostics.Debug.WriteLine($"SlotCount: {CurrentMappingTable.SlotCount}");
            System.Diagnostics.Debug.WriteLine($"SlotPitchMm: {CurrentMappingTable.SlotPitchMm}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeMm: {CurrentMappingTable.PositionRangeMm}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeUpperPercent: {CurrentMappingTable.PositionRangeUpperPercent}");
            System.Diagnostics.Debug.WriteLine($"PositionRangeLowerPercent: {CurrentMappingTable.PositionRangeLowerPercent}");
            System.Diagnostics.Debug.WriteLine($"WaferThicknessMm: {CurrentMappingTable.WaferThicknessMm}");
            System.Diagnostics.Debug.WriteLine($"ThicknessRangeMm: {CurrentMappingTable.ThicknessRangeMm}");
            System.Diagnostics.Debug.WriteLine($"OffsetMm: {CurrentMappingTable.OffsetMm}");
            System.Diagnostics.Debug.WriteLine($"FirstSlotPositionMm: {CurrentMappingTable.FirstSlotPositionMm}"); // Add to debug output

            System.Diagnostics.Debug.WriteLine("\n=================================");
        }
        #endregion

        public void LoadCRC16Settings(IniDocument document)
        {
            EnableCRC16Protocol = document.ReadBool("CRC16", "EnableProtocol", EnableCRC16Protocol);
            ProtocolStationCode = document.ReadByte("CRC16", "StationCode", ProtocolStationCode);
            ProtocolAddress = document.ReadByte("CRC16", "Address", ProtocolAddress);
            EnableProtocolLogging = document.ReadBool("CRC16", "EnableLogging", EnableProtocolLogging);
        }

        public void SaveCRC16Settings(IniDocument document)
        {
            document.WriteBool("CRC16", "EnableProtocol", EnableCRC16Protocol);
            document.WriteByte("CRC16", "StationCode", ProtocolStationCode);
            document.WriteByte("CRC16", "Address", ProtocolAddress);
            document.WriteBool("CRC16", "EnableLogging", EnableProtocolLogging);
        }
    }
}
