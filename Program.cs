using Fact.BatchCleaner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data;
using MoE.Commercial.Data.Db2;
using System.Net.Http;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Register config-bound settings using Options pattern
        services.Configure<Db2Settings>(context.Configuration.GetSection("Db2"));

        // Register services
        services.AddHttpClient<CycleCleaner>();

        services.AddTransient<ICycleCleaner>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var dataProvider = sp.GetRequiredService<IDataProvider>();
            var logger = sp.GetRequiredService<ILogger<CycleCleaner>>();
            var xmlBuilder = sp.GetRequiredService<IXmlMessageBuilder>();
            var db2Options = sp.GetRequiredService<IOptions<Db2Settings>>();

            return new CycleCleaner(dataProvider, logger, xmlBuilder, db2Options, httpClient);
        });
        services.AddTransient<IDataProvider, Db2OdbcDataProvider>();
        services.AddSingleton<IXmlMessageBuilder, XmlMessageBuilder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var app = services.GetRequiredService<ICycleCleaner>();
await app.Run();


