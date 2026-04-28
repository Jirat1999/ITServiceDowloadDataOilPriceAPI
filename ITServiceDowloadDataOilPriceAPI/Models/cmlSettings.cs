using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models
{
    public class cmlSettings
    {
        public string tMode {  get; set; } = string.Empty;
        public int nIntervalMinutes {  get; set; }
        public bool bManualTrigger { get; set; }
    }
}
