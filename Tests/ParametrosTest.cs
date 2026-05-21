using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Tests;

public class ParametrosTest : ITest
{
    private readonly IConfiguration _config;
    private readonly ILogger<ParametrosTest> _logger;
    private readonly ScreenshotService _screenshots;

    public ParametrosTest(IConfiguration config, ILogger<ParametrosTest> logger, ScreenshotService screenshots)
    {
        _config = config;
        _logger = logger;
        _screenshots = screenshots;
    }

    public async Task<List<TestResult>> RunAsync(IWebDriver driver)
    {
        var results = new List<TestResult>();
        var page = new ParametrosPage(driver, _config);

        int totalTests = int.TryParse(_config["TestSettings:TotalParametrosTests"], out var t) ? t : 25;

        // 2 navigation tests fixed; rest split 40/40/20
        int remaining = totalTests - 2;
        int altaCount = (int)Math.Floor(remaining * 0.4);
        int modCount  = (int)Math.Floor(remaining * 0.4);
        int delCount  = remaining - altaCount - modCount;

        // ── BLOQUE 1: Navegación ─────────────────────────────────────────────

        results.Add(await RunCase(driver, "TC-PRM-NAV-01", "Navegar a Parametros por sidebar", "Parametros-Navegacion",
            () =>
            {
                page.NavigateViaSidebar();
                return page.IsParametrosPageLoaded();
            }));

        results.Add(await RunCase(driver, "TC-PRM-NAV-02", "Navegar a Parametros por URL directa", "Parametros-Navegacion",
            () =>
            {
                page.NavigateViaUrl();
                return page.IsParametrosPageLoaded();
            }));

        // Ensure we are on the Parametros page before CRUD
        if (!page.IsParametrosPageLoaded())
            page.NavigateViaUrl();

        // ── BLOQUE 2: ALTA ───────────────────────────────────────────────────

        var createdParams = new List<string>();
        for (int n = 1; n <= altaCount; n++)
        {
            var clave  = $"PARAM_TEST_{n}";
            var valor  = $"valor_auto_{n}";
            var desc   = $"Parametro de prueba {n}";
            var estado = n % 2 == 0 ? "Inactivo" : "Activo";
            var testId = $"TC-PRM-ALTA-{n:D2}";

            results.Add(await RunCase(driver, testId, $"Crear parámetro {clave}", "Parametros-Alta",
                () =>
                {
                    page.ClickNuevo();
                    page.FillForm(clave, valor, desc, estado);
                    page.ClickGuardar();
                    return page.IsParametroInTable(clave);
                }));

            if (results.Last().Passed)
                createdParams.Add(clave);
        }

        // ── BLOQUE 3: MODIFICAR ──────────────────────────────────────────────

        var modTargets = createdParams.Take(modCount).ToList();
        for (int n = 0; n < modTargets.Count; n++)
        {
            var clave  = modTargets[n];
            var valor  = $"valor_modificado_{n + 1}";
            var desc   = $"Modificado automaticamente {n + 1}";
            var estado = n % 2 == 0 ? "Inactivo" : "Activo";
            var testId = $"TC-PRM-MOD-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Modificar parámetro {clave}", "Parametros-Modificar",
                () =>
                {
                    page.ClickEditarByClave(clave);
                    page.FillForm(clave, valor, desc, estado);
                    page.ClickGuardar();
                    return page.IsParametroInTable(clave);
                }));
        }

        // ── BLOQUE 4: ELIMINAR ───────────────────────────────────────────────

        var delTargets = createdParams.Take(delCount).ToList();
        for (int n = 0; n < delTargets.Count; n++)
        {
            var clave  = delTargets[n];
            var testId = $"TC-PRM-DEL-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Eliminar parámetro {clave}", "Parametros-Eliminar",
                () =>
                {
                    page.ClickEliminarByClave(clave);
                    page.ConfirmEliminar();
                    return !page.IsParametroInTable(clave);
                }));
        }

        await Task.CompletedTask;
        return results;
    }

    private async Task<TestResult> RunCase(
        IWebDriver driver,
        string testName,
        string description,
        string category,
        Func<bool> action)
    {
        bool passed = false;
        string message = string.Empty;
        string? screenshotPath = null;

        try
        {
            passed = action();
            message = passed ? "OK" : $"[ASERCION] Resultado inesperado: {description}";
        }
        catch (Exception ex)
        {
            message = ErrorClassifier.Classify(ex);
            _logger.LogWarning("{Test} → {Msg}", testName, message);
        }
        finally
        {
            try { screenshotPath = _screenshots.TakeScreenshot(driver, testName); }
            catch (Exception ex) { _logger.LogWarning("Screenshot failed: {M}", ex.Message); }
        }

        await Task.CompletedTask;
        return new TestResult
        {
            TestName = testName,
            Category = category,
            Passed = passed,
            Message = message,
            ScreenshotPath = screenshotPath,
            ExecutedAt = DateTime.Now
        };
    }
}
