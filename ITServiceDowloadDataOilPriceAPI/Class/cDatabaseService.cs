using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dapper;
using ITServiceDowloadDataOilPriceAPI.Models;
using ITServiceDowloadDataOilPriceAPI.Models.Database;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public class cDatabaseService
    {
        private readonly ILogger<cDatabaseService> oLogger;
        private readonly cDbLogHelper oDbLogHelper;

        public cDatabaseService(ILogger<cDatabaseService> oLogger)
        {
            this.oLogger = oLogger;
            this.oDbLogHelper = new cDbLogHelper(oLogger);
        }

        public async Task cSaveToDatabaseAsync(cmlFuelPriceRoot oData, string tRawJson)
        {
            string tConnStr = cConnectionHelper.C_GETxConnectionString(cConfig.oConnectionConfig);
            oLogger.LogInformation(">>> Saving to MSSQL Database...");

            using var oConn = new SqlConnection(tConnStr);
            await oConn.OpenAsync();

            try
            {
                using var oTrans = oConn.BeginTransaction();

                int nStationCount = 0, nPriceCount = 0, nUpdateCount = 0;

                long nLongId = await oConn.QuerySingleAsync<long>(cSqlCommands.C_GETxInsertLogStart(), new { Json = tRawJson }, oTrans);

                DateTime dEffDate = DateTime.Now.Date;
                if (DateTime.TryParse(oData.poResponse.tDate, new CultureInfo("TH-th"), DateTimeStyles.None, out DateTime dPrase))
                {
                    dEffDate = dPrase;
                }

                if (oData.poResponse.tStations != null)
                {
                    foreach(var oStation in oData.poResponse.tStations)
                    {
                        string tStaCode = oStation.Key.ToUpper();
                        string tStaName = oStation.Key.ToUpper();

                        int? nStationId = await oConn.QueryFirstOrDefaultAsync<int>(cSqlCommands.C_GETxGetStationId(), new { Code = tStaCode }, oTrans);
                        if (nStationId == null || nStationId == 0)
                        {
                            nStationId = await oConn.QuerySingleAsync<int>(cSqlCommands.C_GETxInsertStation(), new { Code = tStaCode, Name = tStaName }, oTrans);
                        }
                        else
                        {
                            await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateStation(), new { Code = tStaCode, Name = tStaName }, oTrans);
                        }
                        nStationCount++;

                        if(oStation.Value != null)
                        {
                            foreach (var oFuel in oStation.Value)
                            {
                                decimal CPrice = oFuel.Value.cNumericPrice;
                                if (CPrice <= 0) continue;
                                nPriceCount++;

                                var oLastPrice = await oConn.QueryFirstOrDefaultAsync<cmlTCNM_PRICE_FuelPrices>(
                                    cSqlCommands.C_GETxCheckLatestPrice(),
                                    new { StationId = nStationId, FuelCode = oFuel.Key }, oTrans);

                                if (oLastPrice != null)
                                {
                                    if (oLastPrice.cFCPeice == CPrice && oLastPrice.dFDEffactiveDate.Date == dEffDate.Date)
                                    {
                                        continue;
                                    }
                                }

                                nUpdateCount++;

                                int? nFuelTypeId = await oConn.QueryFirstOrDefaultAsync<int?>(cSqlCommands.C_GETxGetFuelTypeId(), 
                                    new { Code = oFuel.Key }, oTrans);
                                if (nFuelTypeId == null || nFuelTypeId == 0)
                                {
                                    nFuelTypeId = await oConn.QuerySingleAsync<int>(cSqlCommands.C_GETxInsertFuelType(),
                                        new { Code = oFuel.Key, Name = oFuel.Value.tName }, oTrans);
                                }
                                else
                                {
                                    await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateFuelType(), new { Code = oFuel.Key, Name = oFuel.Value.tName }, oTrans);
                                }

                                int? nPriceExists = await oConn.QueryFirstOrDefaultAsync<int?>(cSqlCommands.C_GETxCheckPriceExistsForDate(),
                                    new { StationId = nStationId, FuelTypeId = nFuelTypeId, Date = dEffDate }, oTrans);

                                if (nPriceExists == null)
                                {
                                    await oConn.ExecuteAsync(cSqlCommands.C_GETxInsertPrice(),
                                        new { StationId = nStationId, FuelTypeId = nFuelTypeId, Date = dEffDate, Price = CPrice }, oTrans);
                                }
                                else
                                {
                                    await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdatePrice(),
                                        new { StationId = nStationId, FuelTypeId = nFuelTypeId, Date = dEffDate, Price = CPrice }, oTrans);
                                }
                            }       
                        }
                    }
                }

                await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateLogEnd(),
                    new { StaCount = nStationCount, PriceCount = nPriceCount, LogId =  nLongId}, oTrans);

                oTrans.Commit();
                oLogger.LogInformation(">>> Database save complete!");
            }
            catch (Exception oEx)
            {
                oLogger.LogError(">>> DB Error: {Msg}", oEx.Message);
                await oDbLogHelper.C_SAVxLogErrorAsync(tConnStr, "cSaveToDatabaseAsync", oEx.Message, oEx.StackTrace ?? "");
            }
        }
    }    
}
