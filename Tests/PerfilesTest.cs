using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Tests;

public class PerfilesTest : ITest
{
    private readonly IConfiguration _config;
    private readonly ILogger<PerfilesTest> _logger;
    private readonly ScreenshotService _screenshots;

    public PerfilesTest(IConfiguration config, ILogger<PerfilesTest> logger, ScreenshotService screenshots)
    {
        _config = config;
        _logger = logger;
        _screenshots = screenshots;
    }

    public async Task<List<TestResult>> RunAsync(IWebDriver driver)
    {
        var results = new List<TestResult>();
        var page = new PerfilesPage(driver, _config);

        int totalTests = int.TryParse(_config["TestSettings:TotalPerfilesTests"], out var t) ? t : 40;

        // Fixed: 2 navigation tests. Remaining split 40/40/20 across ALTA/MOD/DEL.
        int remaining = totalTests - 2;
        int altaCount = (int)Math.Floor(remaining * 0.4);
        int modCount = (int)Math.Floor(remaining * 0.4);
        int delCount = remaining - altaCount - modCount;

        // ── BLOQUE 1: Navegación ─────────────────────────────────────────────

        results.Add(await RunCase(driver, "TC-PERF-NAV-01", "Navegar a Perfiles por sidebar", "Perfiles-Navegacion",
            () =>
            {
                page.NavigateViaSidebar();
                return page.IsPerfilesPageLoaded();
            }));

        results.Add(await RunCase(driver, "TC-PERF-NAV-02", "Navegar a Perfiles por Dashboard", "Perfiles-Navegacion",
            () =>
            {
                page.NavigateViaDashboardLink();
                return page.IsPerfilesPageLoaded();
            }));

        // Ensure we are on the Perfiles page before CRUD
        if (!page.IsPerfilesPageLoaded())
            page.NavigateViaSidebar();

        // ── BLOQUE 2: ALTA ───────────────────────────────────────────────────

        var createdProfiles = new List<string>();
        for (int n = 1; n <= altaCount; n++)
        {
            var nombre = $"PerfilTest_{n}";
            var desc = $"Descripcion automatica {n}";
            var estado = n % 2 == 0 ? "Inactivo" : "Activo";
            var testId = $"TC-PERF-ALTA-{n:D2}";

            results.Add(await RunCase(driver, testId, $"Crear perfil {nombre}", "Perfiles-Alta",
                () =>
                {
                    page.ClickNuevo();
                    page.FillForm(nombre, desc, estado);
                    page.ClickGuardar();
                    return page.IsProfileInTable(nombre);
                }));

            if (results.Last().Passed)
                createdProfiles.Add(nombre);
        }

        // ── BLOQUE 3: MODIFICAR ──────────────────────────────────────────────

        var modTargets = createdProfiles.Take(modCount).ToList();
        for (int n = 0; n < modTargets.Count; n++)
        {
            var nombre = modTargets[n];
            var desc = $"Modificado {n + 1}";
            var estado = n % 2 == 0 ? "Inactivo" : "Activo";
            var testId = $"TC-PERF-MOD-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Modificar perfil {nombre}", "Perfiles-Modificar",
                () =>
                {
                    page.ClickEditarByNombre(nombre);
                    page.FillForm(nombre, desc, estado);
                    page.ClickGuardar();
                    return page.IsProfileInTable(nombre);
                }));
        }

        // ── BLOQUE 4: ELIMINAR ───────────────────────────────────────────────

        var delTargets = createdProfiles.Take(delCount).ToList();
        for (int n = 0; n < delTargets.Count; n++)
        {
            var nombre = delTargets[n];
            var testId = $"TC-PERF-DEL-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Eliminar perfil {nombre}", "Perfiles-Eliminar",
                () =>
                {
                    page.ClickEliminarByNombre(nombre);
                    page.ConfirmEliminar();
                    return !page.IsProfileInTable(nombre);
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
