using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ITServiceDowloadDataOilPriceAPI.Models;

namespace ITServiceDowloadDataOilPriceAPI.Class
{
    public class cApiService
    {
        private readonly ILogger<cApiService> oLogger;
        private readonly HttpClient oClient;

        public cApiService(ILogger<cApiService> oLogger, HttpClient oClient)
        {
            this.oLogger = oLogger;
            this.oClient = oClient;
        }

        public async Task<(cmlFuelPriceRoot oData, string tRawJson)> C_GETxOilPriceAsync(CancellationToken poCt)
        {
            string tUrl = cConfig.oApiConfig?.tUrl;
            if (string.IsNullOrEmpty(tUrl))
            {
                oLogger.LogWarning("API URL is missing in configuration.");
                return (null, string.Empty);
            }

            try
            {
                var oResponse = await oClient.GetAsync(tUrl, poCt);
                oResponse.EnsureSuccessStatusCode();

                string tJsonString = await oResponse.Content.ReadAsStringAsync(poCt);
                var oFuelRoot = JsonSerializer.Deserialize<cmlFuelPriceRoot>(tJsonString, 
                    new JsonSerializerOptions{ PropertyNameCaseInsensitive = true });

                if (oFuelRoot?.tStatus == "success" && oFuelRoot.poResponse != null)
                {
                    oLogger.LogInformation(">>> Download successfully. Date from API: {Date}", oFuelRoot.poResponse.tDate);
                    return (oFuelRoot, tJsonString);
                }

                oLogger.LogWarning(">>> API returned unexpected status or empty response.");

                return (null, string.Empty);

            }
            catch (Exception oEx) 
            {
                oLogger.LogError(oEx, ">>> Error occurred while fetching data from API.");
                return (null, string.Empty);
            }
        }
    }
}
