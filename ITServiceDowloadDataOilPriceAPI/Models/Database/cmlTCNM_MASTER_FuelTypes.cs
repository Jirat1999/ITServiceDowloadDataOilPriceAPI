using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models.Database
{
    public class cmlTCNM_MASTER_FuelTypes
    {
        public int nFNFuelTypeId {  get; set; }
        public string tFTCode { get; set; }
        public string tFTName { get; set; }
        public DateTime? dFDCreatedAt { get; set; }
        public DateTime? dFDUpdatedAt { get; set; }
    }
}
