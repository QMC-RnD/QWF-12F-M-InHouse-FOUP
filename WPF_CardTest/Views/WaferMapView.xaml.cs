using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPF_CardTest.Views
{
    public partial class WaferMapView : Window
    {
        public WaferMapView()
        {
            InitializeComponent();
            this.Loaded += WaferMapView_Loaded;
        }

        private void WaferMapView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplay();
        }

        // Properties to display wafer mapping results
        public int FirstWaferRefPos { get; set; }
        public uint AvgSlotPitch { get; set; }
        public int WaferNumber { get; set; }
        public int[] WaferStatusMap { get; set; }
        public double[] WaferThicknessMap2 { get; set; }
        public int[] WaferCount1Map2 { get; set; }
        public int[] WaferBottomMap { get; set; }
        public int[] WaferTopMap { get; set; }
        public int[] SlotRefPos { get; set; }
        public int[] SlotBoundary { get; set; }

        /// <summary>
        /// Event handler for the Close button
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Check this method in WaferMapView.xaml.cs
        public void UpdateDisplay()
        {
            // Add debug logging to trace execution
            System.Diagnostics.Debug.WriteLine("UpdateDisplay called in WaferMapView");

            // Log input values
            System.Diagnostics.Debug.WriteLine($"FirstWaferRefPos: {FirstWaferRefPos}");
            System.Diagnostics.Debug.WriteLine($"AvgSlotPitch: {AvgSlotPitch}");
            System.Diagnostics.Debug.WriteLine($"WaferNumber: {WaferNumber}");

            // Update summary information
            if (txtFirstSlotRef != null)
                txtFirstSlotRef.Text = $"First Slot Reference: {FirstWaferRefPos}";
            if (txtAvgPitch != null)
                txtAvgPitch.Text = $"Average Pitch: {AvgSlotPitch}";
            if (txtWaferCount != null)
                txtWaferCount.Text = $"Wafers Detected: {WaferNumber}";

            // Create data for the DataGrid
            if (ResultsGrid != null && WaferStatusMap != null)
            {
                // Log the number of items in arrays for debugging
                System.Diagnostics.Debug.WriteLine($"WaferStatusMap length: {WaferStatusMap?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"SlotRefPos length: {SlotRefPos?.Length ?? 0}");

                List<WaferData> waferDataList = new List<WaferData>();

                for (int i = 0; i < WaferStatusMap.Length; i++)
                {
                    // Debug the data being assigned
                    System.Diagnostics.Debug.WriteLine($"Slot {i + 1}: Status={WaferStatusMap[i]}, " +
                        $"Thickness={WaferThicknessMap2?[i] ?? 0}, " +
                        $"Count={WaferCount1Map2?[i] ?? 0}, " +
                        $"Position={SlotRefPos?[i] ?? 0}");

                    waferDataList.Add(new WaferData
                    {
                        SlotNumber = i + 1,
                        Status = GetWaferStatusString(WaferStatusMap[i]),
                        Thickness = i < WaferThicknessMap2?.Length ? WaferThicknessMap2[i] : 0,
                        Count = i < WaferCount1Map2?.Length ? WaferCount1Map2[i] : 0,
                        Position = i < SlotRefPos?.Length ? SlotRefPos[i] : 0
                    });
                }

                // Set the DataGrid's ItemsSource
                ResultsGrid.ItemsSource = waferDataList;
            }
        }



        private string GetWaferStatusString(int statusCode)
        {
            switch (statusCode)
            {
                case 0: return "Empty";
                case 1: return "Normal";
                case 2: return "Cross";
                case 3: return "Thick";
                case 4: return "Thin";
                case 5: return "Position Error";
                case 6: return "Double Wafer";
                case 10: return "Conflict";
                default: return $"Unknown ({statusCode})";
            }
        }

        /// <summary>
        /// Class to represent each row in the wafer mapping results grid
        /// </summary>
        private class WaferData
        {
            public int SlotNumber { get; set; }
            public string Status { get; set; }
            public double Thickness { get; set; }
            public int Count { get; set; }
            public int Position { get; set; }
        }
    }
}
