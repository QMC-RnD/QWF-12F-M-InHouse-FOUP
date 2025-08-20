using FOUPCtrl.HardwareManager.Controllers;
using FOUPCtrl.HardwareManager.Controllers.IOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.Hardware
{
    public class Controllers
    {
        // IO1616Card _credenIOCard1 Inputs
        public IIOController ClampSensor { get; } = new CredenIOController(nameof(ClampSensor)) { Port = 0, Bit = 0 };
        public IIOController UnclampSensor { get; } = new CredenIOController(nameof(UnclampSensor)) { Port = 0, Bit = 1 };
        public IIOController LatchSensor { get; } = new CredenIOController(nameof(LatchSensor)) { Port = 0, Bit = 6 };
        public IIOController UnlatchSensor { get; } = new CredenIOController(nameof(UnlatchSensor)) { Port = 0, Bit = 7 };
        public IIOController DockForwardLimit { get; } = new CredenIOController(nameof(DockForwardLimit)) { Port = 1, Bit = 11 - 8 }; // Bit 3
        public IIOController DockBackwardLimit { get; } = new CredenIOController(nameof(DockBackwardLimit)) { Port = 1, Bit = 12 - 8 }; // Bit 4
        public IIOController ElevatorUpperLimit { get; } = new CredenIOController(nameof(ElevatorUpperLimit)) { Port = 0, Bit = 6 };
        public IIOController ProtrusionSensor { get; } = new CredenIOController(nameof(ProtrusionSensor)) { Port = 0, Bit = 7 };

        // IO1616Card _credenIOCard1 Outputs (all port 2 unless noted)
        public IIOController ClampOutput { get; } = new CredenIOController(nameof(ClampOutput)) { Port = 2, Bit = 7 };
        public IIOController UnclampOutput { get; } = new CredenIOController(nameof(UnclampOutput)) { Port = 2, Bit = 6 };
        public IIOController LatchOutput { get; } = new CredenIOController(nameof(LatchOutput)) { Port = 2, Bit = 12 - 8 }; // Bit 4
        public IIOController UnlatchOutput { get; } = new CredenIOController(nameof(UnlatchOutput)) { Port = 2, Bit = 13 - 8 }; // Bit 5
        public IIOController ElevatorUpOutput1 { get; } = new CredenIOController(nameof(ElevatorUpOutput1)) { Port = 2, Bit = 2 };
        public IIOController ElevatorUpOutput2 { get; } = new CredenIOController(nameof(ElevatorUpOutput2)) { Port = 2, Bit = 5 };
        public IIOController ElevatorDownOutput1 { get; } = new CredenIOController(nameof(ElevatorDownOutput1)) { Port = 2, Bit = 3 };
        public IIOController ElevatorDownOutput2 { get; } = new CredenIOController(nameof(ElevatorDownOutput2)) { Port = 2, Bit = 4 };
        public IIOController DoorForwardOutput { get; } = new CredenIOController(nameof(DoorForwardOutput)) { Port = 2, Bit = 11 - 8 }; // Bit 3
        public IIOController DoorBackwardOutput { get; } = new CredenIOController(nameof(DoorBackwardOutput)) { Port = 2, Bit = 10 - 8 }; // Bit 2
        public IIOController DockForwardOutput { get; } = new CredenIOController(nameof(DockForwardOutput)) { Port = 2, Bit = 9 - 8 }; // Bit 1
        public IIOController DockBackwardOutput { get; } = new CredenIOController(nameof(DockBackwardOutput)) { Port = 2, Bit = 8 - 8 }; // Bit 0
        public IIOController MappingForwardOutput { get; } = new CredenIOController(nameof(MappingForwardOutput)) { Port = 2, Bit = 14 - 8 }; // Bit 6
        public IIOController MappingBackwardOutput { get; } = new CredenIOController(nameof(MappingBackwardOutput)) { Port = 2, Bit = 15 - 8 }; // Bit 7

        // IO1616Card _credenIOCard2 Inputs
        public IIOController ElevatorLowerLimit { get; } = new CredenIOController(nameof(ElevatorLowerLimit)) { Port = 0, Bit = 4 };
        public IIOController DoorForwardLimit { get; } = new CredenIOController(nameof(DoorForwardLimit)) { Port = 1, Bit = 10 - 8 }; // Bit 2
        public IIOController DoorBackwardLimit { get; } = new CredenIOController(nameof(DoorBackwardLimit)) { Port = 1, Bit = 11 - 8 }; // Bit 3
        public IIOController MappingForwardLimit { get; } = new CredenIOController(nameof(MappingForwardLimit)) { Port = 1, Bit = 12 - 8 }; // Bit 4
        public IIOController MappingBackwardLimit { get; } = new CredenIOController(nameof(MappingBackwardLimit)) { Port = 1, Bit = 13 - 8 }; // Bit 5
        public IIOController VacuumSensorInput { get; } = new CredenIOController(nameof(VacuumSensorInput)) { Port = 1, Bit = 8 - 8 }; // Bit 0

        // IO1616Card _credenIOCard2 Outputs
        public IIOController VacuumOutput { get; } = new CredenIOController(nameof(VacuumOutput)) { Port = 2, Bit = 0 };

        public Controllers()
        {
        }

        public List<IController> GetControllerList()
        {
            List<IController> controllers = new List<IController>();

            controllers.Add(ClampSensor);
            controllers.Add(UnclampSensor);
            controllers.Add(LatchSensor);
            controllers.Add(UnlatchSensor);
            controllers.Add(DockForwardLimit);
            controllers.Add(DockBackwardLimit);
            controllers.Add(ElevatorUpperLimit);
            controllers.Add(ProtrusionSensor);
            controllers.Add(ClampOutput);
            controllers.Add(UnclampOutput);
            controllers.Add(LatchOutput);
            controllers.Add(UnlatchOutput);
            controllers.Add(ElevatorUpOutput1);
            controllers.Add(ElevatorUpOutput2);
            controllers.Add(ElevatorDownOutput1);
            controllers.Add(ElevatorDownOutput2);
            controllers.Add(DoorForwardOutput);
            controllers.Add(DoorBackwardOutput);
            controllers.Add(DockForwardOutput);
            controllers.Add(DockBackwardOutput);
            controllers.Add(MappingForwardOutput);
            controllers.Add(MappingBackwardOutput);

            // IO Card 2 - New controllers
            controllers.Add(ElevatorLowerLimit);
            controllers.Add(DoorForwardLimit);
            controllers.Add(DoorBackwardLimit);
            controllers.Add(MappingForwardLimit);
            controllers.Add(MappingBackwardLimit);
            controllers.Add(VacuumSensorInput);
            controllers.Add(VacuumOutput);

            return controllers;

        }
    }
}
