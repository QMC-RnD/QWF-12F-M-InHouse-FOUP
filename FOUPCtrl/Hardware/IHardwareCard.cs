//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace FOUPCtrl.Hardware
//{
//    public interface IHardwareCard
//    {
//        bool ConnectRS485(byte id, string port);
//        void Close();
//        CardStatus ReadPort(byte portId, ref byte value);
//        CardStatus WritePort(byte portId, byte value);
//        CardStatus SetAbsPosition(byte axis, int position);
//        CardStatus GetAbsPosition(byte axis, ref int position);
//        CardStatus SetFeedbackPosSrc(byte axis, byte source);
//    }

//    public enum CardStatus
//    {
//        Successful = 0,
//        LinkError = 1,
//        TimeOut = 2,
//        CheckSumError = 3,
//        InvalidCommand = 4,
//        InvalidData = 5
//    }
//}
