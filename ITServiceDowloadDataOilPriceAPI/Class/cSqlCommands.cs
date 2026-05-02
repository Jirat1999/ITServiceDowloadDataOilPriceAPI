using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using ITServiceDowloadDataOilPriceAPI.Models.Database;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public static class cSqlCommands
    {
        // ==========================================
        // 1. TCNM_MASTER_Stations (ปั๊มน้ำมัน)
        // ==========================================
        public static string C_GETxGetStationId()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT FNStationId FROM {cmlTable.tTCNM_MASTER_Stations} WHERE FTCode = @Code;");
            return oSql.ToString();
        }

        public static string C_GETxInsertStation()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"INSERT INTO {cmlTable.tTCNM_MASTER_Stations} (FTCode, FTName) VALUES (@Code, @Name);");
            oSql.AppendLine($"SELECT CAST(SCOPE_IDENTITY() as INT);");
            return oSql.ToString();
        }

        public static string C_GETxUpdateStation()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"UPDATE {cmlTable.tTCNM_MASTER_Stations} SET FTName = @Name, FDUpdatedAt = GETDATE() WHERE FTCode = @Code;");
            return oSql.ToString();
        }

        public static string C_GETxAllStations()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT FTCode, FNStationId FROM {cmlTable.tTCNM_MASTER_Stations};");
            return oSql.ToString();
        }

        // ==========================================
        // 2. TCNM_MASTER_FuelTypes (ชนิดน้ำมัน)
        // ==========================================
        public static string C_GETxGetFuelTypeId() 
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT FNFuelTypeId FROM {cmlTable.tTCNM_MASTER_FuelTypes} WHERE FTCode = @Code;");
            return oSql.ToString();
        }

        public static string C_GETxInsertFuelType()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"INSERT INTO {cmlTable.tTCNM_MASTER_FuelTypes} (FTCode, FTName) VALUES (@Code, @Name);");
            oSql.AppendLine($"SELECT CAST(SCOPE_IDENTITY() as INT);");
            return oSql.ToString();
        }

        public static string C_GETxUpdateFuelType()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"UPDATE {cmlTable.tTCNM_MASTER_FuelTypes} SET FTName = @Name, FDUpdatedAt = GETDATE() WHERE FTCode = @Code;");
            return oSql.ToString();
        }

        public static string C_GETxAllFuelTypes()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT FTCode, FNFuelTypeId FROM {cmlTable.tTCNM_MASTER_FuelTypes};");
            return oSql.ToString();
        }

        // ==========================================
        // 3. TCNM_PRICE_FuelPrices (ราคาน้ำมัน)
        // ==========================================
        public static string C_GETxCheckLatestPrice()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT TOP 1");
            oSql.AppendLine($"P.FCPrice AS cFCPrice,");
            oSql.AppendLine($"P.FDEffectiveDate AS dFDEffectiveDate");
            oSql.AppendLine($"FROM {cmlTable.tTCNM_PRICE_FuelPrices} P");
            oSql.AppendLine($"INNER JOIN {cmlTable.tTCNM_MASTER_FuelTypes} F ON P.FNFuelTypeId = F.FNFuelTypeId");
            oSql.AppendLine($"WHERE P.FNStationId = @StationId");
            oSql.AppendLine($"AND F.FTCode = @FuelCode");
            oSql.AppendLine($"ORDER BY P.FDEffectiveDate DESC;");
            return oSql.ToString();
        }

        public static string C_GETxCheckPriceExistsForDate()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT 1 FROM {cmlTable.tTCNM_PRICE_FuelPrices}");
            oSql.AppendLine($"WHERE FNStationId = @StationId AND FNFuelTypeId = @FuelTypeId AND FDEffectiveDate = @Date;");
            return oSql.ToString();
        }

        public static string C_GETxInsertPrice()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"INSERT INTO {cmlTable.tTCNM_PRICE_FuelPrices} (FNStationId, FNFuelTypeId, FDEffectiveDate, FCPrice)");
            oSql.AppendLine($"VALUES (@StationId, @FuelTypeId, @Date, @Price);");
            return oSql.ToString();
        }

        public static string C_GETxUpdatePrice()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"UPDATE {cmlTable.tTCNM_PRICE_FuelPrices} SET FCPrice = @Price");
            oSql.AppendLine($"WHERE FNStationId = @StationId AND FNFuelTypeId = @FuelTypeId AND FDEffectiveDate = @Date;");
            return oSql.ToString();
        }

        public static string C_GETxPricesByDate()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"SELECT FNStationId AS nFNStationId, FNFuelTypeId AS nFNFuelTypeId, FCPrice AS cFCPrice");
            oSql.AppendLine($"FROM {cmlTable.tTCNM_PRICE_FuelPrices}");
            oSql.AppendLine($"WHERE CAST(FDEffectiveDate AS DATE) = CAST(@Date AS DATE);");
            return oSql.ToString();
        }

        // ==========================================
        // 4. Logs (บันทึกการทำงาน)
        // ==========================================
        public static string C_GETxInsertLogStart()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"INSERT INTO {cmlTable.tTCNM_LOG_FuelUpdate} (FDUpdateStart, FTStatus, FTPriceDataJSON)");
            oSql.AppendLine($"VALUES (GETDATE(), 'Processing', @Json);");
            oSql.AppendLine($"SELECT CAST(SCOPE_IDENTITY() as BIGINT);");
            return oSql.ToString();
        }

        public static string C_GETxUpdateLogEnd()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"UPDATE {cmlTable.tTCNM_LOG_FuelUpdate}");
            oSql.AppendLine($"SET FDUpdateEnd = GETDATE(), FNStationCount = @StaCount, FNPriceCount = @PriceCount, FTStatus = 'Success', FTMessage = 'Complete'");
            oSql.AppendLine($"WHERE FNLogId = @LogId;");
            return oSql.ToString();
        }

        public static string C_GETxInsertErrorLogs()
        {
            StringBuilder oSql = new StringBuilder();
            oSql.AppendLine($"INSERT INTO {cmlTable.tTCNM_ERROR_ErrorLogs} (FTProcessName, FTErrorMessage, FTStackTrace)");
            oSql.AppendLine($"VALUES (@Proc, @Msg, @Stack);");
            return oSql.ToString();
        }
    }
}
