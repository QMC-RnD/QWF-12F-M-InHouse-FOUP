//using System;
//using System.Reflection;
//using System.IO;
//using System.Linq;

//namespace FOUPCtrl.Hardware
//{
//    public static class HardwareCardFactory
//    {
//        public static IHardwareCard CreateIO1616Card()
//        {
//            if (Environment.Is64BitProcess)
//            {
//                return new Hardware64CardWrapper("IO1616Card");
//            }
//            else
//            {
//                return new Hardware32CardWrapper("IO1616Card");
//            }
//        }

//        public static IHardwareCard CreateAX0040Card()
//        {
//            if (Environment.Is64BitProcess)
//            {
//                return new Hardware64CardWrapper("AX0040Card");
//            }
//            else
//            {
//                return new Hardware32CardWrapper("AX0040Card");
//            }
//        }
//    }

//    // 32-bit hardware wrapper using reflection
//    internal class Hardware32CardWrapper : IHardwareCard
//    {
//        private object _card;
//        private Type _cardType;

//        public Hardware32CardWrapper(string cardTypeName)
//        {
//            try
//            {
//                var assembly = Assembly.LoadFrom("Creden.Hardware.Cards.dll");
//                _cardType = assembly.GetType($"Creden.Hardware.Cards.{cardTypeName}");

//                if (_cardType == null)
//                {
//                    // Try to find the type with different casing or variations
//                    _cardType = FindTypeInAssembly(assembly, cardTypeName);

//                    if (_cardType == null)
//                    {
//                        var availableTypes = string.Join(", ", assembly.GetExportedTypes().Select(t => t.Name));
//                        throw new TypeLoadException($"Type 'Creden.Hardware.Cards.{cardTypeName}' not found in assembly. Available types: {availableTypes}");
//                    }
//                }

//                _card = Activator.CreateInstance(_cardType);
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidOperationException($"Failed to create 32-bit {cardTypeName}: {ex.Message}", ex);
//            }
//        }

//        public bool ConnectRS485(byte id, string port)
//        {
//            var method = _cardType.GetMethod("ConnectRS485");
//            return (bool)method.Invoke(_card, new object[] { id, port });
//        }

//        public void Close()
//        {
//            var method = _cardType.GetMethod("Close");
//            method.Invoke(_card, null);
//        }

//        public CardStatus ReadPort(byte portId, ref byte value)
//        {
//            var method = _cardType.GetMethod("ReadPort");
//            var parameters = new object[] { portId, value };
//            var result = method.Invoke(_card, parameters);
//            value = (byte)parameters[1];
//            return (CardStatus)result;
//        }

//        public CardStatus WritePort(byte portId, byte value)
//        {
//            var method = _cardType.GetMethod("WritePort");
//            return (CardStatus)method.Invoke(_card, new object[] { portId, value });
//        }

//        public CardStatus SetAbsPosition(byte axis, int position)
//        {
//            var method = _cardType.GetMethod("SetAbsPosition");
//            return (CardStatus)method.Invoke(_card, new object[] { axis, position });
//        }

//        public CardStatus GetAbsPosition(byte axis, ref int position)
//        {
//            var method = _cardType.GetMethod("GetAbsPosition");
//            var parameters = new object[] { axis, position };
//            var result = method.Invoke(_card, parameters);
//            position = (int)parameters[1];
//            return (CardStatus)result;
//        }

//        public CardStatus SetFeedbackPosSrc(byte axis, byte source)
//        {
//            var method = _cardType.GetMethod("SetFeedbackPosSrc");
//            return (CardStatus)method.Invoke(_card, new object[] { axis, source });
//        }

//        private static Type FindTypeInAssembly(Assembly assembly, string typeName)
//        {
//            // Try different variations of the type name
//            var possibleNames = new[]
//            {
//                $"Creden.Hardware.Cards.{typeName}",
//                $"Creden.Hardware64.Cards.{typeName}",
//                typeName,
//                $"{typeName}",
//                $"Creden.Hardware.{typeName}",
//                $"Creden.{typeName}"
//            };

//            foreach (var name in possibleNames)
//            {
//                var type = assembly.GetType(name, false, true); // case-insensitive
//                if (type != null)
//                {
//                    System.Diagnostics.Debug.WriteLine($"Found type: {type.FullName}");
//                    return type;
//                }
//            }

//            // Try to find by partial name match
//            var types = assembly.GetExportedTypes();
//            var matchingType = types.FirstOrDefault(t =>
//                t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
//                t.Name.Contains(typeName));

//            if (matchingType != null)
//            {
//                System.Diagnostics.Debug.WriteLine($"Found matching type: {matchingType.FullName}");
//            }

//            return matchingType;
//        }
//    }

//    // 64-bit hardware wrapper using reflection
//    internal class Hardware64CardWrapper : IHardwareCard
//    {
//        private object _card;
//        private Type _cardType;

//        public Hardware64CardWrapper(string cardTypeName)
//        {
//            try
//            {
//                var assembly = Assembly.LoadFrom("Creden.Hardware64.Cards.dll");
//                _cardType = assembly.GetType($"Creden.Hardware64.Cards.{cardTypeName}");

//                if (_cardType == null)
//                {
//                    // Try to find the type with different casing or variations
//                    _cardType = FindTypeInAssembly(assembly, cardTypeName);

//                    if (_cardType == null)
//                    {
//                        var availableTypes = string.Join(", ", assembly.GetExportedTypes().Select(t => t.Name));
//                        throw new TypeLoadException($"Type 'Creden.Hardware64.Cards.{cardTypeName}' not found in assembly. Available types: {availableTypes}");
//                    }
//                }

//                _card = Activator.CreateInstance(_cardType);
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidOperationException($"Failed to create 64-bit {cardTypeName}: {ex.Message}", ex);
//            }
//        }

//        public bool ConnectRS485(byte id, string port)
//        {
//            var method = _cardType.GetMethod("ConnectRS485");
//            return (bool)method.Invoke(_card, new object[] { id, port });
//        }

//        public void Close()
//        {
//            var method = _cardType.GetMethod("Close");
//            method.Invoke(_card, null);
//        }

//        public CardStatus ReadPort(byte portId, ref byte value)
//        {
//            var method = _cardType.GetMethod("ReadPort");
//            var parameters = new object[] { portId, value };
//            var result = method.Invoke(_card, parameters);
//            value = (byte)parameters[1];
//            return (CardStatus)result;
//        }

//        public CardStatus WritePort(byte portId, byte value)
//        {
//            var method = _cardType.GetMethod("WritePort");
//            return (CardStatus)method.Invoke(_card, new object[] { portId, value });
//        }

//        public CardStatus SetAbsPosition(byte axis, int position)
//        {
//            var method = _cardType.GetMethod("SetAbsPosition");
//            return (CardStatus)method.Invoke(_card, new object[] { axis, position });
//        }

//        public CardStatus GetAbsPosition(byte axis, ref int position)
//        {
//            var method = _cardType.GetMethod("GetAbsPosition");
//            var parameters = new object[] { axis, position };
//            var result = method.Invoke(_card, parameters);
//            position = (int)parameters[1];
//            return (CardStatus)result;
//        }

//        public CardStatus SetFeedbackPosSrc(byte axis, byte source)
//        {
//            var method = _cardType.GetMethod("SetFeedbackPosSrc");
//            return (CardStatus)method.Invoke(_card, new object[] { axis, source });
//        }

//        private static Type FindTypeInAssembly(Assembly assembly, string typeName)
//        {
//            // Try different variations of the type name
//            var possibleNames = new[]
//            {
//                $"Creden.Hardware64.Cards.{typeName}",
//                $"Creden.Hardware.Cards.{typeName}",
//                typeName,
//                $"{typeName}",
//                $"Creden.Hardware64.{typeName}",
//                $"Creden.{typeName}"
//            };

//            foreach (var name in possibleNames)
//            {
//                var type = assembly.GetType(name, false, true); // case-insensitive
//                if (type != null)
//                {
//                    System.Diagnostics.Debug.WriteLine($"Found type: {type.FullName}");
//                    return type;
//                }
//            }

//            // Try to find by partial name match
//            var types = assembly.GetExportedTypes();
//            var matchingType = types.FirstOrDefault(t =>
//                t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
//                t.Name.Contains(typeName));

//            if (matchingType != null)
//            {
//                System.Diagnostics.Debug.WriteLine($"Found matching type: {matchingType.FullName}");
//            }

//            return matchingType;
//        }
//    }
//}