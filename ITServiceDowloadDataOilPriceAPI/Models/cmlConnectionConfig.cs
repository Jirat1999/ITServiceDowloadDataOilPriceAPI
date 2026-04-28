using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Models
{
    public class cmlConnectionConfig
    {
        public string tServerDB { get; set; } = string.Empty;
        public string tNameDB { get; set; } = string.Empty;
        public string tUser {  get; set; } = string.Empty;
        public string tPassword { get; set; } = string.Empty;
        public bool bIntegratedSecurity { get; set; } = false;
        public bool bEncrypt {  get; set; } = false;
        public bool bTrustServerCertificate { get; set; } = true;
    }
}
