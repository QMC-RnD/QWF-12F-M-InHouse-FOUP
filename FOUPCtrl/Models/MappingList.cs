using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FOUPCtrl;

namespace FOUPCtrl.Models
{
    public class MappingList
    {
        private List<int> _mappingAbsRawData;
        private List<int> _mappingIORawData;
        private int _slotCount = 25;
        private int _totalDataCount;
        public List<int> MappingAbsRawData { get => _mappingAbsRawData; set => _mappingAbsRawData = value; }
        public List<int> MappingIORawData { get => _mappingAbsRawData; set => _mappingAbsRawData = value; }
        public int TotalDataCount { get => _totalDataCount; set => _totalDataCount = value; }


        public List<List<int>> RawDataToWaferData(List<int> Source)
        {
            /*Split data into "25" individual list for specific slot
             return -> List<List<int>>(25)*/
            int batches = (int)Math.Ceiling(((double)MappingIORawData.Count / (double)_slotCount));
            return Source.Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / batches).Select(x => x.Select(v => v.Value).ToList()).ToList();

        }

        public List<int> WaferDataCount(List<List<int>> Source)
        {
            /* Return the number of detected wafer counts*/
            List<int> waferStatus = new List<int>(25);
            foreach (List<int> SourceData in Source)
            {
                var counts = SourceData.Where(x => x > 0).Count();

                waferStatus.Add(counts);
            }
            return waferStatus;
        }
        //public List<int> WaferCount()
        //{

        //    //var waferData = RawDataToWaferData(MappingRawData);
        //    //var waferCount = WaferDataCount(waferData);
        //    //MappingDataCount = waferCount;
        //    //return waferCount;

        //}


    }


}
