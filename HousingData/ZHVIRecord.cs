using System.Collections.Generic;

namespace HousingData
{
    public class ZHVIRecord
    {
        public string RegionName { get; set; }
        public Dictionary<string, decimal> DateValues { get; set; }

        public ZHVIRecord()
        {
            DateValues = new Dictionary<string, decimal>();
        }
    }
}