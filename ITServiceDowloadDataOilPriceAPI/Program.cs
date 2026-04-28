using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using ITServiceDowloadDataOilPriceAPI;
using ITServiceDowloadDataOilPriceAPI.Class;
using ITServiceDowloadDataOilPriceAPI.Models;

var builder = Host.CreateApplicationBuilder(args);

var oConfig = builder.Configuration;
cConfig.oSettingConfig = oConfig.GetSection("oSettingConfig").Get<cmlSettings>() ?? new cmlSettings();
cConfig.oConnectionConfig = oConfig.GetSection("ConnectionConfig").Get<cmlConnectionConfig>() ?? new cmlConnectionConfig();
cConfig.oApiConfig = oConfig.GetSection("ApiConfig").Get<cmlApiConfig>() ?? new cmlApiConfig();

builder.Services.AddHttpClient();
builder.Services.AddTransient<cApiService>();
builder.Services.AddTransient<cDatabaseService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
