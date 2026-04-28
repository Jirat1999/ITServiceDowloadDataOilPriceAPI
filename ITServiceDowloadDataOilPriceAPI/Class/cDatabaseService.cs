using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dapper;
using ITServiceDowloadDataOilPriceAPI.Models;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public class cDatabaseService
    {
        private readonly ILogger<cDatabaseService> oLogger;

        public cDatabaseService(ILogger<cDatabaseService> oLogger) => this.oLogger = oLogger;

        public async Task cSaveToDatabaseAsync(cmlFuelPriceRoot oData, string tRawJson)
        {
            string tConnStr = $"Server={cConfig.oConnectionConfig.tServerDB};" +
                              $"Database={cConfig.oConnectionConfig.tNameDB};" +
                              $"User Id={cConfig.oConnectionConfig.tUser};" +
                              $"Password={cConfig.oConnectionConfig.tPassword};" +
                              $"Integrated Security={cConfig.oConnectionConfig.bIntegratedSecurity};" +
                              $"Encrypt={cConfig.oConnectionConfig.bEncrypt};" +
                              $"TrustServerCertificate={cConfig.oConnectionConfig.bTrustServerCertificate};";

            oLogger.LogInformation(">>> Saving to MSSQL Database...");

            using var oConn = new SqlConnection(tConnStr);
            await oConn.OpenAsync();
            using var oTrans = oConn.BeginTransaction();
            try
            {
                int nStationCount = 0;
                int nPriceCount = 0;

                string tInsertLogSql = @"
                    INSERT INTO TCNM_LOG_FuelUpdate (FDUpdateStart, FTStatus, FTPriceDataJSON) 
                    VALUES (GETDATE(), 'Processing', @Json);
                    SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

                long nLongId = await oConn.QuerySingleAsync<long>(tInsertLogSql, new { Json = tRawJson }, oTrans);
                DateTime dEffactiveDatedt = DateTime.Now.Date;
                try
                {
                    var oThCulture = new CultureInfo("TH-th");
                    if (DateTime.TryParse(oData.poResponse.tDate, oThCulture, DateTimeStyles.None, out DateTime dParsedDate))
                    {
                        dEffactiveDatedt = dParsedDate;
                    }
                }
                catch { }

                if (oData.poResponse.tStations != null)
                {
                    foreach (var oStation in oData.poResponse.tStations)
                    {
                        string tStationCode = oStation.Key;
                        string tStationName = tStationCode.ToUpper();

                        string tUpsertSations = @"
                            IF NOT EXISTS (SELECT 1 FROM TCNM_MASTER_Stations WHERE FTCode = @Code)
                                INSERT INTO TCNM_MASTER_Stations (FTCode, FTName) VALUES (@Code, @Name);
                            ELSE
                                UPDATE TCNM_MASTER_Stations SET FTName = @Name, FDUpdatedAt = GETDATE() WHERE FTCode = @Code;
                    
                            SELECT FNStationId FROM TCNM_MASTER_Stations WHERE FTCode = @Code;";

                        int nStationId = await oConn.QuerySingleAsync<int>(tUpsertSations, new { Code = tStationCode, Name = tStationName }, oTrans);
                        nStationCount++;

                        if (oStation.Value != null)
                        {
                            foreach (var oFuel in oStation.Value)
                            {
                                string tFuelCode = oFuel.Key;
                                string tFuelName = oFuel.Value.tName;
                                decimal cPrice = oFuel.Value.cNumericPrice;

                                if (cPrice <= 0)
                                {
                                    continue;
                                }

                                string tUpsertFuelType = @"
                                    IF NOT EXISTS (SELECT 1 FROM TCNM_MASTER_FuelTypes WHERE FTCode = @Code)
                                        INSERT INTO TCNM_MASTER_FuelTypes (FTCode, FTName) VALUES (@Code, @Name);
                                    ELSE
                                        UPDATE TCNM_MASTER_FuelTypes SET FTName = @Name, FDUpdatedAt = GETDATE() WHERE FTCode = @Code;
                            
                                    SELECT FNFuelTypeId FROM TCNM_MASTER_FuelTypes WHERE FTCode = @Code;";

                                int nFuelTypeId = await oConn.QuerySingleAsync<int>(tUpsertFuelType, new { Code = tFuelCode, Name = tFuelName }, oTrans);

                                string tUpsertPrice = @"
                                    IF NOT EXISTS (SELECT 1 FROM TCNM_PRICE_FuelPrices WHERE FNStationId = @StationId AND FNFuelTypeId = @FuelTypeId AND FDEffectiveDate = @Date)
                                        INSERT INTO TCNM_PRICE_FuelPrices (FNStationId, FNFuelTypeId, FDEffectiveDate, FCPrice)
                                    VALUES (@StationId, @FuelTypeId, @Date, @Price);
                                    ELSE
                                        UPDATE TCNM_PRICE_FuelPrices SET FCPrice = @Price 
                                        WHERE FNStationId = @StationId AND FNFuelTypeId = @FuelTypeId AND FDEffectiveDate = @Date;";

                                await oConn.ExecuteAsync(tUpsertPrice, new
                                {
                                    StationId = nStationId,
                                    FuelTypeId = nFuelTypeId,
                                    Date = dEffactiveDatedt,
                                    Price = cPrice
                                }, oTrans);

                                nPriceCount++;
                            }
                        }
                    }
                }
                string tUpdateLogSql = @"
                    UPDATE TCNM_LOG_FuelUpdate 
                        SET FDUpdateEnd = GETDATE(), FNStationCount = @StaCount, FNPriceCount = @PriceCount, FTStatus = 'Success', FTMessage = 'Complete' 
                    WHERE FNLogId = @LogId;";
                await oConn.ExecuteAsync(tUpdateLogSql, new { StaCount = nStationCount, PriceCount = nPriceCount, LogId = nLongId }, oTrans);

                oTrans.Commit();
                oLogger.LogInformation(">>> Database save complete! (Stations: {S}, Prices: {P})", nStationCount, nPriceCount);
            }
            catch (Exception oEx)
            {
                if (oConn.State == System.Data.ConnectionState.Open && oTrans != null)
                {
                    try
                    {
                        oTrans.Rollback();
                    }
                    catch { }

                    oLogger.LogError(">>> An error occurred while saving to the DB: {Msg}", oEx.Message);
                    await C_SAVxLogError(tConnStr, "cSaveToDatabaseAsync", oEx.Message, oEx.StackTrace ?? "");
                }
            }
        }

            public async Task C_SAVxLogError(string tConnStr, string tProcess, string tMsg, string tStackTrace)
        {
            if (string.IsNullOrEmpty(tConnStr)) return;
            try
            {
                using var oConnErr = new SqlConnection(tConnStr);
                string tSql = "INSERT INTO TCNM_ERROR_ErrorLogs (FTProcessName, FTErrorMessage, FTStackTrace) VALUES (@Proc, @Msg, @Stack)";
                await oConnErr.ExecuteAsync(tSql, new { Proc = tProcess, Msg = tMsg, Stack = tStackTrace });
            }
            catch (Exception oEx)
            {
                oLogger.LogCritical(oEx, ">>> CRITICAL: Unable to log the error to the TCNM_ERROR_ErrorLogs table!");
            }
        }
    
    }
}
