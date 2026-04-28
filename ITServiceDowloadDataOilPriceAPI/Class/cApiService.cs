using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly IHttpClientFactory oHttpClientFactory;

        public cApiService(ILogger<cApiService> oLogger, IHttpClientFactory oHttpClientFactory)
        {
            this.oLogger = oLogger;
            this.oHttpClientFactory = oHttpClientFactory;
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
                using var oClient = oHttpClientFactory.CreateClient();
                var oResponse = await oClient.GetAsync(tUrl, poCt);
                oResponse.EnsureSuccessStatusCode();

                string tJsonString = await oResponse.Content.ReadAsStringAsync(poCt);
                var oFuelRoot = JsonSerializer.Deserialize<cmlFuelPriceRoot>(tJsonString, new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });

                if (oFuelRoot?.tStatus == "success" && oFuelRoot.poResponse != null)
                {
                    string tFormattedJson = JsonSerializer.Serialize(oFuelRoot, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    oLogger.LogInformation(">>> Download successfully. Date from API: {Date}", oFuelRoot.poResponse.tDate);
                    return (oFuelRoot, tFormattedJson);
                }

                oLogger.LogWarning(">>> API returned unexpected status or empty response.");

                // เส้นทางที่ 3: ถ้าดึงข้อมูลได้ แต่ API ตอบกลับมาว่าไม่สำเร็จ (จุดนี้แหละครับที่มักจะลืมกัน)
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
