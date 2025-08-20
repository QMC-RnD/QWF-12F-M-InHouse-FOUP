using System;
using System.Diagnostics;

namespace FOUPCtrl.Communication
{
    public static class CRC16Test
    {
        /// <summary>
        /// Test CRC16 calculation with known test data from your diagram
        /// </summary>
        public static void TestCRC16WithKnownData()
        {
            Debug.WriteLine("=== CRC16 Test with Known Data ===");

            // Test data from your diagram: SOH + Code + ADR + LEN + CMD
            // "01 30 30 31 52 44 57 3A 4C 50 31 2C 53 4C 4F 54 33"
            string testDataHex = "0130301352445733A4C50312C534C4F5433";

            // Calculate CRC16
            ushort calculatedCRC = CRC16Calculator.CalculateCRC16FromHexString(testDataHex);

            Debug.WriteLine($"Test data: {testDataHex}");
            Debug.WriteLine($"Calculated CRC16: 0x{calculatedCRC:X4}");
            Debug.WriteLine($"Expected CRC16: 0xF9FE");
            Debug.WriteLine($"✅ Match: {calculatedCRC == 0xF9FE}");

            // Test full message parsing
            string fullMessageHex = "0130301352445733A4C50312C534C4F5433F9FE0D";
            byte[] messageBytes = HexStringToBytes(fullMessageHex);

            Debug.WriteLine($"\nFull message: {fullMessageHex}");
            Debug.WriteLine($"Message length: {messageBytes.Length} bytes");

            // Test validation
            bool isValid = CRC16Calculator.ValidateCRC16(messageBytes);
            Debug.WriteLine($"✅ CRC16 validation: {isValid}");

            // Test parsing
            var parsedMessage = ProtocolMessage.Parse(messageBytes);
            if (parsedMessage != null)
            {
                Debug.WriteLine($"✅ Parsed command: '{parsedMessage.Command}'");
            }
            else
            {
                Debug.WriteLine("❌ Failed to parse message");
            }
        }

        /// <summary>
        /// Test creating and sending protocol messages
        /// </summary>
        public static void TestProtocolMessageCreation()
        {
            Debug.WriteLine("\n=== Protocol Message Creation Test ===");

            string[] testCommands = { "CLAMPON", "CLAMPOFF", "STAS", "LOAD", "UNLOAD" };

            foreach (string cmd in testCommands)
            {
                var message = new ProtocolMessage
                {
                    Command = cmd
                };

                byte[] messageBytes = message.ToBytes();
                string hexString = message.ToHexString();

                Debug.WriteLine($"Command: {cmd}");
                Debug.WriteLine($"Hex: {hexString}");
                Debug.WriteLine($"Length: {messageBytes.Length} bytes");

                // Test parsing back
                var parsed = ProtocolMessage.Parse(messageBytes);
                bool success = parsed != null && parsed.Command == cmd;
                Debug.WriteLine($"✅ Round-trip test: {success}\n");
            }
        }

        /// <summary>
        /// Run a quick test to verify CRC16 is working
        /// </summary>
        public static void RunQuickTest()
        {
            Debug.WriteLine("=== CRC16 Quick Test ===");

            // Test with your exact example from the diagram
            string testHex = "0130301352445733A4C50312C534C4F5433";
            ushort crc = CRC16Calculator.CalculateCRC16FromHexString(testHex);

            Debug.WriteLine($"Input: {testHex}");
            Debug.WriteLine($"CRC16: 0x{crc:X4}");
            Debug.WriteLine($"Expected: 0xF9FE");
            Debug.WriteLine($"✅ Correct: {crc == 0xF9FE}");

            // Test creating a simple protocol message
            var message = new ProtocolMessage { Command = "STAS" };
            Debug.WriteLine($"STAS message: {message.ToHexString()}");

            Debug.WriteLine("=== Test Complete ===");
        }

        /// <summary>
        /// Comprehensive test of all CRC16 functionality
        /// </summary>
        public static void RunAllTests()
        {
            Debug.WriteLine("========================================");
            Debug.WriteLine("      COMPREHENSIVE CRC16 TESTS");
            Debug.WriteLine("========================================");

            try
            {
                TestCRC16WithKnownData();
                TestProtocolMessageCreation();
                RunQuickTest();

                Debug.WriteLine("\n✅ ALL TESTS COMPLETED SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n❌ TEST FAILED: {ex.Message}");
            }

            Debug.WriteLine("========================================");
        }

        private static byte[] HexStringToBytes(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}