using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FOUPCtrl
{
    public static class FOUPInfo
    {
        public const string sMachineVersion = "1.0";
        public const string ErrorExist = "05";
        public const string InterlockExist = "04";
    }

    // Global Constants for error handling
    public static class GlobalConstants
    {
        public const string FOUP_NO_ERROR = "No Error";
    }

    //Error list
    //GET:STAS[4,5]
    public struct ErrorCode
    {
        public const string Error_None = "00";

        // 1x series - Operation timeout errors
        public const string Error_Clamp_Timeover = "10";
        public const string Error_Unclamp_Timeover = "11";
        public const string Error_Dock_Timeover = "12";
        public const string Error_Undock_Timeover = "13";
        public const string Error_Latch_Timeover = "14";
        public const string Error_Unlatch_Timeover = "15";
        public const string Error_Vacuum_Timeover = "16";
        public const string Error_VacuumRelease_Timeover = "17";
        public const string Error_DoorOpen_Timeover = "18";
        public const string Error_DoorClose_Timeover = "19";
        public const string Error_MappingForward_Timeover = "1A";
        public const string Error_MappingReturn_Timeover = "1B";
        public const string Error_Communication = "1F";

        // 2x series - Movement timeout errors
        public const string Error_HomeReturn_Timeover = "20";
        public const string Error_Loading_Timeover = "21";
        public const string Error_Unloading_Timeover = "22";
        public const string Error_Positioning_Timeover = "23";
        public const string Error_Mapping = "24";
        public const string Error_Elevator_Timeover = "25";
        public const string Error_Door_Timeover = "26";
        public const string Error_Mapping_Timeover = "27"; 
        public const string Error_DoorMovement_Timeover = "28";
        public const string Error_MappingStart_Timeover = "29";
        public const string Error_MappingEnd_Timeover = "2A";
        public const string Error_LoadPosition_Timeover = "2B";

        // 4x series - Data and mode errors
        public const string Error_MappingData = "40";
        public const string Error_ModeSelect = "41";
        public const string Error_MappingCalibration1 = "42";
        public const string Error_MappingCalibration2 = "43";
        public const string Error_MappingWithoutCassette = "44";

        // 7x series - Sensor errors
        public const string Error_Clamp_Sensor = "70";
        public const string Error_Dock_Sensor = "71";
        public const string Error_Latch_Sensor = "72";
        public const string Error_Door_Sensor = "73";
        public const string Error_Mapping_Sensor = "74";
        public const string Error_ProtrusionSensor = "75";
        public const string Error_MappingSensor = "76";
        public const string Error_ElevatorAxis_Sensor = "77";

        // Ax series - Wafer and mounting errors
        public const string Error_WaferDrop = "A0";
        public const string Error_WaferProtruded = "A1";
        public const string Error_FOUPMount_Sensor = "A2";
        public const string Error_FOUPMount_Load = "A3";
        public const string Error_PresenceSensor = "A2";
        public const string Error_AirPressure = "A5";

        // Bx series - Host errors
        public const string Error_Host = "B0";

        // Cx series - Parameter errors
        public const string Error_Parameter = "C0";

        // Ex series - System errors
        public const string Error_FANalarm = "E0";

        public const string Error_Encoder = "E2";
        public const string Error_ServoAlarm = "E3";
        public const string Error_VoltageDrop = "E3";
        public const string Error_Overrun = "E4";

        // Fx series - Hardware errors
        public const string Error_DockHandPinch = "FE";
    }

    //Interlock list
    //When command was received, response 00 if no interlock,
    //else response 04, with interlock code after backslash '\' 
    public struct Interlock
    {
        public static string NoPod = "10";
        public static string NotHomePosition = "12"; //unclamp, latch, z-home
        public static string LoadingNotCompleted = "13"; //clamp, unlatch,z-home, then z-axis only can move
        public static string PodMounting = "1F";
        public static string NotUnlatched = "40";
        public static string ZNotHome = "43";
        public static string WaferProtruded = "50";
    }

    //GET:STAS[0]
    public enum MachineStatus
    {
        Normal = '0',
        RecoverableError = 'A',
        UnrecoverableError = 'E'
    }

    //GET:STAS[1]
    public enum MachineMode
    {
        Online = '0',
        Maintenance = '2'
    }

    //GET:STAS[2]
    public enum LoadStatus
    {
        InOperation = '0',
        HomePosition = '1',
        LoadPosition = '2',
        Indefinite = '?'
    }

    //GET:STAS[3]
    public enum Operation
    {
        Stopping = '0',
        Operating = '1'
    }

    //GET:STAS[6] - Updated to match usage in FOUP_Ctrl.cs
    public enum ContainerStatus
    {
        None = '0',
        Normal_mounting = '1',
        Abnormal_mounting = '2',
        Indefinite = '?'
    }

    //GET:STAS[7]
    public enum ClampStatus
    {
        Open = '0',
        Close = '1',
        Indefinite = '?'
    }

    //GET:STAS[8]
    public enum LatchStatus
    {
        Open = '0',
        Close = '1',
        Indefinite = '?'
    }

    //GET:STAS[9] - Vacuum Status
    public enum VacuumStatus
    {
        Off = '0',
        On = '1'
    }

    //GET:STAS[10] - Door Position
    public enum DoorPosition
    {
        Open = '0',
        Close = '1',
        Indefinite = '?'
    }

    //GET:STAS[11] - Wafer Protrusion Sensor
    public enum WaferProtrusionSensor
    {
        Protrude = '0',
        No_protrude = '1'
    }

    //GET:STAS[12] - Z-Axis (Elevator) Position
    public enum ZAxisPosition
    {
        Up_position = '0',
        Down_position = '1',
        Mapping_start = '2',
        Mapping_end = '3',
        Indefinite = '?'
    }

    //GET:STAS[13] - Dock Position
    public enum DockPosition
    {
        Undock = '0',
        Dock = '1',
        Indefinite = '?'
    }

    //GET:STAS[15] - Mapping Position
    public enum MappingPosition
    {
        Waiting_position = '0',
        Measuring_position = '1',
        Indefinite = '?'
    }

    //GET:STAS[17] - Mapping Status
    public enum MappingStatus
    {
        Inexecution = '0',
        Normal_end = '1',
        Abnormal_end = '2',
        InProcess = '3',
        Completed = '4'
    }

    //GET:STAS[18] - Pod Type
    public enum PodType
    {
        Type1 = '0',
        Type2 = '1',
        Type3 = '2',
        Type4 = '3',
        Type5 = '4'
    }

    // Legacy enums for backward compatibility
    public enum PodExist
    {
        NoPod = '0',
        PodMounted = '1'
    }

    enum WaferProtrusion
    {
        Shading = '0',
        Lighting = '1'
    }

    // NEW: Cassette Placement Status enum
    public enum CassettePlacementStatus
    {
        No_Cassette = '0',           // No presence sensors active
        Properly_Placed = '1',       // Presence sensors 1,2,3 active, diagonal sensors inactive
        Improper_Placement = '2',    // Any diagonal sensor active (red condition)
        Partial_Detection = '3',     // Only some presence sensors active
        Indefinite = '?'            // Cannot determine status
    }
    public enum CassettePresenceStatus
    {
        None = '0',           // No cassette detected
        Present = '1',        // Cassette detected
        Indefinite = '?'      // Cannot determine
    }
}