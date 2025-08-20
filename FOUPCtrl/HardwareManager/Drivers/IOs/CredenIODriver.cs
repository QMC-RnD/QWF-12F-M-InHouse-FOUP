using Creden.Hardware.Cards;
using FOUPCtrl.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using FOUPCtrl.HardwareManager.Exceptions;

namespace FOUPCtrl.HardwareManager.Drivers.IOs
{
    [DataContract(Namespace = "")]
    public class CredenIODriver : Driver, IIODriver
    {
        //private static NLog.ILogger logger = NLog.LogManager.GetCurrentClassLogger();
        private IO1616Card _credenIOCard;

        [DataMember]
        public int Address { get; set; }

        [DataMember]
        public string PortName { get; set; }

        public IReadOnlyList<object> Mutexes { get; set; } = new List<object>()
        {
            new object(),
            new object(),
            new object(),
            new object(),
        }
        .AsReadOnly();

        public CredenIODriver(int address, string portName)
        {
            Address = address;
            PortName = portName;
            Id = $"{GetType().Name}[{Address.ToString("D2")}][{PortName}]";
        }

        public override void Connect()
        {
            if (Connected)
            {
                return;
            }

            _credenIOCard = new IO1616Card();

            Connected = _credenIOCard.ConnectRS485((byte)Address, PortName);

            if (!Connected)
            {
                _credenIOCard.Close();
                _credenIOCard = null;
                throw new InvalidOperationException($"Unable to connect to {Id}");
            }
            else
            {
                try
                {
                    string result = String.Empty;
                    CardStatus success = _credenIOCard.GetFirmwareVersion(ref result, (byte)255);
                    if (success != CardStatus.Successful)
                    {
                        Connected = false;
                        _credenIOCard.Close();
                        _credenIOCard = null;
                        throw new InvalidOperationException($"Unable to connect to {Id}.");
                    }
                }
                catch (Exception ex)
                {
                    //failed
                    if (_credenIOCard != null)
                    {
                        _credenIOCard.Close();
                        _credenIOCard = null;
                    }
                    Connected = false;
                    throw new InvalidOperationException($"Unable to connect to {Id}.");
                }
            }
        }

        public override void Disconnect()
        {
            _credenIOCard?.Close();
            _credenIOCard = null;
            Connected = false;
        }

        public int DigitalRead(int portId)
        {
            byte value = 0;
            CardStatus status = _credenIOCard.ReadPort((byte)portId, ref value);

            if (status != CardStatus.Successful)
            {
                if (status == CardStatus.LinkError)
                    throw new CredenLinkErrorException($"An error occurred when attempting to DigitalRead PortId:{portId} on {Id}." + $" {status}.");
                throw new InvalidOperationException(
                    $"An error occurred when attempting to DigitalRead PortId:{portId} on {Id}." +
                    $" {status}.");
            }

            return value;
        }

        public void DigitalWrite(int portId, int state)
        {
            CardStatus status = _credenIOCard.WritePort((byte)portId, (byte)state);

            if (status != CardStatus.Successful)
            {
                if (status == CardStatus.LinkError)
                    throw new CredenLinkErrorException(
                        $"An error occurred when attempting to DigitalWrite PortId:{portId} on {Id}." +
                        $" {status}.");
                throw new InvalidOperationException(
                    $"An error occurred when attempting to DigitalWrite PortId:{portId} on {Id}." +
                    $" {status}.");
            }
        }

        public int AnalogRead(int pin)
        {
            byte value = 0;
            CardStatus status = _credenIOCard.ReadAnalog((byte)pin, ref value);

            if (status != CardStatus.Successful)
            {
                if (status == CardStatus.LinkError)
                    throw new CredenLinkErrorException(
                    $"An error occurred when attempting to AnalogRead Pin:{pin} on {Id}." +
                    $" {status}.");
                throw new InvalidOperationException(
                    $"An error occurred when attempting to AnalogRead Pin:{pin} on {Id}." +
                    $" {status}.");
            }

            return value;
        }

        public void AnalogWrite(int pin, int value)
        {
            CardStatus status = _credenIOCard.WriteAnalog((byte)pin, (byte)value);

            if (status != CardStatus.Successful)
            {
                if (status == CardStatus.LinkError)
                    throw new CredenLinkErrorException(
                        $"An error occurred when attempting to AnalogWrite Pin:{pin} on {Id}." +
                        $" {status}.");
                throw new InvalidOperationException(
                    $"An error occurred when attempting to AnalogWrite Pin:{pin} on {Id}." +
                    $" {status}.");
            }
        }

    }
}
