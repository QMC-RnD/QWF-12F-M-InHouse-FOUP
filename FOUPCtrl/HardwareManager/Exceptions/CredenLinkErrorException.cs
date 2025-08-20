using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.HardwareManager.Exceptions
{
    public class CredenLinkErrorException : Exception
    {
        public CredenLinkErrorException()
        {
        }

        public CredenLinkErrorException(string message)
            : base(message)
        {
        }

        public CredenLinkErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
