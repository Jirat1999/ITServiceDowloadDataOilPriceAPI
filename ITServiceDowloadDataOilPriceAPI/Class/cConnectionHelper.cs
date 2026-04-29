using ITServiceDowloadDataOilPriceAPI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public class cConnectionHelper
    {
        public static string C_GETxConnectionString(cmlConnectionConfig oConfig)
        {
            if (oConfig == null) return string.Empty;

            string tConnStr = $"Server={oConfig.tServerDB};" +
                              $"Database={oConfig.tNameDB};" +
                              $"User Id={oConfig.tUser};" +
                              $"Password={oConfig.tPassword};" +
                              $"Integrated Security={oConfig.bIntegratedSecurity};" +
                              $"Encrypt={oConfig.bEncrypt};" +
                              $"TrustServerCertificate={oConfig.bTrustServerCertificate};";

            return tConnStr;
        }
    }
}
