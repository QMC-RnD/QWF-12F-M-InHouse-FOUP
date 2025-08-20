using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl.Utilities
{
    public struct IntBits
    {
        public IntBits(int initialBitValue)
        {
            Bits = initialBitValue;
        }

        public int Bits { get; private set; }

        public bool this[int index]
        {
            get
            {
                if (index >= 0)
                {
                    return (Bits & (1 << index)) != 0;
                }
                else
                {
                    throw new IndexOutOfRangeException("Index cannot be less than zero.");
                }
            }

            set
            {
                // turn the bit on if value is true; otherwise, turn it off
                if (value)
                {
                    Bits |= 1 << index;
                }
                else
                {
                    Bits &= ~(1 << index);
                }
            }
        }
    }
}
