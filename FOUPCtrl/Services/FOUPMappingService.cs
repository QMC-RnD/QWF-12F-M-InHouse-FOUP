using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FOUPCtrl.Models;
using FoupControl;

namespace FOUPCtrl.Services
{
    public class FOUPMappingService
    {
        private readonly FOUP_Ctrl _foupCtrl;
        private readonly FOUPTrainingService _trainingService;

        public FOUPMappingService(FOUP_Ctrl foupCtrl, FOUPTrainingService trainingService)
        {
            _foupCtrl = foupCtrl ?? throw new ArgumentNullException(nameof(foupCtrl));
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        }

        public async Task<MappingCalibrationResult> ExecuteMappingAutoCalibration(CancellationToken token, IMappingSettings settings, Action<string> callbackAction = null)
        {
            try
            {
                callbackAction?.Invoke("Starting Auto Calibration...");

                var result = await _foupCtrl.MappingAutoCalibration(token, settings, callbackAction);

                if (result.Success)
                {
                    // Update the current mapping table with the calculated values
                    var currentMapTable = Settings.Instance.CurrentMappingTable;
                    if (currentMapTable != null)
                    {
                        currentMapTable.SlotPitchMm = Math.Abs(result.AvgPitch);
                        currentMapTable.FirstSlotPositionMm = result.Slot1Pos;

                        if (currentMapTable.WaferThicknessMm <= 0)
                            currentMapTable.WaferThicknessMm = result.AvgThickness;

                        Settings.Instance.SaveToFile();
                    }

                    return new MappingCalibrationResult
                    {
                        Success = true,
                        WaferCount = result.WaferCount,
                        Slot1Position = result.Slot1Pos,
                        AvgPitch = result.AvgPitch,
                        AvgThickness = result.AvgThickness
                    };
                }
                else
                {
                    return new MappingCalibrationResult { Success = false, ErrorMessage = "Auto calibration failed" };
                }
            }
            catch (Exception ex)
            {
                return new MappingCalibrationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public MappingAnalysisResult PerformOfflineMappingAnalysis(string filePath, TrainingParameters trainedParams, int mappingType)
        {
            try
            {
                if (!VerifyMappingPrerequisites(trainedParams))
                {
                    return new MappingAnalysisResult { Success = false, ErrorMessage = "Training data not available" };
                }

                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    return new MappingAnalysisResult { Success = false, ErrorMessage = "CSV file has insufficient data" };
                }

                var parsedData = _trainingService.ParseCsvData(lines);
                if (parsedData.Count < 2)
                {
                    return new MappingAnalysisResult { Success = false, ErrorMessage = "Insufficient valid data points" };
                }

                // Get the current mapping type/profile settings
                var currentProfile = Settings.Instance.CurrentProfile;
                var currentMappingTable = Settings.Instance.CurrentMappingTable;

                // Extract parameters from the current type settings
                string typeName = currentProfile.Name;
                double typeSlotPitchMm = currentMappingTable.SlotPitchMm;
                double typePositionToleranceMm = currentMappingTable.PositionRangeMm;
                double typeExpectedThicknessMm = currentMappingTable.WaferThicknessMm;
                double typeThicknessToleranceMm = currentMappingTable.ThicknessRangeMm;
                int typeSlotCount = currentMappingTable.SlotCount > 0 ? currentMappingTable.SlotCount : 25;

                // Perform Mapping Analysis using the type parameters
                var analysisResult = FOUPCtrl.WaferMap.PerformMappingAnalysisWithTypeParameters(
                    parsedData,
                    trainedParams.CalibratedSlot1PosMm,
                    trainedParams.CalibratedAvgPitchMm,
                    typeSlotCount,
                    typeSlotPitchMm,
                    typePositionToleranceMm,
                    typeExpectedThicknessMm,
                    typeThicknessToleranceMm,
                    typeSlotCount,
                    typeName,
                    (msg) => System.Diagnostics.Debug.WriteLine(msg));

                return new MappingAnalysisResult
                {
                    Success = true,
                    WaferMapAnalysisResult = analysisResult
                };
            }
            catch (Exception ex)
            {
                return new MappingAnalysisResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public bool VerifyMappingPrerequisites(TrainingParameters trainedParams)
        {
            return trainedParams != null &&
                   !double.IsNaN(trainedParams.CalibratedAvgPitchMm) &&
                   !double.IsNaN(trainedParams.CalibratedSlot1PosMm);
        }

        public async Task<FOUPCtrl.WaferMap.MappingAnalysisResult> ExecuteTopToBottomMapping(CancellationToken token, IMappingSettings settings)
        {
            try
            {
                await _foupCtrl.MappingOperation_UpToDown(token, settings);
                return _foupCtrl.GetLastMappingAnalysisResult();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class MappingCalibrationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int WaferCount { get; set; }
        public double Slot1Position { get; set; }
        public double AvgPitch { get; set; }
        public double AvgThickness { get; set; }
    }

    public class MappingAnalysisResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public FOUPCtrl.WaferMap.MappingAnalysisResult WaferMapAnalysisResult { get; set; }
    }

    public class TrainingParameters
    {
        public double CalibratedSlot1PosMm { get; set; }
        public double CalibratedAvgPitchMm { get; set; }
        public bool IsMapTrained { get; set; }
    }
}