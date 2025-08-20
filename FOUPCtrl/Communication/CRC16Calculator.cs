using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.Communication
{
    public static class CRC16Calculator
    {
        /// <summary>
        /// Calculate CRC16 checksum for the given data
        /// Uses CRC16-CCITT (polynomial 0x1021) with initial value 0xFFFF
        /// This is the most common CRC16 variant for communication protocols
        /// </summary>
        /// <param name="data">Data bytes to calculate CRC for</param>
        /// <returns>CRC16 checksum as UInt16</returns>
        public static ushort CalculateCRC16(byte[] data)
        {
            const ushort polynomial = 0x1021; // CRC16-CCITT polynomial
            ushort crc = 0xFFFF; // Initial value for CRC16-CCITT

            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ polynomial);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculate CRC16 using IBM/ANSI variant (polynomial 0x8005)
        /// </summary>
        /// <param name="data">Data bytes to calculate CRC for</param>
        /// <returns>CRC16 checksum as UInt16</returns>
        public static ushort CalculateCRC16_IBM(byte[] data)
        {
            const ushort polynomial = 0x8005; // IBM/ANSI polynomial
            ushort crc = 0x0000; // Initial value for IBM variant

            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ polynomial);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculate CRC16 using Modbus variant (polynomial 0x8005, initial 0xFFFF, reflected)
        /// </summary>
        /// <param name="data">Data bytes to calculate CRC for</param>
        /// <returns>CRC16 checksum as UInt16</returns>
        public static ushort CalculateCRC16_Modbus(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculate CRC16 from hex string (for testing purposes)
        /// </summary>
        /// <param name="hexString">Hex string without spaces</param>
        /// <returns>CRC16 checksum as UInt16</returns>
        public static ushort CalculateCRC16FromHexString(string hexString)
        {
            // Remove spaces and convert to uppercase
            hexString = hexString.Replace(" ", "").ToUpper();

            // Convert hex string to byte array
            byte[] data = new byte[hexString.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return CalculateCRC16(data);
        }

        /// <summary>
        /// Test all CRC16 variants with the given test data
        /// </summary>
        /// <param name="hexString">Test data as hex string</param>
        /// <param name="expectedCRC">Expected CRC16 value</param>
        public static void TestAllCRC16Variants(string hexString, ushort expectedCRC)
        {
            // Remove spaces and convert to uppercase
            hexString = hexString.Replace(" ", "").ToUpper();

            // Convert hex string to byte array
            byte[] data = new byte[hexString.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            System.Diagnostics.Debug.WriteLine($"=== Testing CRC16 Variants ===");
            System.Diagnostics.Debug.WriteLine($"Test data: {hexString}");
            System.Diagnostics.Debug.WriteLine($"Expected CRC: 0x{expectedCRC:X4}");
            System.Diagnostics.Debug.WriteLine("");

            // Test CCITT variant
            ushort crcCCITT = CalculateCRC16(data);
            System.Diagnostics.Debug.WriteLine($"CRC16-CCITT (0x1021, init 0xFFFF): 0x{crcCCITT:X4} {(crcCCITT == expectedCRC ? "✅ MATCH" : "❌")}");

            // Test IBM variant  
            ushort crcIBM = CalculateCRC16_IBM(data);
            System.Diagnostics.Debug.WriteLine($"CRC16-IBM (0x8005, init 0x0000):   0x{crcIBM:X4} {(crcIBM == expectedCRC ? "✅ MATCH" : "❌")}");

            // Test Modbus variant
            ushort crcModbus = CalculateCRC16_Modbus(data);
            System.Diagnostics.Debug.WriteLine($"CRC16-Modbus (0x8005, reflected):  0x{crcModbus:X4} {(crcModbus == expectedCRC ? "✅ MATCH" : "❌")}");

            System.Diagnostics.Debug.WriteLine("");
        }

        /// <summary>
        /// Validate a message with CRC16 checksum
        /// </summary>
        /// <param name="messageWithCRC">Complete message including CRC16 bytes</param>
        /// <returns>True if CRC is valid</returns>
        public static bool ValidateCRC16(byte[] messageWithCRC)
        {
            if (messageWithCRC.Length < 3) // At least 1 data byte + 2 CRC bytes
                return false;

            // Extract data (everything except last 3 bytes: CRC16 + CR)
            byte[] data = new byte[messageWithCRC.Length - 3];
            Array.Copy(messageWithCRC, 0, data, 0, data.Length);

            // Extract CRC (bytes before the last CR byte)
            ushort receivedCRC = (ushort)((messageWithCRC[messageWithCRC.Length - 3] << 8) |
                                          messageWithCRC[messageWithCRC.Length - 2]);

            // Test all variants to see which one matches
            ushort calculatedCRC_CCITT = CalculateCRC16(data);
            ushort calculatedCRC_IBM = CalculateCRC16_IBM(data);
            ushort calculatedCRC_Modbus = CalculateCRC16_Modbus(data);

            System.Diagnostics.Debug.WriteLine($"Received CRC: 0x{receivedCRC:X4}");
            System.Diagnostics.Debug.WriteLine($"Calculated CRC-CCITT: 0x{calculatedCRC_CCITT:X4}");
            System.Diagnostics.Debug.WriteLine($"Calculated CRC-IBM: 0x{calculatedCRC_IBM:X4}");
            System.Diagnostics.Debug.WriteLine($"Calculated CRC-Modbus: 0x{calculatedCRC_Modbus:X4}");

            return receivedCRC == calculatedCRC_CCITT ||
                   receivedCRC == calculatedCRC_IBM ||
                   receivedCRC == calculatedCRC_Modbus;
        }

        /// <summary>
        /// Test CRC16 calculation with known test data
        /// </summary>
        public static void TestCRC16()
        {
            // Test with your example data
            string testData = "0130301352445733A4C50312C534C4F5433";
            ushort expectedCRC = 0xF9FE;

            TestAllCRC16Variants(testData, expectedCRC);
        }
    }
}
