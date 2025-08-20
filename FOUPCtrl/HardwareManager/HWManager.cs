//using FOUPCtrl.HardwareManager.Controllers;
//using FOUPCtrl.HardwareManager.Drivers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace FOUPCtrl.HardwareManager
//{
//    public class HWManager
//    {
//        public HWManager(Environment.SpecialFolder specialFolder)
//        {
//            DriverSerializer = new DriverSerializer(specialFolder);
//            ControllerSerializer = new ControllerSerializer(specialFolder);
//            Drivers = new List<IDriver>();
//        }

//        public List<IDriver> Drivers { get; }

//        public DriverSerializer DriverSerializer { get; }

//        public ControllerSerializer ControllerSerializer { get; }

//        public void LoadDrivers()
//        {
//            Drivers.AddRange(DriverSerializer.DeserializeDriverObjects());
//        }

//        public void ReadController(IController controller)
//        {
//            ControllerSerializer.Deserialize(controller);
//        }

//        public void InitController(IController controller)
//        {
//            IDriver driver = Drivers.FirstOrDefault(d => d.Id == controller.DriverId);
//            controller.SetDriver(driver);
//        }
//    }
//}
