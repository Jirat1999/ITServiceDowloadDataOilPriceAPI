using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ITServiceDowloadDataOilPriceAPI.Models
{
    public class cmlFuelPriceRoot
    {
        [JsonPropertyName("status")]
        public string tStatus { get; set; }
        [JsonPropertyName("response")]
        public cmlFuelData poResponse { get; set; }
        [JsonIgnore]
        public string tRawJson { get; set; }
    }

    public class cmlFuelData
    {
        [JsonPropertyName("date")]
        public string tDate { get; set; }
        [JsonPropertyName("stations")]
        public Dictionary<string, Dictionary<string, cmlFuelType>> tStations { get; set; }
    }

    public class cmlFuelType
    {
        [JsonPropertyName ("name")]
        public string tName { get; set; }
        [JsonPropertyName("price")]
        public JsonElement oRawPrice { get; set; }
        [JsonIgnore]
        public decimal cNumericPrice
        {
            get
            {
                // ถ้าเป็นตัวเลขปกติ
                if (oRawPrice.ValueKind == JsonValueKind.Number)
                    return oRawPrice.GetDecimal();

                // ถ้าเป็น String (เช่น "39.50")
                if (oRawPrice.ValueKind == JsonValueKind.String)
                {
                    string tStrValue = oRawPrice.GetString();
                    if (decimal.TryParse(tStrValue, out decimal nParsed))
                        return nParsed;
                }

                // ถ้า API ส่งมาว่างๆ, null หรือพัง ให้มีค่าเป็น 0
                return 0m;
            }
        }
    }
}
