using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FOUPCtrl.Models;
using FoupControl;

namespace FOUPCtrl.Services
{
    public class FOUPTrainingService
    {
        private readonly FOUP_Ctrl _foupCtrl;
        private const int EXPECTED_SLOTS = 25;

        // Training state
        private bool _isMapTrained = false;
        private double _calibratedSlot1PosMm = double.NaN;
        private double _calibratedAvgPitchMm = double.NaN;
        private int[] _trainedSlotRefPulses = null;
        private int[] _trainedBoundariesPulses = null;

        public FOUPTrainingService(FOUP_Ctrl foupCtrl)
        {
            _foupCtrl = foupCtrl ?? throw new ArgumentNullException(nameof(foupCtrl));
        }

        public bool IsMapTrained => _isMapTrained;
        public double CalibratedSlot1PosMm => _calibratedSlot1PosMm;
        public double CalibratedAvgPitchMm => _calibratedAvgPitchMm;
        public int[] TrainedSlotRefPulses => _trainedSlotRefPulses;
        public int[] TrainedBoundariesPulses => _trainedBoundariesPulses;

        public TrainingResult TrainFromCsv(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return new TrainingResult { Success = false, ErrorMessage = "File does not exist" };
                }

                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    return new TrainingResult { Success = false, ErrorMessage = "CSV file has insufficient data" };
                }

                var parsedData = ParseCsvData(lines);
                var trainingResult = ProcessTrainingData(parsedData);

                return trainingResult;
            }
            catch (Exception ex)
            {
                return new TrainingResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<TrainingResult> TrainFromLiveData(IMappingSettings settings, CancellationToken token)
        {
            try
            {
                await _foupCtrl.MappingOperation_UpToDown(token, settings);
                var liveData = _foupCtrl.GetMappingData();

                if (liveData == null || liveData.Count < 2)
                {
                    return new TrainingResult { Success = false, ErrorMessage = "Insufficient live data collected" };
                }

                return ProcessTrainingData(liveData);
            }
            catch (Exception ex)
            {
                return new TrainingResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<TrainingResult> TrainFromLiveDataHighSpeed(IMappingSettings settings, CancellationToken token)
        {
            try
            {
                var totalStartTime = System.Diagnostics.Stopwatch.StartNew();
                await _foupCtrl.MappingOperation_UpToDown_HighSpeed(token, settings);
                totalStartTime.Stop();

                var liveData = _foupCtrl.GetMappingData();

                if (liveData == null || liveData.Count < 2)
                {
                    return new TrainingResult { Success = false, ErrorMessage = "Insufficient live data collected" };
                }

                // Export high-speed data
                await ExportHighSpeedData(liveData, totalStartTime.ElapsedMilliseconds);

                return ProcessTrainingData(liveData);
            }
            catch (Exception ex)
            {
                return new TrainingResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public List<FOUP_Ctrl.DataPoint> ParseCsvData(string[] lines)
        {
            var parsedData = new List<FOUP_Ctrl.DataPoint>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');
                if (values.Length >= 3)
                {
                    if (long.TryParse(values[0], out long timeMs) &&
                        double.TryParse(values[1], out double positionMm) &&
                        int.TryParse(values[2], out int sensorValue))
                    {
                        parsedData.Add(new FOUP_Ctrl.DataPoint
                        {
                            TimeMs = timeMs,
                            Position = positionMm,
                            SensorValue = sensorValue
                        });
                    }
                }
            }

            return parsedData;
        }

        public TrainingResult ProcessTrainingData(List<FOUP_Ctrl.DataPoint> mappingData)
        {
            // Reset previous training results
            _calibratedSlot1PosMm = double.NaN;
            _calibratedAvgPitchMm = double.NaN;
            _isMapTrained = false;
            _trainedSlotRefPulses = null;
            _trainedBoundariesPulses = null;

            if (mappingData == null || mappingData.Count < 2)
            {
                return new TrainingResult { Success = false, ErrorMessage = "Insufficient data for training" };
            }

            var detectedWafersMm = FindWaferEdgesMm(mappingData);
            if (detectedWafersMm.Count < 2)
            {
                return new TrainingResult { Success = false, ErrorMessage = $"Detected only {detectedWafersMm.Count} wafer(s). Need at least 2 for training" };
            }

            // Sort and calculate wafer centers
            detectedWafersMm = detectedWafersMm.OrderBy(w => Math.Abs(w.startPosMm)).ToList();
            var waferCenters = detectedWafersMm.Select(w => (w.startPosMm + w.endPosMm) / 2.0).ToList();

            bool isNegativeCoordinateSystem = waferCenters.Any() && waferCenters[0] < 0;
            if (isNegativeCoordinateSystem)
            {
                waferCenters = waferCenters.OrderByDescending(c => c).ToList();
            }
            else
            {
                waferCenters = waferCenters.OrderBy(c => c).ToList();
            }

            double firstWaferCenterMm = waferCenters.First();
            double lastWaferCenterMm = waferCenters.Last();
            double distance = lastWaferCenterMm - firstWaferCenterMm;
            int numberOfGaps = EXPECTED_SLOTS - 1;

            if (numberOfGaps <= 0)
            {
                return new TrainingResult { Success = false, ErrorMessage = "Invalid slot count" };
            }

            _calibratedAvgPitchMm = distance / numberOfGaps;
            _calibratedSlot1PosMm = firstWaferCenterMm;

            const double MM_PER_PULSE = 0.18;
            int avgPitchPulsesSigned = (int)Math.Round(_calibratedAvgPitchMm / MM_PER_PULSE);
            int firstSlotRefPulses = (int)Math.Round(_calibratedSlot1PosMm / MM_PER_PULSE);

            // Calculate slot references and boundaries
            _trainedSlotRefPulses = new int[EXPECTED_SLOTS];
            _trainedBoundariesPulses = new int[EXPECTED_SLOTS + 1];

            for (int i = 0; i < EXPECTED_SLOTS; i++)
            {
                _trainedSlotRefPulses[i] = firstSlotRefPulses + (i * avgPitchPulsesSigned);
            }

            int halfPitch = Math.Abs(avgPitchPulsesSigned) / 2;
            _trainedBoundariesPulses[0] = _trainedSlotRefPulses[0] - (isNegativeCoordinateSystem ? -halfPitch : halfPitch);

            for (int i = 1; i < EXPECTED_SLOTS; i++)
            {
                _trainedBoundariesPulses[i] = (_trainedSlotRefPulses[i - 1] + _trainedSlotRefPulses[i]) / 2;
            }

            _trainedBoundariesPulses[EXPECTED_SLOTS] = _trainedSlotRefPulses[EXPECTED_SLOTS - 1] +
                (isNegativeCoordinateSystem ? -halfPitch : halfPitch);

            _isMapTrained = true;

            return new TrainingResult
            {
                Success = true,
                FirstSlotRefPulses = firstSlotRefPulses,
                AvgPitchPulses = Math.Abs(avgPitchPulsesSigned),
                SlotRefPulses = _trainedSlotRefPulses?.ToArray(),
                BoundaryPulses = _trainedBoundariesPulses?.ToArray(),
                DetectedWaferCount = detectedWafersMm.Count
            };
        }

        public List<(double startPosMm, double endPosMm)> FindWaferEdgesMm(List<FOUP_Ctrl.DataPoint> data)
        {
            var edges = new List<(double startPosMm, double endPosMm)>();
            double startEdgeMm = double.NaN;

            if (data == null || data.Count < 2) return edges;

            for (int i = 1; i < data.Count; i++)
            {
                // Rising edge (0 -> 1)
                if (data[i - 1].SensorValue == 0 && data[i].SensorValue == 1)
                {
                    startEdgeMm = data[i].Position;
                }
                // Falling edge (1 -> 0)
                else if (data[i - 1].SensorValue == 1 && data[i].SensorValue == 0)
                {
                    if (!double.IsNaN(startEdgeMm))
                    {
                        double endEdgeMm = data[i - 1].Position;
                        double bottom = Math.Min(startEdgeMm, endEdgeMm);
                        double top = Math.Max(startEdgeMm, endEdgeMm);

                        if (Math.Abs(top - bottom) > 0.1)
                        {
                            edges.Add((bottom, top));
                        }

                        startEdgeMm = double.NaN;
                    }
                }
            }

            return edges;
        }

        private async Task ExportHighSpeedData(List<FOUP_Ctrl.DataPoint> liveData, long totalScanTimeMs)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string exportDir = Path.Combine(documentsPath, "FOUP_HighSpeed_Training_Data");

                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                double dataPointsPerSecond = liveData.Count / (totalScanTimeMs / 1000.0);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"HighSpeed_Training_{timestamp}_{liveData.Count}points_{dataPointsPerSecond:F0}pps.csv";
                string fullPath = Path.Combine(exportDir, filename);

                using (var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8, 65536))
                {
                    writer.WriteLine($"# HIGH-SPEED TRAINING DATA EXPORT");
                    writer.WriteLine($"# Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"# Total Scan Time: {totalScanTimeMs:F1} ms");
                    writer.WriteLine($"# Data Points: {liveData.Count}");
                    writer.WriteLine($"# Collection Rate: {dataPointsPerSecond:F1} points/second");
                    writer.WriteLine($"#");
                    writer.WriteLine("Time (ms),Position (mm),Sensor Value,Data Point Index,Interval (ms)");

                    for (int i = 0; i < liveData.Count; i++)
                    {
                        var point = liveData[i];
                        double intervalMs = 0;
                        if (i > 0)
                        {
                            intervalMs = point.TimeMs - liveData[i - 1].TimeMs;
                        }

                        writer.WriteLine($"{point.TimeMs},{point.Position:F3},{point.SensorValue},{i + 1},{intervalMs:F3}");
                    }
                }
            }
            catch (Exception)
            {
                // Log error but don't throw - export is secondary functionality
            }
        }
    }

    public class TrainingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int FirstSlotRefPulses { get; set; }
        public int AvgPitchPulses { get; set; }
        public int[] SlotRefPulses { get; set; }
        public int[] BoundaryPulses { get; set; }
        public int DetectedWaferCount { get; set; }
    }
}