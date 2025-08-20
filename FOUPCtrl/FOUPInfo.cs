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

    //Error list
    //GET:STAS[4,5]
    public struct ErrorCode
    {
        public const string Error_None = "00";

        public const string Error_Clamp_Timeover = "10";
        public const string Error_Unclamp_Timeover = "11";
        public const string Error_Latch_Timeover = "14";
        public const string Error_Unlatch_Timeover = "15";

        // New error codes for elevator, door, dock, mapping and vacuum
        public const string Error_Elevator_Timeover = "18";
        public const string Error_Door_Timeover = "19";
        public const string Error_Dock_Timeover = "1A";
        public const string Error_Mapping_Timeover = "1B";
        public const string Error_Vacuum_Timeover = "1C";

        public const string Error_Positioning_Timeover = "23";
        public const string Error_Mapping = "24";

        public const string Error_MappingData = "40";
        public const string Error_ModeSelect = "41";
        public const string Error_MappingCalibration1 = "42"; //No Wafer at last slot during calibration
        public const string Error_MappingCalibration2 = "43"; //No Wafer at first slot during calibration 
        public const string Error_MappingWithoutCassette = "44";

        public const string Error_Clamp_Sensor = "70"; //Clamp and Unclamp both ON at the same time
        public const string Error_Latch_Sensor = "72"; //Latch and Unlatch both ON at the same time
        public const string Error_ProtrusionSensor = "75"; //Protrusion sensor not ON when light blocked by cassette (at base and top)
        public const string Error_MappingSensor = "76"; //Mapping sensor not ON when light blocked by cassette (at base and top)

        public const string Error_WaferProtruded = "A1";
        public const string Error_PresenceSensor = "A2";

        public const string Error_Host = "B0";
        public const string Error_Parameter = "C0";

        public const string Error_FANalarm = "E0";

        public const string Error_Encoder = "E2";
        public const string Error_ServoAlarm = "E3";
        public const string Error_Overrun = "E4";
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
    enum MachineMode
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

    //GET:STAS[4,5]

    //GET:STAS[6]
    public enum PodExist
    {
        NoPod = '0',
        PodMounted = '1'
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

    //GET:STAS[11]
    enum WaferProtrusion
    {
        Shading = '0',
        Lighting = '1'
    }

    //GET:STAS[12]
    public enum ZAxisPosition
    {
        UpPosition = '0',
        DownPosition = '1',
        MapStart = '2',
        MapEnd = '3',
        Indefinite = '?'
    }

    //GET:STAS[17]
    public enum MappingStatus
    {
        Inexecution = '0',
        NormalEnd = '1',
        AbnormalEnd = '2',
        // Added new mapping status values
        InProcess = '3',
        Completed = '4'
    }

    //GET:STAS[18]
    public enum PodType
    {
        Type1 = '0',
        Type2 = '1',
        Type3 = '2',
        Type4 = '3',
        Type5 = '4'
    }
}