using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPF_CardTest.Views
{
    public partial class WaferMapTeachingView : Window
    {
        public class SlotData
        {
            public int SlotNumber { get; set; }
            public int Position { get; set; }
            public int BoundaryStart { get; set; }
            public int BoundaryEnd { get; set; }
        }

        // Properties to expose training data
        public uint TeachAvgSlotPitch { get; set; }
        public int TeachWaferNumber { get; set; }
        public int TeachFirstWaferRefPos { get; set; }
        public int[] TeachSlotRefPos { get; set; }
        public int[] TeachSlotBoundary { get; set; }

        private SlotData _selectedSlot;

        public WaferMapTeachingView()
        {
            InitializeComponent();
        }

        public void LoadData(int firstWaferRef, int avgPitch, int[] slotRefPositions, int[] boundaries)
        {
            // Store values in properties
            TeachFirstWaferRefPos = firstWaferRef;
            TeachAvgSlotPitch = (uint)Math.Abs(avgPitch);
            TeachWaferNumber = slotRefPositions.Length;
            TeachSlotRefPos = slotRefPositions;
            TeachSlotBoundary = boundaries;

            // Update summary info
            txtFirstSlotRef.Text = $"First Slot Reference: {firstWaferRef}";
            txtAvgPitch.Text = $"Average Pitch: {avgPitch}";
            txtSlotCount.Text = $"Slot Count: {slotRefPositions.Length}";

            // Create data for DataGrid
            List<SlotData> slotDataList = new List<SlotData>();

            for (int i = 0; i < slotRefPositions.Length; i++)
            {
                slotDataList.Add(new SlotData
                {
                    SlotNumber = i + 1,
                    Position = slotRefPositions[i],
                    BoundaryStart = i > 0 ? boundaries[i] : boundaries[0],
                    BoundaryEnd = i < slotRefPositions.Length - 1 ? boundaries[i + 1] : boundaries[boundaries.Length - 1]
                });
            }

            TeachingGrid.ItemsSource = slotDataList;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Event handler for TeachingGrid selection changes
        /// </summary>
        private void TeachingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected slot data
            _selectedSlot = TeachingGrid.SelectedItem as SlotData;

            if (_selectedSlot != null)
            {
                // Update display to show details about the selected slot
                // Create a detail display section if needed or update the summary
                string detailText = $"Selected Slot: {_selectedSlot.SlotNumber}\n" +
                                   $"Position: {_selectedSlot.Position}\n" +
                                   $"Range: {_selectedSlot.BoundaryStart} - {_selectedSlot.BoundaryEnd}";

                // Display a temporary popup with details or update a UI element
                MessageBox.Show(detailText, $"Slot {_selectedSlot.SlotNumber} Details",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // Optionally: Visualization highlighting
                // This would require additional UI elements to show the selected slot position
            }
        }
    }
}
