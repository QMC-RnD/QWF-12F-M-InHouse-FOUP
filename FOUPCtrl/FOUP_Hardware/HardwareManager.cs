using FoupControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace FOUPCtrl.Hardware
{
    public static class HardwareManager
    {
        private static readonly Lazy<FOUP_Ctrl> _foupCtrl = new Lazy<FOUP_Ctrl>(() => CreateFoupCtrl());
        private static bool _hardwareAvailable = true;

        public static FOUP_Ctrl FoupCtrl
        {
            get
            {
                if (!_hardwareAvailable)
                {
                    throw new InvalidOperationException("Hardware drivers are not available. Cannot access FOUP controller.");
                }
                return _foupCtrl.Value;
            }
        }

        public static bool IsHardwareAvailable => _hardwareAvailable;

        private static FOUP_Ctrl CreateFoupCtrl()
        {
            try
            {
                // Log the architecture we're running under
                string processArch = Environment.Is64BitProcess ? "64-bit" : "32-bit";
                string osArch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
                System.Diagnostics.Debug.WriteLine($"Creating FOUP_Ctrl in {processArch} process on {osArch} OS");

                // Additional diagnostic information
                LogHardwareDiagnostics();
                var archInfo = HardwareManager.GetArchitectureInfo();
                var isValid = HardwareManager.ValidateHardwareCompatibility(out string message);
                System.Diagnostics.Debug.WriteLine($"Architecture: {archInfo}");
                System.Diagnostics.Debug.WriteLine($"Hardware Valid: {isValid}, Message: {message}");

                return new FOUP_Ctrl();
            }
            catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("Creden.Hardware"))
            {
                _hardwareAvailable = false;
                string architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";
                string requiredDll = Environment.Is64BitProcess ? "Creden.Hardware64.Cards.dll" : "Creden.Hardware.Cards.dll";

                System.Diagnostics.Debug.WriteLine($"Hardware DLL not found for {architecture} process: {ex.Message}");
                throw new InvalidOperationException(
                    $"Creden hardware drivers not found for {architecture} process. " +
                    $"Please ensure {requiredDll} is in the application directory.",
                    ex);
            }
            catch (System.BadImageFormatException ex)
            {
                _hardwareAvailable = false;
                string architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";

                System.Diagnostics.Debug.WriteLine($"Hardware DLL architecture mismatch for {architecture} process: {ex.Message}");
                throw new InvalidOperationException(
                    $"Hardware DLL architecture mismatch. Running {architecture} process but DLL is for different architecture. " +
                    $"Make sure you're using the correct platform build (x86/x64).",
                    ex);
            }
            catch (TypeLoadException ex)
            {
                _hardwareAvailable = false;
                System.Diagnostics.Debug.WriteLine($"Type loading error: {ex.Message}");
                throw new InvalidOperationException(
                    $"Failed to load hardware types from DLL. {ex.Message} " +
                    $"This might indicate a version mismatch or missing dependencies.",
                    ex);
            }
            catch (Exception ex)
            {
                _hardwareAvailable = false;
                System.Diagnostics.Debug.WriteLine($"Unexpected error creating FOUP_Ctrl: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                throw new InvalidOperationException(
                    $"Failed to initialize hardware controller: {ex.Message}",
                    ex);
            }
        }

        private static void LogHardwareDiagnostics()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string dllName = Environment.Is64BitProcess ? "Creden.Hardware64.Cards.dll" : "Creden.Hardware.Cards.dll";
                string dllPath = Path.Combine(baseDirectory, dllName);

                System.Diagnostics.Debug.WriteLine($"Looking for DLL: {dllPath}");
                System.Diagnostics.Debug.WriteLine($"DLL exists: {File.Exists(dllPath)}");

                if (File.Exists(dllPath))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        var types = assembly.GetExportedTypes();
                        System.Diagnostics.Debug.WriteLine($"DLL loaded successfully. Found {types.Length} exported types:");

                        foreach (var type in types.Take(10)) // Log first 10 types
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {type.FullName}");
                        }

                        if (types.Length > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"  ... and {types.Length - 10} more types");
                        }

                        // Specifically look for IO1616Card-related types
                        var ioTypes = types.Where(t => t.Name.Contains("IO1616") || t.Name.Contains("Card")).ToList();
                        if (ioTypes.Any())
                        {
                            System.Diagnostics.Debug.WriteLine("IO/Card-related types found:");
                            foreach (var type in ioTypes)
                            {
                                System.Diagnostics.Debug.WriteLine($"  - {type.FullName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load or inspect DLL: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LogHardwareDiagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to initialize hardware and returns success status
        /// </summary>
        public static bool TryInitializeHardware(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var ctrl = FoupCtrl; // This will trigger initialization
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets information about the current process and expected DLL
        /// </summary>
        public static string GetArchitectureInfo()
        {
            string processArch = Environment.Is64BitProcess ? "64-bit" : "32-bit";
            string osArch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            string requiredDll = Environment.Is64BitProcess ? "Creden.Hardware64.Cards.dll" : "Creden.Hardware.Cards.dll";

            return $"Process: {processArch}, OS: {osArch}, Required DLL: {requiredDll}";
        }

        /// <summary>
        /// Validates that the correct hardware DLL is available and matches the process architecture
        /// </summary>
        public static bool ValidateHardwareCompatibility(out string validationMessage)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string expectedDll = Environment.Is64BitProcess ? "Creden.Hardware64.Cards.dll" : "Creden.Hardware.Cards.dll";
                string wrongDll = Environment.Is64BitProcess ? "Creden.Hardware.Cards.dll" : "Creden.Hardware64.Cards.dll";

                string expectedPath = Path.Combine(baseDirectory, expectedDll);
                string wrongPath = Path.Combine(baseDirectory, wrongDll);

                bool hasCorrectDll = File.Exists(expectedPath);
                bool hasWrongDll = File.Exists(wrongPath);

                if (hasCorrectDll && !hasWrongDll)
                {
                    validationMessage = $"Correct hardware DLL found: {expectedDll}";
                    return true;
                }
                else if (!hasCorrectDll && hasWrongDll)
                {
                    validationMessage = $"Wrong architecture DLL found: {wrongDll}. Expected: {expectedDll}";
                    return false;
                }
                else if (hasCorrectDll && hasWrongDll)
                {
                    validationMessage = $"Both DLLs present. This may cause conflicts. Remove: {wrongDll}";
                    return true; // Still usable but warn
                }
                else
                {
                    validationMessage = $"Hardware DLL not found. Expected: {expectedDll}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                validationMessage = $"Error validating hardware: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Lists all available types in the hardware DLL for debugging purposes
        /// </summary>
        public static string[] GetAvailableHardwareTypes()
        {
            try
            {
                string dllName = Environment.Is64BitProcess ? "Creden.Hardware64.Cards.dll" : "Creden.Hardware.Cards.dll";
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);

                if (!File.Exists(dllPath))
                {
                    return new[] { $"DLL not found: {dllPath}" };
                }

                var assembly = Assembly.LoadFrom(dllPath);
                return assembly.GetExportedTypes().Select(t => t.FullName).ToArray();
            }
            catch (Exception ex)
            {
                return new[] { $"Error loading DLL: {ex.Message}" };
            }
        }
    }
}