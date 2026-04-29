using System;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ITServiceDowloadDataOilPriceAPI.Class;

namespace ITServiceDowloadDataOilPriceAPI
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> oLogger;
        private readonly cApiService oApiService;
        private readonly cDatabaseService oDBService;

        public Worker(ILogger<Worker> oLogger, cApiService oApiService, cDatabaseService oDBService)
        {
            this.oLogger = oLogger;
            this.oApiService = oApiService;
            this.oDBService = oDBService;
        }
        protected override async Task ExecuteAsync(CancellationToken oStoppingToken)
        {
            while(!oStoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool bScheduleTrigger = (cConfig.oSettingConfig?.tMode == "OilPrice");

                    if(bScheduleTrigger || (cConfig.oSettingConfig?.bManualTrigger ?? false))
                    {
                        oLogger.LogInformation(">> Triggered work process.");

                        var (oData, tJon) = await oApiService.C_GETxOilPriceAsync(oStoppingToken);

                        if (oData != null && !string.IsNullOrEmpty(tJon))
                        {
                            await oDBService.cSaveToDatabaseAsync(oData, tJon);
                        }
                        else
                        {
                            oLogger.LogWarning(">>> Skip saving: API returned null or empty data.");
                        }

                        int nSafeInterval = cConfig.oSettingConfig?.nIntervalMinutes <= 0 ? 1 : cConfig.oSettingConfig.nIntervalMinutes;
                        oLogger.LogInformation(">> Complete work. Waiting for the next round in {Mins} minutes...\n", nSafeInterval);

                        await Task.Delay(TimeSpan.FromMinutes(nSafeInterval), oStoppingToken);
                    }else
                    {
                        await Task.Delay(5000, oStoppingToken);
                    }
                } 
                catch (Exception oEx) 
                {
                    oLogger.LogError(oEx, ">> Error occurred while executing the worker.");

                    string tConnStr = cConnectionHelper.C_GETxConnectionString(cConfig.oConnectionConfig);

                    oLogger.LogInformation(">>> CHECK CONN STRING: {conn}", tConnStr);

                    var oLogHelper = new cDbLogHelper(oLogger);

                    await oLogHelper.C_SAVxLogErrorAsync(tConnStr, "Worker_ExecuteAsync",oEx.Message,oEx.StackTrace ?? "");

                    await Task.Delay(5000,oStoppingToken);
                }
            }
        }
    }
}
