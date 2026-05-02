using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models.Database
{
    public class cmlTCNM_PRICE_FuelPrices
    {
        public int nFNStationId { get; set; }
        public int nFNFuelTypeId { get; set; }
        public DateTime dFDEffactiveDate { get; set; }
        public decimal cFCPrice { get; set; }
    }
}
