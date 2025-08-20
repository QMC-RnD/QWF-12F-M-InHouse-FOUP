using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoupControl;

namespace FOUPCtrl
{
    public partial class WaferMap
    {
        // MappingAnalysisResult class to store mapping analysis results
        public class MappingAnalysisResult
        {
            public int FirstWaferRefPosPulses { get; set; }
            public int AvgSlotPitchPulses { get; set; }
            public int DetectedWaferCount { get; set; }
            public int ExpectedSlots { get; set; }
            public double[] WaferThicknessMm { get; set; }
            public int[] WaferCount1Value { get; set; }
            public int[] WaferStatus { get; set; }
            public int[] SlotRefPositionPulses { get; set; }
            public int[] WaferBottomEdgePulses { get; set; }
            public int[] WaferTopEdgePulses { get; set; }
            public int[] SlotBoundaryPulses { get; set; }

            public MappingAnalysisResult(int numberOfSlots)
            {
                ExpectedSlots = numberOfSlots;
                WaferThicknessMm = new double[numberOfSlots];
                WaferCount1Value = new int[numberOfSlots];
                WaferStatus = new int[numberOfSlots];
                SlotRefPositionPulses = new int[numberOfSlots];
                WaferBottomEdgePulses = new int[numberOfSlots];
                WaferTopEdgePulses = new int[numberOfSlots];
                SlotBoundaryPulses = new int[numberOfSlots + 1];

                for (int i = 0; i < numberOfSlots; i++)
                {
                    WaferThicknessMm[i] = 0.0;
                    WaferBottomEdgePulses[i] = 0;
                    WaferTopEdgePulses[i] = 0;
                    // Explicitly set status to empty (0)
                    WaferStatus[i] = 0;
                }
            }
        }

        // Static method to perform mapping analysis with type parameters
        public static MappingAnalysisResult PerformMappingAnalysisWithTypeParameters(
            List<FoupControl.FOUP_Ctrl.DataPoint> scanData,
            double trainedSlot1RefMm,
            double trainedAvgPitchMm,
            int expectedSlots,
            // Type parameters
            double typeSlotPitchMm,
            double typePositionToleranceMm,
            double typeExpectedThicknessMm,
            double typeThicknessToleranceMm,
            int typeSlotCount,
            string typeName,
            Action<string> logger)
        {
            bool isTopToBottom = scanData.Count < 2 || scanData[0].Position > scanData[scanData.Count - 1].Position;
            logger?.Invoke($"Detected scan direction: {(isTopToBottom ? "Top-to-Bottom" : "Bottom-to-Top")}");

            // Normalize scan data direction
            if (!isTopToBottom)
            {
                scanData.Reverse();
                logger?.Invoke("Scan data reversed for bottom-to-top mapping.");
                // Optionally, adjust pitch sign if needed
                if (trainedAvgPitchMm > 0)
                    trainedAvgPitchMm = -trainedAvgPitchMm;
            }

            const double MM_PER_PULSE = 0.18;

            logger?.Invoke($"--- Starting Operational Mapping Analysis using Type: {typeName} ---");

            // Override trained parameters with type parameters if available
            if (typeSlotCount > 0)
            {
                expectedSlots = typeSlotCount;
                logger?.Invoke($"Using configured slot count: {expectedSlots} from type");
            }

            if (typeSlotPitchMm > 0 && Math.Abs(trainedAvgPitchMm) == 0)
            {
                // Only use configured pitch if no trained pitch is available
                double sign = Math.Sign(trainedAvgPitchMm);
                trainedAvgPitchMm = typeSlotPitchMm * sign;
                logger?.Invoke($"Using configured slot pitch: {trainedAvgPitchMm:F3} mm from type (no training data available)");
            }
            else if (Math.Abs(trainedAvgPitchMm) > 0)
            {
                logger?.Invoke($"Using trained slot pitch: {trainedAvgPitchMm:F3} mm (ignoring configured pitch of {typeSlotPitchMm:F3} mm)");
            }

            MappingAnalysisResult results = new MappingAnalysisResult(expectedSlots);

            // Input validation
            if (double.IsNaN(trainedSlot1RefMm) || double.IsNaN(trainedAvgPitchMm) || trainedAvgPitchMm == 0)
            {
                logger?.Invoke($"Analysis Error: Invalid parameters (Slot1Ref={trainedSlot1RefMm}, AvgPitch={trainedAvgPitchMm}). Run training first or check data.");
                results.WaferStatus = Enumerable.Repeat(99, expectedSlots).ToArray(); // Use 99 for error
                return results;
            }

            if (expectedSlots <= 0)
            {
                logger?.Invoke($"Analysis Error: Invalid expectedSlots ({expectedSlots}).");
                results.WaferStatus = Enumerable.Repeat(99, Math.Max(1, expectedSlots)).ToArray();
                return results;
            }

            // Calculate pitch and reference points
            double absPositionToleranceMm = Math.Abs(typePositionToleranceMm);
            logger?.Invoke($"Input Parameters: Slot1Ref={trainedSlot1RefMm:F2}mm, AvgPitch={trainedAvgPitchMm:F3}mm, Slots={expectedSlots}");
            logger?.Invoke($"Tolerances: Pos=+/-{absPositionToleranceMm:F3}mm, Thick=+/-{typeThicknessToleranceMm:F3}mm (Expected={typeExpectedThicknessMm:F3}mm)");

            // 1. Calculate reference positions for all slots
            double[] slotRefMm = new double[expectedSlots];
            double[] boundMm = new double[expectedSlots + 1];
            int avgPitchPulse = (int)Math.Round(Math.Abs(trainedAvgPitchMm) / MM_PER_PULSE);
            int firstPulse = (int)Math.Round(trainedSlot1RefMm / MM_PER_PULSE);

            results.AvgSlotPitchPulses = avgPitchPulse;
            results.FirstWaferRefPosPulses = firstPulse;
            results.ExpectedSlots = expectedSlots;

            // Calculate reference positions for each slot
            for (int i = 0; i < expectedSlots; i++)
            {
                slotRefMm[i] = trainedSlot1RefMm + (i * trainedAvgPitchMm);
                results.SlotRefPositionPulses[i] = firstPulse + (i * avgPitchPulse * Math.Sign(trainedAvgPitchMm));
            }

            // Calculate boundaries between slots
            boundMm[0] = slotRefMm[0] - (Math.Abs(trainedAvgPitchMm) / 2.0);
            results.SlotBoundaryPulses[0] = results.SlotRefPositionPulses[0] - (avgPitchPulse / 2);

            for (int i = 1; i < expectedSlots; i++)
            {
                boundMm[i] = (slotRefMm[i - 1] + slotRefMm[i]) / 2.0;
                results.SlotBoundaryPulses[i] = (results.SlotRefPositionPulses[i - 1] + results.SlotRefPositionPulses[i]) / 2;
            }

            boundMm[expectedSlots] = slotRefMm[expectedSlots - 1] + (Math.Abs(trainedAvgPitchMm) / 2.0);
            results.SlotBoundaryPulses[expectedSlots] = results.SlotRefPositionPulses[expectedSlots - 1] + (avgPitchPulse / 2);

            // 2. Find wafer edges in scan data
            List<(double startPos, double endPos)> waferEdges = new List<(double, double)>();

            // Find all wafer edges
            double? currentStart = null;
            for (int i = 1; i < scanData.Count; i++)
            {
                // Rising edge (start of wafer detection)
                if (scanData[i - 1].SensorValue == 0 && scanData[i].SensorValue == 1)
                {
                    currentStart = scanData[i].Position;
                }
                // Falling edge (end of wafer detection)
                else if (scanData[i - 1].SensorValue == 1 && scanData[i].SensorValue == 0 && currentStart.HasValue)
                {
                    waferEdges.Add((currentStart.Value, scanData[i - 1].Position));
                    currentStart = null;
                }
            }

            logger?.Invoke($"Detected {waferEdges.Count} wafer edges in scan data.");
            results.DetectedWaferCount = waferEdges.Count;

            // 3. Process detected wafers
            for (int w = 0; w < waferEdges.Count; w++)
            {
                var (startPos, endPos) = waferEdges[w];
                double thickness = Math.Abs(endPos - startPos);
                double centerPos = (startPos + endPos) / 2.0;

                // Find the closest slot to this wafer position
                int closestSlotIndex = -1;
                double minDistance = double.MaxValue;

                for (int i = 0; i < expectedSlots; i++)
                {
                    double distance = Math.Abs(centerPos - slotRefMm[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestSlotIndex = i;
                    }
                }

                if (closestSlotIndex >= 0)
                {
                    int slotIndex = closestSlotIndex;
                    double waferRefPos = slotRefMm[slotIndex];
                    double Thick = typeExpectedThicknessMm;
                    double ThickRange = typeThicknessToleranceMm;

                    results.WaferThicknessMm[slotIndex] = thickness;
                    results.WaferBottomEdgePulses[slotIndex] = (int)Math.Round(startPos / MM_PER_PULSE);
                    results.WaferTopEdgePulses[slotIndex] = (int)Math.Round(endPos / MM_PER_PULSE);
                    results.WaferCount1Value[slotIndex] = 1;

                    double lowerBound = waferRefPos - absPositionToleranceMm;
                    double upperBound = waferRefPos + absPositionToleranceMm;

                    // Log wafer details for debugging
                    logger?.Invoke($"Wafer details - StartPos: {startPos:F3}mm, EndPos: {endPos:F3}mm, Thickness: {thickness:F3}mm");
                    logger?.Invoke($"Position bounds - LowerBound: {lowerBound:F3}mm, UpperBound: {upperBound:F3}mm");
                    logger?.Invoke($"Thickness params - Expected: {Thick:F3}mm, Tolerance: {ThickRange:F3}mm");

                    // IMPROVED CROSS WAFER DETECTION - MODIFIED FOR INDUSTRIAL APPLICATIONS
                    bool isCrossed = false;

                    // Calculate slot boundary positions
                    double prevBoundary = double.MinValue;
                    double nextBoundary = double.MaxValue;

                    if (slotIndex > 0)
                    {
                        prevBoundary = boundMm[slotIndex]; // Boundary between previous slot and current
                    }

                    if (slotIndex < expectedSlots - 1)
                    {
                        nextBoundary = boundMm[slotIndex + 1]; // Boundary between current slot and next
                    }

                    // First check: Proximity to boundary
                    double distToPrevBoundary = Math.Abs(prevBoundary - centerPos);
                    double distToNextBoundary = Math.Abs(nextBoundary - centerPos);
                    double minBoundaryDist = Math.Min(distToPrevBoundary, distToNextBoundary);

                    // Check if wafer is near a boundary (within 1/3 of pitch)
                    bool isNearBoundary = minBoundaryDist < Math.Abs(trainedAvgPitchMm) / 3.0;

                    logger?.Invoke($"Distance to boundaries: Prev={distToPrevBoundary:F3}mm, Next={distToNextBoundary:F3}mm");
                    logger?.Invoke($"Is near boundary: {isNearBoundary} (threshold: {Math.Abs(trainedAvgPitchMm) / 3.0:F3}mm)");

                    // Second check: Physical boundary crossing
                    bool crossesPrevBoundary = (startPos < prevBoundary && endPos > prevBoundary);
                    bool crossesNextBoundary = (startPos < nextBoundary && endPos > nextBoundary);

                    logger?.Invoke($"Slot {slotIndex + 1} boundary check - Prev: {prevBoundary:F3}mm, Next: {nextBoundary:F3}mm");
                    logger?.Invoke($"Boundary crossing check - Prev: {crossesPrevBoundary}, Next: {crossesNextBoundary}");

                    // IMPORTANT INDUSTRIAL CHANGE: Check if wafer is very thick compared to normal
                    // In real-world applications, crossed wafers can appear very thick due to various optical effects
                    bool isVeryThick = thickness > 2.5 * Thick;
                    logger?.Invoke($"Is very thick check: {isVeryThick} (thickness: {thickness:F3}mm > {2.5 * Thick:F3}mm)");

                    // Determine if this is a crossed wafer by combining all evidence
                    if (crossesPrevBoundary || crossesNextBoundary)
                    {
                        // Definite physical crossing
                        results.WaferStatus[slotIndex] = 2; // Crossed
                        isCrossed = true;
                        logger?.Invoke($"Wafer physically crosses a boundary: marked as crossed");
                    }
                    else if (isNearBoundary && isVeryThick)
                    {
                        // Near boundary and very thick - industrial real-world case for crossed wafer
                        results.WaferStatus[slotIndex] = 2; // Crossed
                        isCrossed = true;
                        logger?.Invoke($"Wafer is near boundary and very thick: marked as crossed due to optical effects");
                    }
                    else if (isVeryThick && Math.Abs(minBoundaryDist - Math.Abs(trainedAvgPitchMm) / 3.0) < 2.0)
                    {
                        // Borderline case - very thick and almost at crossing threshold
                        results.WaferStatus[slotIndex] = 2; // Crossed
                        isCrossed = true;
                        logger?.Invoke($"Borderline case - very thick wafer almost at boundary: marked as crossed");
                    }

                    // If crossed, determine which neighbor is involved
                    if (isCrossed)
                    {
                        int neighborSlotIndex;

                        if (crossesPrevBoundary)
                        {
                            neighborSlotIndex = slotIndex - 1;
                        }
                        else if (crossesNextBoundary)
                        {
                            neighborSlotIndex = slotIndex + 1;
                        }
                        else
                        {
                            // Use distance to determine most likely neighbor
                            neighborSlotIndex = (distToPrevBoundary < distToNextBoundary) ? slotIndex - 1 : slotIndex + 1;
                        }

                        // Ensure neighbor index is valid
                        if (neighborSlotIndex >= 0 && neighborSlotIndex < expectedSlots)
                        {
                            results.WaferStatus[neighborSlotIndex] = 2; // Also crossed
                            logger?.Invoke($"Wafer is crossed between slots {slotIndex + 1} and {neighborSlotIndex + 1}");
                        }
                    }

                    // Only check other conditions if not crossed
                    if (!isCrossed)
                    {
                        // Treat all thick wafers as "Thick" regardless of thickness
                        if (thickness >= Thick + ThickRange)
                        {
                            results.WaferStatus[slotIndex] = 3; // Thick
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} is thick: {thickness:F3}mm");
                        }
                        // Thin wafer
                        else if (thickness <= Thick - ThickRange)
                        {
                            results.WaferStatus[slotIndex] = 4; // Thin
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} is thin: {thickness:F3}mm");
                        }
                        // Normal wafer
                        else if (startPos >= lowerBound && endPos <= upperBound)
                        {
                            results.WaferStatus[slotIndex] = 1; // Normal
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} is normal: {thickness:F3}mm");
                        }
                        // Position error
                        else
                        {
                            results.WaferStatus[slotIndex] = 5; // Position error
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} has position error: {centerPos:F2}mm vs ref {waferRefPos:F2}mm");
                        }
                    }
                }
                else
                {
                    logger?.Invoke($"Warning: Wafer at position {centerPos:F2}mm could not be assigned to a slot (closest was {closestSlotIndex + 1} at distance {minDistance:F2}mm).");
                }
            }

            // Make sure to show slot positions even when no wafers are detected
            for (int i = 0; i < expectedSlots; i++)
            {
                if (results.WaferCount1Value[i] == 0)
                {
                    results.WaferStatus[i] = 0; // Explicitly mark as empty
                }
            }

            // For debugging purposes, log all slot statuses
            logger?.Invoke("--- Slot Status Summary ---");
            for (int i = 0; i < expectedSlots; i++)
            {
                string statusText = GetStatusText(results.WaferStatus[i]);
                logger?.Invoke($"Slot {i + 1}: Status={statusText}, Thickness={results.WaferThicknessMm[i]:F3}mm");
            }

            logger?.Invoke("--- Mapping Analysis Complete ---");
            return results;
        }


        // Helper method to get status text for logging
        private static string GetStatusText(int status)
        {
            switch (status)
            {
                case 0: return "Empty";
                case 1: return "Normal";
                case 2: return "Crossed";
                case 3: return "Thick";
                case 4: return "Thin";
                case 5: return "Position Error";
                case 6: return "Double"; // Keeping this for backward compatibility
                case 10: return "Conflict";
                case 99: return "Error";
                default: return $"Unknown ({status})";
            }
        }

        // Keep the original method for backward compatibility
        public static MappingAnalysisResult PerformMappingAnalysis(
            List<FoupControl.FOUP_Ctrl.DataPoint> scanData,
            double trainedSlot1RefMm,
            double trainedAvgPitchMm,
            int expectedSlots,
            double positionToleranceMm,
            double expectedThicknessMm,
            double thicknessToleranceMm,
            Action<string> logger)
        {
            const double MM_PER_PULSE = 0.18;

            logger?.Invoke("--- Starting Operational Mapping Analysis ---");
            MappingAnalysisResult results = new MappingAnalysisResult(expectedSlots);

            // Input validation
            if (double.IsNaN(trainedSlot1RefMm) || double.IsNaN(trainedAvgPitchMm) || trainedAvgPitchMm == 0)
            {
                logger?.Invoke($"Analysis Error: Invalid training parameters (Slot1Ref={trainedSlot1RefMm}, AvgPitch={trainedAvgPitchMm}). Run training first or check data.");
                results.WaferStatus = Enumerable.Repeat(99, expectedSlots).ToArray(); // Use 99 for error
                return results;
            }

            if (expectedSlots <= 0)
            {
                logger?.Invoke($"Analysis Error: Invalid expectedSlots ({expectedSlots}).");
                results.WaferStatus = Enumerable.Repeat(99, Math.Max(1, expectedSlots)).ToArray();
                return results;
            }

            // Calculate pitch and reference points
            double absPositionToleranceMm = Math.Abs(positionToleranceMm);
            logger?.Invoke($"Input Parameters: Slot1Ref={trainedSlot1RefMm:F2}mm, AvgPitch={trainedAvgPitchMm:F3}mm, Slots={expectedSlots}");
            logger?.Invoke($"Tolerances: Pos=+/-{absPositionToleranceMm:F3}mm, Thick=+/-{thicknessToleranceMm:F3}mm (Expected={expectedThicknessMm:F3}mm)");

            // 1. Calculate reference positions for all slots
            double[] slotRefMm = new double[expectedSlots];
            double[] boundMm = new double[expectedSlots + 1];
            int avgPitchPulse = (int)Math.Round(Math.Abs(trainedAvgPitchMm) / MM_PER_PULSE);
            int firstPulse = (int)Math.Round(trainedSlot1RefMm / MM_PER_PULSE);

            results.AvgSlotPitchPulses = avgPitchPulse;
            results.FirstWaferRefPosPulses = firstPulse;
            results.ExpectedSlots = expectedSlots;

            // Calculate reference positions for each slot
            for (int i = 0; i < expectedSlots; i++)
            {
                slotRefMm[i] = trainedSlot1RefMm + (i * trainedAvgPitchMm);
                results.SlotRefPositionPulses[i] = firstPulse + (i * avgPitchPulse * Math.Sign(trainedAvgPitchMm));
            }

            // Calculate boundaries between slots
            boundMm[0] = slotRefMm[0] - (Math.Abs(trainedAvgPitchMm) / 2.0);
            results.SlotBoundaryPulses[0] = results.SlotRefPositionPulses[0] - (avgPitchPulse / 2);

            for (int i = 1; i < expectedSlots; i++)
            {
                boundMm[i] = (slotRefMm[i - 1] + slotRefMm[i]) / 2.0;
                results.SlotBoundaryPulses[i] = (results.SlotRefPositionPulses[i - 1] + results.SlotRefPositionPulses[i]) / 2;
            }

            boundMm[expectedSlots] = slotRefMm[expectedSlots - 1] + (Math.Abs(trainedAvgPitchMm) / 2.0);
            results.SlotBoundaryPulses[expectedSlots] = results.SlotRefPositionPulses[expectedSlots - 1] + (avgPitchPulse / 2);

            // 2. Find wafer edges in scan data
            List<(double startPos, double endPos)> waferEdges = new List<(double, double)>();

            // Find all wafer edges
            double? currentStart = null;
            for (int i = 1; i < scanData.Count; i++)
            {
                // Rising edge (start of wafer detection)
                if (scanData[i - 1].SensorValue == 0 && scanData[i].SensorValue == 1)
                {
                    currentStart = scanData[i].Position;
                }
                // Falling edge (end of wafer detection)
                else if (scanData[i - 1].SensorValue == 1 && scanData[i].SensorValue == 0 && currentStart.HasValue)
                {
                    waferEdges.Add((currentStart.Value, scanData[i - 1].Position));
                    currentStart = null;
                }
            }

            logger?.Invoke($"Detected {waferEdges.Count} wafer edges in scan data.");
            results.DetectedWaferCount = waferEdges.Count;

            // 3. Process detected wafers
            foreach (var (startPos, endPos) in waferEdges)
            {
                double thickness = Math.Abs(endPos - startPos);
                double centerPos = (startPos + endPos) / 2.0;

                // Find the closest slot to this wafer position
                int closestSlotIndex = -1;
                double minDistance = double.MaxValue;

                for (int i = 0; i < expectedSlots; i++)
                {
                    double distance = Math.Abs(centerPos - slotRefMm[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestSlotIndex = i;
                    }
                }

                // Check if the closest slot is within acceptable tolerance 
                // (use increased tolerance - either the configured one or the pitch/2, whichever is larger)
                double effectiveTolerance = Math.Max(absPositionToleranceMm, Math.Abs(trainedAvgPitchMm) / 2.0);
                if (closestSlotIndex >= 0 && minDistance <= effectiveTolerance)
                {
                    int slotIndex = closestSlotIndex;
                    logger?.Invoke($"Assigned wafer at {centerPos:F2}mm to slot {slotIndex + 1} (ref={slotRefMm[slotIndex]:F2}mm, dist={minDistance:F2}mm)");

                    // Store wafer data
                    results.WaferThicknessMm[slotIndex] = thickness;
                    results.WaferBottomEdgePulses[slotIndex] = (int)Math.Round(startPos / MM_PER_PULSE);
                    results.WaferTopEdgePulses[slotIndex] = (int)Math.Round(endPos / MM_PER_PULSE);
                    results.WaferCount1Value[slotIndex] = 1;  // Count the wafer

                    // Determine wafer status - use original position tolerance for classification
                    double lowerBound = slotRefMm[slotIndex] - absPositionToleranceMm;
                    double upperBound = slotRefMm[slotIndex] + absPositionToleranceMm;
                    double crossLowerBound = slotRefMm[slotIndex] - 2 * absPositionToleranceMm;
                    double crossUpperBound = slotRefMm[slotIndex] + 2 * absPositionToleranceMm;

                    bool isCross = false;

                    // Cross wafer detection
                    // If wafer bottom or top is outside normal tolerance but within 2x tolerance, and thickness is not double
                    if (
                        (
                            (startPos >= crossLowerBound && startPos <= crossUpperBound) ||
                            (endPos >= crossLowerBound && endPos <= crossUpperBound)
                        )
                    )
                    {
                        results.WaferStatus[slotIndex] = 2; // Crossed
                        logger?.Invoke($"Wafer at slot {slotIndex + 1} is crossed: start={startPos:F2}mm, end={endPos:F2}mm, thickness={thickness:F3}mm");
                        isCross = true;
                    }

                    if (!isCross)
                    {
                        // Check position
                        if (centerPos < lowerBound || centerPos > upperBound)
                        {
                            // Position error
                            results.WaferStatus[slotIndex] = 5; // Position error
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} has position error: {centerPos:F2}mm vs ref {slotRefMm[slotIndex]:F2}mm");
                        }
                        // Check thickness
                        else if (Math.Abs(thickness - expectedThicknessMm) > thicknessToleranceMm)
                        {
                            if (thickness > expectedThicknessMm)
                            {
                                // All thick wafers are just "Thick" now, no double wafer condition
                                results.WaferStatus[slotIndex] = 3; // Thick
                                logger?.Invoke($"Wafer at slot {slotIndex + 1} is thick: {thickness:F3}mm");
                            }
                            else
                            {
                                // Too thin
                                results.WaferStatus[slotIndex] = 4; // Thin
                                logger?.Invoke($"Wafer at slot {slotIndex + 1} is thin: {thickness:F3}mm");
                            }
                        }
                        else
                        {
                            // Normal wafer
                            results.WaferStatus[slotIndex] = 1; // Normal
                            logger?.Invoke($"Wafer at slot {slotIndex + 1} is normal: {thickness:F3}mm");
                        }
                    }
                }
                else
                {
                    logger?.Invoke($"Warning: Wafer at position {centerPos:F2}mm could not be assigned to a slot (closest was {closestSlotIndex + 1} at distance {minDistance:F2}mm).");
                }
            }

            // For debugging purposes, log all slot statuses
            logger?.Invoke("--- Slot Status Summary ---");
            for (int i = 0; i < expectedSlots; i++)
            {
                string statusText = GetStatusText(results.WaferStatus[i]);
                logger?.Invoke($"Slot {i + 1}: Status={statusText}, Thickness={results.WaferThicknessMm[i]:F3}mm");
            }

            logger?.Invoke("--- Mapping Analysis Complete ---");
            return results;
        }

    }
}
