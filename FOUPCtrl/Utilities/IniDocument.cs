using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.Utilities
{
    public class IniDocument
    {
        public Dictionary<string, Dictionary<string, string>> Sections { get; set; }
            = new Dictionary<string, Dictionary<string, string>>();

        public virtual void Load(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ReadFile(stream);
            }
        }

        public virtual void Save(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                WriteFile(stream);
            }
        }

        public string ReadString(string section, string key, string defaultValue = null)
        {
            string value = ReadProperty(section, key);

            return value ?? defaultValue;
        }

        public byte ReadByte(string section, string key, byte defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            byte byteValue = defaultValue;
            bool parsed = byte.TryParse(value, out byteValue);

            return parsed ? byteValue : defaultValue;
        }

        public sbyte ReadSByte(string section, string key, sbyte defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            sbyte sbyteValue = defaultValue;
            bool parsed = sbyte.TryParse(value, out sbyteValue);

            return parsed ? sbyteValue : defaultValue;
        }

        public short ReadShort(string section, string key, short defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            short shortValue = defaultValue;
            bool parsed = short.TryParse(value, out shortValue);

            return parsed ? shortValue : defaultValue;
        }

        public ushort ReadUShort(string section, string key, ushort defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            ushort ushortValue = defaultValue;
            bool parsed = ushort.TryParse(value, out ushortValue);

            return parsed ? ushortValue : defaultValue;
        }

        public int ReadInt(string section, string key, int defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            int intValue = defaultValue;
            bool parsed = int.TryParse(value, out intValue);

            return parsed ? intValue : defaultValue;
        }

        public uint ReadUInt(string section, string key, uint defaultValue = 0)
        {
            string value = ReadProperty(section, key);

            uint uintValue = defaultValue;
            bool parsed = uint.TryParse(value, out uintValue);

            return parsed ? uintValue : defaultValue;
        }

        public double ReadFloat(string section, string key, float defaultValue = 0f)
        {
            string value = ReadProperty(section, key);

            float floatValue = defaultValue;
            bool parsed = float.TryParse(value, out floatValue);

            return parsed ? floatValue : defaultValue;
        }

        public double ReadDouble(string section, string key, double defaultValue = 0.0)
        {
            string value = ReadProperty(section, key);

            double doubleValue = defaultValue;
            bool parsed = double.TryParse(value, out doubleValue);

            return parsed ? doubleValue : defaultValue;
        }

        public decimal ReadDecimal(string section, string key, decimal defaultValue = 0.0m)
        {
            string value = ReadProperty(section, key);

            decimal decimalValue = defaultValue;
            bool parsed = decimal.TryParse(value, out decimalValue);

            return parsed ? decimalValue : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue = false)
        {
            string value = ReadProperty(section, key);

            bool boolValue = defaultValue;
            bool parsed = bool.TryParse(value, out boolValue);

            return parsed ? boolValue : defaultValue;
        }

        public void WriteString(string section, string key, string value)
        {
            WriteProperty(section, key, value);
        }

        public void WriteByte(string section, string key, byte value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteSByte(string section, string key, sbyte value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteShort(string section, string key, short value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteUShort(string section, string key, ushort value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteInt(string section, string key, int value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteUInt(string section, string key, uint value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteFloat(string section, string key, double value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteDouble(string section, string key, double value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteDecimal(string section, string key, decimal value)
        {
            WriteProperty(section, key, value.ToString());
        }

        public void WriteBool(string section, string key, bool value)
        {
            WriteProperty(section, key, value ? bool.TrueString : bool.FalseString);
        }

        public List<string> GetSections()
        {
            return Sections.Keys.ToList();
        }

        public int GetSectionCount()
        {
            return Sections.Keys.Count;
        }

        public int GetPropertiesCount(string section)
        {
            if (!Sections.ContainsKey(section))
            {
                return 0;
            }

            return Sections[section].Count;
        }

        public void RemoveSection(string section)
        {
            Sections.Remove(section);
        }

        public void ClearIni()
        {
            Sections.Clear();
        }

        public void SortIni()
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();

            foreach (var section in Sections.OrderBy(kvp => kvp.Key))
            {
                var properties = new Dictionary<string, string>();

                foreach (var property in section.Value.OrderBy(kvp => kvp.Key))
                {
                    properties.Add(property.Key, property.Value);
                }

                sections.Add(section.Key, properties);
            }

            Sections = sections;
        }

        protected void ReadFile(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                Sections.Clear();
                string line = null;
                Dictionary<string, string> properties = null;

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line != string.Empty)
                    {
                        if (line[0] == '[' && line[line.Length - 1] == ']')
                        {
                            properties = new Dictionary<string, string>();
                            line = line.Substring(1, line.Length - 2);
                            Sections.Add(line, properties);
                        }
                        else
                        {
                            string[] property = line.Split(new[] { '=' }, 2);
                            if (properties != null)
                            {
                                properties.Add(property[0].Trim(), property[1].Trim());
                            }
                        }
                    }
                }
            }
        }

        protected void WriteFile(Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);

            foreach (var section in Sections)
            {
                writer.WriteLine($"[{section.Key}]");
                foreach (var property in section.Value)
                {
                    writer.WriteLine(string.Format("{0}={1}", property.Key, property.Value));
                }

                writer.WriteLine();
            }

            writer.Flush();
        }

        private string ReadProperty(string section, string key)
        {
            string value = null;

            if (Sections.ContainsKey(section))
            {
                if (Sections[section].ContainsKey(key))
                {
                    value = Sections[section][key];
                }
            }

            return value;
        }

        private void WriteProperty(string section, string key, string value)
        {
            if (Sections.ContainsKey(section))
            {
                if (Sections[section].ContainsKey(key))
                {
                    Sections[section][key] = value;
                }
                else
                {
                    Sections[section].Add(key, value);
                }
            }
            else
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add(key, value);
                Sections.Add(section, properties);
            }
        }
    }
}
