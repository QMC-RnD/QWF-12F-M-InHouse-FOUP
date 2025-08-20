using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FOUPCtrl;

namespace FOUPCtrl.Models
{
    public class MappingResult
    {
        private int _no;
        private string _status;
        private int _thickness;
        private int _position;
        private int _count;

        public int No { get => _no; set => _no = value; }
        public string Status { get => _status; set => _status = value; }
        public int Thickness { get => _thickness; set => _thickness = value; }
        //public int Position { get => _position; set => _position = value; }
        public int Count { get => _count; set => _count = value; }

    }
}
