using BaqueanoAutoTest;
using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // Single Chrome instance shared for the entire test run
        services.AddSingleton<IWebDriver>(sp =>
        {
            var options = new ChromeOptions();
            if (string.Equals(config["TestSettings:Headless"], "true", StringComparison.OrdinalIgnoreCase))
                options.AddArgument("--headless=new");
            options.AddArgument("--start-maximized");
            return new ChromeDriver(options);
        });

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<ScreenshotService>();
        services.AddSingleton<HtmlReportService>();
        services.AddSingleton<TestRunner>();

        // Tests run in registration order: Login → Perfiles → Usuarios → Parametros → Contactos
        services.AddTransient<ITest, LoginTest>();
        services.AddTransient<ITest, PerfilesTest>();
        services.AddTransient<ITest, UsuariosTest>();
        services.AddTransient<ITest, ParametrosTest>();
        services.AddTransient<ITest, ContactosTest>();

        services.AddHostedService<Worker>();
    })
    .Build();

// Dispose the WebDriver when the host shuts down
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopped.Register(() =>
{
    try { host.Services.GetRequiredService<IWebDriver>().Quit(); }
    catch { /* already closed */ }
});

await host.RunAsync();
