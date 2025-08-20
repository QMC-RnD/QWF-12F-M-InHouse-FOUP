using FOUPCtrl.HardwareManager.Drivers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Serializers
{
    public class DriverSerializer
    {
        private string _path;

        public IDataSerializer DataSerializer { get; set; }

        public DriverSerializer(Environment.SpecialFolder specialFolder)
        {
            _path = Path.Combine(Environment.GetFolderPath(specialFolder),
                                    nameof(FOUPCtrl),
                                    nameof(Drivers));
            DataSerializer = new DataSerializer();
        }

        public void Deserialize(IDriver driver)
        {
            string objectPath = GetObjectPath(driver);
            DataSerializer.Deserialize(driver, objectPath);
        }

        public void Serialize(IDriver driver)
        {
            FileInfo file = new FileInfo(GetObjectPath(driver));
            file.Directory.Create();
            DataSerializer.Serialize(driver, file.FullName);
        }

        public List<IDriver> DeserializeDriverObjects()
        {
            return DataSerializer.DeserializeObjects(_path).Cast<IDriver>().ToList();
        }

        private string GetObjectPath<T>(T driver)
        {
            Driver obj = driver as Driver;
            return Path.Combine(_path, obj.GetType().Name, $"{obj.Id}.xml");
        }

        public void Delete(IDriver driver)
        {
            FileInfo file = new FileInfo(GetObjectPath(driver));
            file.Delete();
        }

        private string FolderName(string filepath)
        {
            return new DirectoryInfo(filepath).Name;
        }

        #region JSON
        //public void Deserialize<T>(T driver)
        //{
        //    string path = GetObjectPath(driver);
        //    using (var stream = File.OpenRead(path))
        //    {
        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        object deserializedObject = JsonSerializer.Deserialize<T>(stream, options);

        //        if (deserializedObject.ToString() != driver.ToString())
        //        {
        //            throw new Exception($"Object Id:{driver} does not match Id:{deserializedObject} in configuration file {path}.");
        //        }

        //        deserializedObject.Copy(driver, typeof(JsonIncludeAttribute));
        //    }
        //}

        //public void Serialize<T>(T driver)
        //{
        //    FileInfo file = new FileInfo(GetObjectPath(driver));
        //    file.Directory.Create();
        //    using (var stream = File.Create(file.FullName))
        //    {
        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        JsonSerializer.Serialize(stream, driver, options);
        //    }
        //}

        //public List<IDriver> DeserializeDriverObjects()
        //{
        //    List<object> objects = new List<object>();

        //    Parallel.ForEach(Directory.GetFiles(_path, "*.json", SearchOption.AllDirectories), filepath => 
        //    {
        //        using (var stream = File.OpenRead(filepath))
        //        {
        //            var options = new JsonSerializerOptions { WriteIndented = true };

        //            switch (new DirectoryInfo(filepath).Parent.Name)
        //            {
        //                case nameof(CredenAxisDriver):
        //                    objects.Add(JsonSerializer.Deserialize<CredenAxisDriver>(stream, options));
        //                    break;
        //                case nameof(CredenIODriver):
        //                    objects.Add(JsonSerializer.Deserialize<CredenIODriver>(stream, options));
        //                    break;
        //                case nameof(SensofarDriver):
        //                    objects.Add(JsonSerializer.Deserialize<SensofarDriver>(stream, options));
        //                    break;
        //            }
        //            //var document = JsonSerializer.Deserialize<object>(stream, options);
        //            //var root = document.RootElement.GetProperty("Type").GetString();
        //            //var type = Type.GetType(root);
        //            //var deserializedObject = JsonSerializer.Deserialize(stream, type, options);
        //            //objects.Add(document);
        //        }
        //    });

        //    return objects.Cast<IDriver>().ToList();
        //}
        #endregion
    }
}
