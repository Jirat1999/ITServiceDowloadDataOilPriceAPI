using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models.Database
{
    public class cmlTCNM_LOG_FuelUpdate
    {
        public long nFNLogId {  get; set; }
        public DateTime dFDUpdateStart { get; set; }
        public DateTime? dFDUpdateEnd { get; set; }
        public int? nFNStationCount { get; set; }
        public int? nFNPriceCount { get; set; }
        public string tFTStatus { get; set; }
        public string tFTMeassage { get; set; }
        public string tFTPriceDataJSON { get; set; }
    }
}
