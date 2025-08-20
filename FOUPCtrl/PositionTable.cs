using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOUPCtrl
{
    public class PositionTable
    {
        public string Name { get; set; } = "Default";

        public double MapStartPositionMm
        {
            get { return _mapStartPositionMm; }
            set
            {
                // Enforce FOUP door limits: cannot be above -5 (closer to 0) or below -1620
                if (value > -5)
                {
                    _mapStartPositionMm = -5;  // Clamp to shallowest allowed position
                }
                else if (value < -1620)
                {
                    _mapStartPositionMm = -1620;  // Clamp to deepest allowed position
                }
                else
                {
                    _mapStartPositionMm = value;  // Within valid range
                }
            }
        }

        public double MapEndPositionMm
        {
            get { return _mapEndPositionMm; }
            set
            {
                // Enforce FOUP door limits: cannot be above -5 (closer to 0) or below -1620
                if (value > -5)
                {
                    _mapEndPositionMm = -5;  // Clamp to shallowest allowed position
                }
                else if (value < -1620)
                {
                    _mapEndPositionMm = -1620;  // Clamp to deepest allowed position
                }
                else
                {
                    _mapEndPositionMm = value;  // Within valid range (allows early stopping)
                }
            }
        }

        private double _mapStartPositionMm = -5;
        private double _mapEndPositionMm = -1620;

        public PositionTable() { }

        public PositionTable(string name)
        {
            Name = name;
        }
    }
}
