using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Tests;

public class ContactosTest : ITest
{
    private readonly IConfiguration _config;
    private readonly ILogger<ContactosTest> _logger;
    private readonly ScreenshotService _screenshots;

    public ContactosTest(IConfiguration config, ILogger<ContactosTest> logger, ScreenshotService screenshots)
    {
        _config      = config;
        _logger      = logger;
        _screenshots = screenshots;
    }

    public async Task<List<TestResult>> RunAsync(IWebDriver driver)
    {
        var results = new List<TestResult>();
        var page    = new ContactosPage(driver, _config);

        // Configurables en appsettings.json
        int maxStateTests = int.TryParse(_config["TestSettings:ContactosStateTests"],  out var s) ? s : 8;
        int deleteCount   = int.TryParse(_config["TestSettings:ContactosDeleteCount"], out var d) ? d : 5;

        // ── BLOQUE 1: Navegación ─────────────────────────────────────────────

        results.Add(await RunCase(driver, "TC-CTT-NAV-01", "Navegar a Contactos por sidebar", "Contactos-Navegacion",
            () => { page.NavigateViaSidebar(); return page.IsContactosPageLoaded(); }));

        results.Add(await RunCase(driver, "TC-CTT-NAV-02", "Navegar a Contactos por URL directa", "Contactos-Navegacion",
            () => { page.NavigateViaUrl(); return page.IsContactosPageLoaded(); }));

        if (!page.IsContactosPageLoaded())
            page.NavigateViaUrl();

        // Leer cuántos contactos hay realmente en la tabla
        int available = page.GetContactCount();
        _logger.LogInformation("Contactos disponibles en tabla: {N}", available);

        if (available == 0)
        {
            _logger.LogWarning("No hay contactos en la tabla — se omiten pruebas de estado y borrado.");
            await Task.CompletedTask;
            return results;
        }

        // ── BLOQUE 2: Cambio de estados ──────────────────────────────────────
        // Para cada contacto (hasta maxStateTests) abrimos el modal y cambiamos
        // el estado rotando entre LEIDO → RESPONDIDO → ARCHIVADO → NUEVO
        // El ciclo SIEMPRE abre la fila en posición `rowIndex` para no repetir el mismo.

        int stateTestsToRun = Math.Min(maxStateTests, available);

        for (int n = 0; n < stateTestsToRun; n++)
        {
            int rowIndex   = n + 1;                                       // 1-based
            string target  = ContactosPage.Estados[n % ContactosPage.Estados.Length];
            string testId  = $"TC-CTT-EST-{n + 1:D2}";
            string desc    = $"Contacto fila {rowIndex} → estado {target}";

            results.Add(await RunCase(driver, testId, desc, "Contactos-Estado",
                () =>
                {
                    // Abrir modal del contacto en la posición indicada
                    if (!page.OpenDetailModal(rowIndex))
                        throw new InvalidOperationException(
                            $"No se pudo abrir el modal para la fila {rowIndex}.");

                    string estadoAntes = page.GetCurrentState();

                    // Verificar que el botón del estado destino existe en el modal
                    if (!page.IsStateButtonVisible(target))
                        throw new InvalidOperationException(
                            $"El botón '{target}' no está visible en el modal. " +
                            $"Estado actual: {estadoAntes}.");

                    page.ClickChangeStateTo(target);

                    // Cerrar el modal y volver a la tabla
                    page.CloseModal();

                    _logger.LogInformation("  {TestId}: {Antes} → {Target}", testId, estadoAntes, target);
                    return true; // éxito si no lanzó excepción
                }));
        }

        // ── BLOQUE 3: Eliminar hasta 5 registros ────────────────────────────
        // Verificar nuevamente cuántos registros quedan (los cambios de estado
        // no borran filas, así que deberían ser los mismos).
        // Sólo ejecutar si existen registros suficientes.

        available = page.GetContactCount();
        int actualDelete = Math.Min(deleteCount, available);

        if (actualDelete == 0)
        {
            _logger.LogWarning("No quedan contactos para borrar — se omiten tests de eliminación.");
        }
        else
        {
            if (actualDelete < deleteCount)
                _logger.LogWarning(
                    "Solo hay {N} contacto(s) — se borrarán {M} en lugar de {D}.",
                    available, actualDelete, deleteCount);

            for (int n = 0; n < actualDelete; n++)
            {
                string testId = $"TC-CTT-DEL-{n + 1:D2}";
                int countBefore = page.GetContactCount();

                results.Add(await RunCase(driver, testId,
                    $"Eliminar contacto #{n + 1} (siempre primera fila disponible)",
                    "Contactos-Eliminar",
                    () =>
                    {
                        if (countBefore == 0)
                            throw new InvalidOperationException(
                                "No quedan contactos para eliminar.");

                        if (!page.ClickEliminarFirstRow())
                            throw new InvalidOperationException(
                                "No se encontró el botón de eliminar en la primera fila.");

                        page.ConfirmEliminar();

                        // Verificar que el conteo bajó en 1
                        int countAfter = page.GetContactCount();
                        return countAfter < countBefore;
                    }));

                // Actualizar conteo para el siguiente intento
                available = page.GetContactCount();
                if (available == 0)
                {
                    _logger.LogWarning("No quedan más contactos — se detiene eliminación en TC-CTT-DEL-{N:D2}.", n + 2);
                    break;
                }
            }
        }

        await Task.CompletedTask;
        return results;
    }

    // ── RunCase genérico ─────────────────────────────────────────────────────
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
            catch (Exception ex) { _logger.LogWarning("Screenshot fallido: {M}", ex.Message); }
        }

        await Task.CompletedTask;
        return new TestResult
        {
            TestName       = testName,
            Category       = category,
            Passed         = passed,
            Message        = message,
            ScreenshotPath = screenshotPath,
            ExecutedAt     = DateTime.Now
        };
    }
}
