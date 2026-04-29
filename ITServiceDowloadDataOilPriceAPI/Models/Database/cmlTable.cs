using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models.Database
{
    public class cmlTable
    {
        // Master Data
        public const string tTCNM_MASTER_Stations = "TCNM_MASTER_Stations";
        public const string tTCNM_MASTER_FuelTypes = "TCNM_MASTER_FuelTypes";

        // Transaction Data
        public const string tTCNM_PRICE_FuelPrices = "TCNM_PRICE_FuelPrices";

        // Log Data
        public const string tTCNM_LOG_FuelUpdate = "TCNM_LOG_FuelUpdate";
        public const string tTCNM_ERROR_ErrorLogs = "TCNM_ERROR_ErrorLogs";
    }
}
