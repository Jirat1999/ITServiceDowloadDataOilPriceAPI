using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models.Database
{
    public class cmlTCNM_MASTER_Stations
    {
        public int nFNStationId { get; set; }
        public string tFTCode { get; set; }
        public string tFTName { get; set; }
        public DateTime? dFDCreatedAt { get; set; }
        public DateTime? dFDUpdatedAt { get; set;  }
    }
}
