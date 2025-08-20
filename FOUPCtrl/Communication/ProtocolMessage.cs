using System;
using System.Text;

namespace FOUPCtrl.Communication
{
    public class ProtocolMessage
    {
        public byte SOH { get; set; } = 0x01;
        public byte Code { get; set; } = 0x30;
        public byte Address { get; set; } = 0x30;
        public byte Length { get; set; }
        public string Command { get; set; }
        public ushort CRC16 { get; set; }
        public byte CR { get; set; } = 0x0D;

        /// <summary>
        /// Parse incoming protocol message from bytes
        /// </summary>
        /// <param name="data">Raw message bytes</param>
        /// <returns>Parsed message or null if invalid</returns>
        public static ProtocolMessage Parse(byte[] data)
        {
            try
            {
                if (data.Length < 7) // Minimum: SOH + Code + ADR + LEN + CRC16 + CR
                    return null;

                // Check if it starts with SOH
                if (data[0] != 0x01)
                    return null;

                // Check if it ends with CR
                if (data[data.Length - 1] != 0x0D)
                    return null;

                // Validate CRC16
                if (!CRC16Calculator.ValidateCRC16(data))
                    return null;

                var message = new ProtocolMessage();
                int index = 0;

                message.SOH = data[index++];
                message.Code = data[index++];
                message.Address = data[index++];
                message.Length = data[index++];

                // Extract command data
                int commandLength = message.Length;
                if (commandLength > 0)
                {
                    byte[] commandBytes = new byte[commandLength];
                    Array.Copy(data, index, commandBytes, 0, commandLength);
                    message.Command = Encoding.ASCII.GetString(commandBytes);
                    index += commandLength;
                }

                // Extract CRC16 (2 bytes)
                message.CRC16 = (ushort)((data[index] << 8) | data[index + 1]);
                index += 2;

                message.CR = data[index];

                return message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Protocol parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert message to byte array for transmission
        /// </summary>
        /// <returns>Complete message with CRC16</returns>
        public byte[] ToBytes()
        {
            // Build message without CRC first
            var commandBytes = Encoding.ASCII.GetBytes(Command ?? "");
            Length = (byte)commandBytes.Length;

            var messageData = new byte[4 + commandBytes.Length]; // SOH + Code + ADR + LEN + CMD
            messageData[0] = SOH;
            messageData[1] = Code;
            messageData[2] = Address;
            messageData[3] = Length;
            Array.Copy(commandBytes, 0, messageData, 4, commandBytes.Length);

            // Calculate CRC16 using CCITT variant (which is working correctly)
            CRC16 = CRC16Calculator.CalculateCRC16_Modbus(messageData);

            // Build final message
            var finalMessage = new byte[messageData.Length + 3]; // + CRC16(2) + CR(1)
            Array.Copy(messageData, 0, finalMessage, 0, messageData.Length);
            finalMessage[messageData.Length] = (byte)(CRC16 >> 8);     // High byte
            finalMessage[messageData.Length + 1] = (byte)(CRC16 & 0xFF); // Low byte
            finalMessage[messageData.Length + 2] = CR;

            return finalMessage;
        }

        /// <summary>
        /// Convert to hex string for debugging
        /// </summary>
        /// <returns>Hex representation of the message</returns>
        public string ToHexString()
        {
            var bytes = ToBytes();
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        /// <summary>
        /// Convert byte array to hex string for debugging
        /// </summary>
        /// <param name="data">Byte array to convert</param>
        /// <returns>Hex string representation</returns>
        public static string BytesToHexString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }
    }
}