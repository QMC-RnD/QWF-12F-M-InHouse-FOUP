using System;
using System.IO;
using FOUPCtrl.Utilities;

namespace FOUPCtrl.Models
{
    public sealed class OEM
    {
        public const string GeneralSection = "General";

        static OEM() { }

        private OEM() { }

        public static OEM Instance { get; private set; } = new OEM();

        public IniDocument IniDocument { get; set; } = new IniDocument();

        public bool IsGeneralLoaded { get; private set; }

        // Example OEM parameters
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public double SensorCalibrationFactor { get; set; }
        public double ZAxisOffset { get; set; }

        public static void ResetInstance()
        {
            Instance = new OEM();
        }

        public void LoadFromFile()
        {
            IniDocument.Load(Settings.OEMDirectoryLocal);
            LoadGeneralFromFile(IniDocument);
        }

        public void SaveToFile()
        {
            SaveOEM(Settings.OEMDirectory);
            SaveOEM(Settings.OEMDirectoryLocal);

            void SaveOEM(string path)
            {
                FileInfo file = new FileInfo(path);
                file.Directory.Create();

                IniDocument.ClearIni();
                SaveGeneralToFile(IniDocument);
                IniDocument.Save(path);
            }
        }

        private void LoadGeneralFromFile(IniDocument document)
        {
            IsGeneralLoaded = false;
            Manufacturer = document.ReadString(GeneralSection, nameof(Manufacturer), "Generic");
            ModelNumber = document.ReadString(GeneralSection, nameof(ModelNumber), "WM-1000");
            SerialNumber = document.ReadString(GeneralSection, nameof(SerialNumber), "000000");
            FirmwareVersion = document.ReadString(GeneralSection, nameof(FirmwareVersion), "1.0.0");
            SensorCalibrationFactor = document.ReadDouble(GeneralSection, nameof(SensorCalibrationFactor), 1.0);
            ZAxisOffset = document.ReadDouble(GeneralSection, nameof(ZAxisOffset), 0.0);
            IsGeneralLoaded = true;
        }

        private void SaveGeneralToFile(IniDocument document)
        {
            document.WriteString(GeneralSection, nameof(Manufacturer), Manufacturer);
            document.WriteString(GeneralSection, nameof(ModelNumber), ModelNumber);
            document.WriteString(GeneralSection, nameof(SerialNumber), SerialNumber);
            document.WriteString(GeneralSection, nameof(FirmwareVersion), FirmwareVersion);
            document.WriteDouble(GeneralSection, nameof(SensorCalibrationFactor), SensorCalibrationFactor);
            document.WriteDouble(GeneralSection, nameof(ZAxisOffset), ZAxisOffset);
        }
    }
}