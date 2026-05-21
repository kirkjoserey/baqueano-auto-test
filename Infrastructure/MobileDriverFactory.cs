using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace BaqueanoAutoTest.Infrastructure;

/// <summary>
/// Crea un ChromeDriver con viewport reducido para pruebas responsive.
/// Acepta un nombre de sección de configuración ("MobileSettings", "TabletSettings", etc.)
/// y aplica dos estrategias en cascada:
///
///   1) EnableMobileEmulation(deviceName) — si se configura un DeviceName válido.
///      Nombres seguros para Chrome 114+: "iPhone 14 Pro Max", "Pixel 7",
///      "Samsung Galaxy S20 Ultra", "iPad Mini", "Surface Pro 7", "Galaxy Fold"
///
///   2) --window-size + --user-agent (fallback automático, siempre funciona)
///      Dispara correctamente los breakpoints CSS de la app responsive.
/// </summary>
public static class MobileDriverFactory
{
    private const string MobileUA =
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/136.0.0.0 Mobile Safari/537.36";

    private const string TabletUA =
        "Mozilla/5.0 (Linux; Android 12; SM-T870) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    /// <param name="sectionName">"MobileSettings" | "TabletSettings" (o cualquier sección compatible)</param>
    public static IWebDriver Create(IConfiguration config, string sectionName = "MobileSettings")
    {
        bool headless = string.Equals(
            config["TestSettings:Headless"], "true",
            StringComparison.OrdinalIgnoreCase);

        int width  = int.TryParse(config[$"{sectionName}:Width"],  out var w) ? Math.Abs(w) : 390;
        int height = int.TryParse(config[$"{sectionName}:Height"], out var h) ? Math.Abs(h) : 844;
        var device = (config[$"{sectionName}:DeviceName"] ?? "").Trim();

        // Estrategia 1: dispositivo con nombre de Chrome DevTools
        if (!string.IsNullOrWhiteSpace(device))
        {
            try { return CreateByDeviceName(device, headless); }
            catch (Exception ex) when (ex is WebDriverException || ex is InvalidOperationException)
            {
                // Nombre no reconocido por esta versión de Chrome → fallback
            }
        }

        // Estrategia 2: tamaño de ventana + User-Agent (siempre disponible)
        bool isTablet = sectionName.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
                     || width >= 600;
        string ua = isTablet ? TabletUA : MobileUA;

        return CreateByWindowSize(width, height, ua, headless);
    }

    private static IWebDriver CreateByDeviceName(string device, bool headless)
    {
        var opts = new ChromeOptions();
        opts.EnableMobileEmulation(device);
        if (headless) opts.AddArgument("--headless=new");
        return new ChromeDriver(opts);   // lanza si el nombre no existe en Chrome
    }

    private static IWebDriver CreateByWindowSize(int width, int height, string ua, bool headless)
    {
        var opts = new ChromeOptions();
        opts.AddArgument($"--window-size={width},{height}");
        opts.AddArgument($"--user-agent={ua}");
        if (headless) opts.AddArgument("--headless=new");
        return new ChromeDriver(opts);
    }
}
