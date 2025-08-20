using FOUPCtrl.HardwareManager.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Serializers
{
    public class ControllerSerializer
    {
        private string _path;

        public IDataSerializer DataSerializer { get; set; }

        public ControllerSerializer(Environment.SpecialFolder specialFolder)
        {
            _path = Path.Combine(Environment.GetFolderPath(specialFolder),
                                    nameof(FOUPCtrl),
                                    nameof(Controllers));
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            DataSerializer = new DataSerializer();
        }

        public void Deserialize(IController controller)
        {
            string objectPath = GetObjectPath(controller);
            DataSerializer.Deserialize(controller, objectPath);
        }

        public void Serialize(IController controller)
        {
            FileInfo file = new FileInfo(GetObjectPath(controller));
            file.Directory.Create();
            DataSerializer.Serialize(controller, file.FullName);
        }

        private string GetObjectPath<T>(T controller)
        {
            Controller obj = controller as Controller;
            return Path.Combine(_path, obj.GetType().Name, $"{obj.Id}.xml");
        }

        #region JSON
        //public void Deserialize<T>(T controller)
        //{
        //    string path = GetObjectPath(controller);
        //    using (var stream = File.OpenRead(path))
        //    {
        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        object deserializedObject = JsonSerializer.Deserialize<T>(stream, options);

        //        if (deserializedObject.ToString() != controller.ToString())
        //        {
        //            throw new Exception($"Object Id:{controller} does not match Id:{deserializedObject} in configuration file {path}.");
        //        }

        //        deserializedObject.Copy(controller, typeof(JsonIncludeAttribute));
        //    }
        //}

        //public void Serialize<T>(T controller)
        //{
        //    FileInfo file = new FileInfo(GetObjectPath(controller));
        //    file.Directory.Create();
        //    using (var stream = File.Create(file.FullName))
        //    {
        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        JsonSerializer.Serialize(stream, controller, options);
        //    }
        //}
        #endregion
    }
}
