using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public class cDbLogHelper
    {
        private readonly ILogger oLogger;
        public cDbLogHelper(ILogger logger) => oLogger = logger;

        public async Task C_SAVxLogErrorAsync(string tConnStr, string tProcess, string tMsg, string tStackTrace)
        {
            if (string.IsNullOrEmpty(tConnStr)) return;

            try
            {
                using var oConnErr = new SqlConnection(tConnStr);
                await oConnErr.ExecuteAsync(cSqlCommands.C_GETxInsertErrorLogs(), new { Proc = tProcess, Msg = tMsg, Stack = tStackTrace });
            }
            catch (Exception oEx)
            {
                oLogger.LogCritical(oEx, ">>> CRITICAL: Unable to log the error to the TCNM_ERROR_ErrorLogs table!");
            }
        }
    }
}
