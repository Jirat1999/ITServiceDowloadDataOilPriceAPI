using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dapper;
using ITServiceDowloadDataOilPriceAPI.Models;
using ITServiceDowloadDataOilPriceAPI.Models.Database;
using System.Globalization;

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

        public async Task cSaveToDatabaseAsync(cmlFuelPriceRoot oData)
        {
            string tConnStr = cConnectionHelper.C_GETxConnectionString(cConfig.oConnectionConfig);
            oLogger.LogInformation(">>> Saving to MSSQL Database...");

            using var oConn = new SqlConnection(tConnStr);

            try
            {
                await oConn.OpenAsync();
                using var oTrans = oConn.BeginTransaction();
                int nStationCount = 0, nPriceCount = 0, nUpdateCount = 0;

                long nLogId = await oConn.QuerySingleAsync<long>(cSqlCommands.C_GETxInsertLogStart(), 
                    new { Json = oData.tRawJson }, oTrans);

                DateTime dEffDate = DateTime.Now.Date;
                if (oData.poResponse != null && DateTime.TryParse(oData.poResponse.tDate, new CultureInfo("TH-th"), DateTimeStyles.None, out DateTime dPrase))
                {
                    dEffDate = dPrase;
                }

                var oDbStations = await oConn.QueryAsync(cSqlCommands.C_GETxAllStations(), null, oTrans);
                var oDictStations = oDbStations.ToDictionary(x => ((string)x.FTCode).Trim().ToUpper(), x => (int)x.FNStationId);

                var oDbFuelTypes = await oConn.QueryAsync(cSqlCommands.C_GETxAllFuelTypes(), null, oTrans);
                var oDictFuelTypes = oDbFuelTypes.ToDictionary(x => ((string)x.FTCode).Trim().ToUpper(), x => (int)x.FNFuelTypeId);

                var oDbPrices = await oConn.QueryAsync<cmlTCNM_PRICE_FuelPrices>(cSqlCommands.C_GETxPricesByDate(), new { Date = dEffDate }, oTrans);
                var oDictPrices = oDbPrices.ToDictionary(x => $"{x.nFNStationId}_{x.nFNFuelTypeId}", x => x.cFCPrice);

                var oListUpdateStations = new List<object>();
                var oListUpdateFuelTypes = new List<object>();
                var oListInsertPrices = new List<object>();
                var oListUpdatePrices = new List<object>();
                var oProcessedFuelTypes = new HashSet<string>();

                if (oData.poResponse != null && oData.poResponse.tStations != null)
                {
                    foreach (var oStation in oData.poResponse.tStations)
                    {
                        string tStaCode = oStation.Key.Trim().ToUpper();
                        string tStaName = oStation.Key.Trim().ToUpper();

                        if (!oDictStations.TryGetValue(tStaCode, out int nStationId))
                        {
                            nStationId = await oConn.QuerySingleAsync<int>(cSqlCommands.C_GETxInsertStation(),
                                new { Code = tStaCode, Name = tStaName }, oTrans);
                            oDictStations[tStaCode] = nStationId; // เอาใส่ Memory อัปเดตไว้
                        }
                        else
                        { 
                            oListUpdateStations.Add(new { Code = tStaCode, Name = tStaName });
                        }
                        nStationCount++;

                        if (oStation.Value != null)
                        {
                            foreach (var oFuel in oStation.Value)
                            {
                                string tFuelCode = oFuel.Key.Trim().ToUpper();
                                decimal cPrice = oFuel.Value.cNumericPrice;
                                if (cPrice <= 0) continue;
                                
                            if (!oDictFuelTypes.TryGetValue(tFuelCode, out int nFuelTypeId))
                                {
                                    nFuelTypeId = await oConn.QuerySingleAsync<int>(cSqlCommands.C_GETxInsertFuelType(), 
                                        new { Code = tFuelCode, Name = oFuel.Value.tName }, oTrans);
                                    oDictFuelTypes[tFuelCode] = nFuelTypeId;

                                    oProcessedFuelTypes.Add(tFuelCode);
                                }
                                else
                                {
                                    if (!oProcessedFuelTypes.Contains(tFuelCode))
                                    {
                                        oListUpdateFuelTypes.Add(new { Code = tFuelCode, Name = oFuel.Value.tName });
                                        oProcessedFuelTypes.Add(tFuelCode);
                                    }
                                }

                                string tPriceKey = $"{nStationId}_{nFuelTypeId}";

                                if (oDictPrices.TryGetValue(tPriceKey, out decimal cOldPrice))
                                {                                
                                    if (cOldPrice != cPrice)
                                    {
                                        oListUpdatePrices.Add(new { StationId = nStationId, FuelTypeId = nFuelTypeId, Date = dEffDate, Price = cPrice });
                                        nUpdateCount++;
                                    }
                                }
                                else
                                {
                                    oListInsertPrices.Add(new { StationId = nStationId, FuelTypeId = nFuelTypeId, Date = dEffDate, Price = cPrice });
                                    nPriceCount++;
                                }
                            }
                        }
                    }
                }

                if (oListUpdateStations.Any()) await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateStation(), oListUpdateStations, oTrans);
                if (oListUpdateFuelTypes.Any()) await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateFuelType(), oListUpdateFuelTypes, oTrans);
                if (oListInsertPrices.Any()) await oConn.ExecuteAsync(cSqlCommands.C_GETxInsertPrice(), oListInsertPrices, oTrans);
                if (oListUpdatePrices.Any()) await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdatePrice(), oListUpdatePrices, oTrans);

                await oConn.ExecuteAsync(cSqlCommands.C_GETxUpdateLogEnd(),
                    new { StaCount = nStationCount, PriceCount = nPriceCount, LogId = nLogId }, oTrans);

                oTrans.Commit();
                oLogger.LogInformation(">>> Database save complete! (Stations: {S}, New Prices Inserted: {P}, Prices Updated: {U})",
                    nStationCount, oListInsertPrices.Count, oListUpdatePrices.Count);
            }
            catch (Exception oEx)
            {
                oLogger.LogError(">>> DB Error: {Msg}", oEx.Message);
                await oDbLogHelper.C_SAVxLogErrorAsync(tConnStr, "cSaveToDatabaseAsync", oEx.Message, oEx.StackTrace ?? "");
            }
        }
    }
}
