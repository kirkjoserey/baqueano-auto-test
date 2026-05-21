using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Infrastructure;

public class TestRunner
{
    private readonly IConfiguration _config;
    private readonly ILogger<TestRunner> _logger;
    private readonly DatabaseService _db;
    private readonly ScreenshotService _screenshots;
    private readonly HtmlReportService _htmlReport;
    private readonly IWebDriver _driver;
    private readonly IEnumerable<ITest> _tests;
    private readonly IServiceProvider _sp;

    public TestRunner(
        IConfiguration config,
        ILogger<TestRunner> logger,
        DatabaseService db,
        ScreenshotService screenshots,
        HtmlReportService htmlReport,
        IWebDriver driver,
        IEnumerable<ITest> tests,
        IServiceProvider sp)
    {
        _config      = config;
        _logger      = logger;
        _db          = db;
        _screenshots = screenshots;
        _htmlReport  = htmlReport;
        _driver      = driver;
        _tests       = tests;
        _sp          = sp;
    }

    public async Task RunAllAsync()
    {
        // ── 1. Limpiar entorno ────────────────────────────────────────────────
        _logger.LogInformation("Limpiando base de datos y carpeta de capturas...");
        await _db.InitializeAsync();
        await _db.ClearAllAsync();
        _screenshots.ClearScreenshots();
        _logger.LogInformation("Entorno limpio. Iniciando suite de tests.");

        var allResults = new List<TestResult>();

        // ── 2. Fase DESKTOP ───────────────────────────────────────────────────
        _logger.LogInformation("━━━━━━ Fase DESKTOP (pantalla completa) ━━━━━━");
        await RunPhaseAsync("DESKTOP", _driver, _tests, allResults, prefix: string.Empty);

        // ── 3. Fase TABLET (si habilitada) ────────────────────────────────────
        await RunViewportPhaseAsync("TabletSettings", "TABLET", "TAB-", allResults);

        // ── 4. Fase MÓVIL (si habilitada) ─────────────────────────────────────
        await RunViewportPhaseAsync("MobileSettings", "MOBILE", "MOB-", allResults);

        // ── 5. Resumen global ─────────────────────────────────────────────────
        int total  = allResults.Count;
        int pass   = allResults.Count(r => r.Passed);
        int fail   = total - pass;
        int dTotal = allResults.Count(r => !r.TestName.StartsWith("TAB-") && !r.TestName.StartsWith("MOB-"));
        int tTotal = allResults.Count(r =>  r.TestName.StartsWith("TAB-"));
        int mTotal = allResults.Count(r =>  r.TestName.StartsWith("MOB-"));

        _logger.LogInformation("══════════════════════════════════════════════════════════");
        _logger.LogInformation("RESULTADO FINAL  Total: {T} | PASS: {P} | FAIL: {F}",
            total, pass, fail);
        _logger.LogInformation("  🖥 Desktop: {D}  |  📟 Tablet: {T2}  |  📱 Móvil: {M}",
            dTotal, tTotal, mTotal);
        _logger.LogInformation("══════════════════════════════════════════════════════════");

        // ── 6. Generar reporte HTML ───────────────────────────────────────────
        try
        {
            var reportPath = await _htmlReport.GenerateAsync(allResults);
            _logger.LogInformation("Reporte disponible en: {Path}", reportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar el reporte HTML");
        }
    }

    // ── Inicia una fase de viewport (Tablet o Mobile) ─────────────────────────
    private async Task RunViewportPhaseAsync(
        string section, string phaseName, string prefix,
        List<TestResult> allResults)
    {
        bool enabled = string.Equals(
            _config[$"{section}:Enabled"], "true",
            StringComparison.OrdinalIgnoreCase);

        if (!enabled)
        {
            _logger.LogInformation("Fase {Phase} deshabilitada ({Section}:Enabled = false).",
                phaseName, section);
            return;
        }

        _logger.LogInformation("━━━━━━ Fase {Phase} ({Section}) ━━━━━━", phaseName, section);
        IWebDriver? driver = null;
        try
        {
            driver = MobileDriverFactory.Create(_config, section);
            int width = int.TryParse(_config[$"{section}:Width"], out var w) ? w : 0;
            _logger.LogInformation("Driver {Phase} listo — viewport ~{W}px.", phaseName, width);

            var viewportConfig = new MobileConfigurationProxy(_config, section);
            var viewportTests  = CreateViewportTests(viewportConfig);

            await RunPhaseAsync(phaseName, driver, viewportTests, allResults, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la fase {Phase}.", phaseName);
        }
        finally
        {
            if (driver != null)
            {
                try { driver.Quit(); } catch { }
                _logger.LogInformation("Driver {Phase} cerrado.", phaseName);
            }
        }
    }

    // ── Ejecutar todos los tests de una fase ──────────────────────────────────
    private async Task RunPhaseAsync(
        string phaseName,
        IWebDriver driver,
        IEnumerable<ITest> tests,
        List<TestResult> allResults,
        string prefix)
    {
        int total = 0, pass = 0, fail = 0;

        foreach (var test in tests)
        {
            List<TestResult> results;
            try
            {
                results = await test.RunAsync(driver);
            }
            catch (Exception ex)
            {
                var testName = test.GetType().Name;
                _logger.LogError(ex, "Excepción no controlada en {Test}", testName);

                string? screenshotPath = null;
                try { screenshotPath = _screenshots.TakeScreenshot(driver, $"{prefix}{testName}_CRASH"); }
                catch { }

                results = new List<TestResult>
                {
                    new()
                    {
                        TestName       = prefix + testName,
                        Category       = phaseName + "-Error",
                        Passed         = false,
                        Message        = ex.Message,
                        ScreenshotPath = screenshotPath,
                        ExecutedAt     = DateTime.Now
                    }
                };
            }

            foreach (var r in results)
            {
                if (!string.IsNullOrEmpty(prefix) && !r.TestName.StartsWith(prefix))
                    r.TestName = prefix + r.TestName;

                total++;
                if (r.Passed) pass++; else fail++;

                _logger.LogInformation("[{Status}][{Phase}] {TestName} — {Message}",
                    r.Passed ? "PASS" : "FAIL", phaseName, r.TestName, r.Message);

                await _db.SaveResultAsync(r);
                allResults.Add(r);
            }
        }

        _logger.LogInformation("◀  {Phase} — Total: {T} | PASS: {P} | FAIL: {F}",
            phaseName, total, pass, fail);
    }

    // ── Crear tests con config de viewport ───────────────────────────────────
    private IEnumerable<ITest> CreateViewportTests(IConfiguration viewportConfig)
    {
        return _tests
            .Select(t => t.GetType())
            .Select(type => (ITest)ActivatorUtilities.CreateInstance(_sp, type, viewportConfig));
    }
}
