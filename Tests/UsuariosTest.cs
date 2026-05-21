using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Tests;

public class UsuariosTest : ITest
{
    private readonly IConfiguration _config;
    private readonly ILogger<UsuariosTest> _logger;
    private readonly ScreenshotService _screenshots;

    // Credenciales estándar usadas al crear usuarios de prueba
    private const string TestUserPassword = "Test@1234";

    public UsuariosTest(IConfiguration config, ILogger<UsuariosTest> logger, ScreenshotService screenshots)
    {
        _config = config;
        _logger = logger;
        _screenshots = screenshots;
    }

    public async Task<List<TestResult>> RunAsync(IWebDriver driver)
    {
        var results = new List<TestResult>();
        var page = new UsuariosPage(driver, _config);

        int totalTests = int.TryParse(_config["TestSettings:TotalUsuariosTests"], out var t) ? t : 12;

        // 2 nav tests fijos; resto 40/40/20
        int remaining = totalTests - 2;
        int altaCount = (int)Math.Floor(remaining * 0.4);
        int modCount  = (int)Math.Floor(remaining * 0.4);
        int delCount  = remaining - altaCount - modCount;

        var perfiles = new[] { "ADMIN", "CONSULTA", "GESTOR" };

        // ── BLOQUE 1: Navegación ─────────────────────────────────────────────

        results.Add(await RunCase(driver, "TC-USR-NAV-01", "Navegar a Usuarios por sidebar", "Usuarios-Navegacion",
            () => { page.NavigateViaSidebar(); return page.IsUsuariosPageLoaded(); }));

        results.Add(await RunCase(driver, "TC-USR-NAV-02", "Navegar a Usuarios por URL directa", "Usuarios-Navegacion",
            () => { page.NavigateViaUrl(); return page.IsUsuariosPageLoaded(); }));

        if (!page.IsUsuariosPageLoaded())
            page.NavigateViaUrl();

        // ── BLOQUE 2: ALTA ───────────────────────────────────────────────────

        var createdUsers = new List<string>();
        for (int n = 1; n <= altaCount; n++)
        {
            var username = $"usertest{n}";
            var nombre   = $"Usuario{n}";
            var apellido = $"Apellido{n}";
            var email    = $"usertest{n}@test.com";
            var perfil   = perfiles[(n - 1) % perfiles.Length];
            bool activo  = n % 3 != 0;
            var testId   = $"TC-USR-ALTA-{n:D2}";

            results.Add(await RunCase(driver, testId, $"Crear usuario {username}", "Usuarios-Alta",
                () =>
                {
                    page.ClickNuevo();
                    page.FillForm(username, nombre, apellido, email, TestUserPassword, perfil, activo);
                    page.ClickGuardar();
                    return page.IsUserInTable(username);
                }));

            if (results.Last().Passed)
                createdUsers.Add(username);
        }

        // ── BLOQUE 3: Login con 30% de los usuarios creados (ventana nueva) ─

        int loginCount = createdUsers.Count > 0
            ? Math.Max(1, (int)Math.Ceiling(createdUsers.Count * 0.3))
            : 0;
        var loginTargets = createdUsers.Take(loginCount).ToList();

        foreach (var (loginUser, idx) in loginTargets.Select((u, i) => (u, i + 1)))
        {
            var testId = $"TC-USR-LOGIN-{idx:D2}";
            results.Add(await RunLoginInNewWindow(driver, testId, loginUser, TestUserPassword));
        }

        // Restaurar sesión admin después de los tests de login en ventanas nuevas
        if (loginTargets.Count > 0)
            RestoreAdminSession(driver, page);

        // ── BLOQUE 4: MODIFICAR ──────────────────────────────────────────────
        // (a) Nunca modificar "admin"
        var modTargets = createdUsers
            .Where(u => !u.Equals("admin", StringComparison.OrdinalIgnoreCase))
            .Take(modCount).ToList();

        for (int n = 0; n < modTargets.Count; n++)
        {
            var username = modTargets[n];
            var nombre   = $"UsuarioMod{n + 1}";
            var apellido = $"ApellidoMod{n + 1}";
            var email    = $"mod{n + 1}@test.com";
            var perfil   = perfiles[n % perfiles.Length];
            bool activo  = n % 2 == 0;
            var testId   = $"TC-USR-MOD-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Modificar usuario {username}", "Usuarios-Modificar",
                () =>
                {
                    page.ClickEditarByUsername(username);
                    page.FillForm(username, nombre, apellido, email, string.Empty, perfil, activo);
                    page.ClickGuardar();
                    return page.IsUserInTable(username);
                }));
        }

        // ── BLOQUE 5: ELIMINAR ───────────────────────────────────────────────
        // (a) Nunca eliminar "admin"
        var delTargets = createdUsers
            .Where(u => !u.Equals("admin", StringComparison.OrdinalIgnoreCase))
            .Take(delCount).ToList();

        for (int n = 0; n < delTargets.Count; n++)
        {
            var username = delTargets[n];
            var testId   = $"TC-USR-DEL-{n + 1:D2}";

            results.Add(await RunCase(driver, testId, $"Eliminar usuario {username}", "Usuarios-Eliminar",
                () =>
                {
                    page.ClickEliminarByUsername(username);
                    page.ConfirmEliminar();
                    return !page.IsUserInTable(username);
                }));
        }

        await Task.CompletedTask;
        return results;
    }

    // ── Login en ventana nueva ────────────────────────────────────────────────
    /// <summary>
    /// Abre una ventana nueva, prueba el login del usuario creado y cierra la ventana.
    /// El screenshot se captura ANTES de cerrar la ventana para reflejar el resultado real.
    /// </summary>
    private async Task<TestResult> RunLoginInNewWindow(
        IWebDriver driver, string testId, string username, string password)
    {
        bool passed = false;
        string message = string.Empty;
        string? screenshotPath = null;
        string mainWindow = driver.CurrentWindowHandle;

        try
        {
            // Abrir ventana nueva
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open('');");
            var newWindow = driver.WindowHandles.Last();
            driver.SwitchTo().Window(newWindow);

            try
            {
                var lp = new LoginPage(driver, _config);
                lp.Navigate();
                lp.EnterUsername(username);
                lp.EnterPassword(password);
                lp.ClickLogin();
                passed = lp.IsLoginSuccessful();
                message = passed ? "OK" : "[ASERCION] Login no exitoso con las credenciales del usuario creado";
            }
            catch (Exception ex)
            {
                message = ErrorClassifier.Classify(ex);
                _logger.LogWarning("{Test} → {Msg}", testId, message);
            }
            finally
            {
                // Capturar screenshot EN la ventana del login (antes de cerrarla)
                try { screenshotPath = _screenshots.TakeScreenshot(driver, testId); }
                catch (Exception ex) { _logger.LogWarning("Screenshot fallido: {M}", ex.Message); }

                // Cerrar ventana de prueba y volver a la principal
                try { driver.Close(); } catch { }
                try { driver.SwitchTo().Window(mainWindow); } catch { }
            }
        }
        catch (Exception ex)
        {
            message = ErrorClassifier.Classify(ex);
            _logger.LogWarning("{Test} (apertura ventana) → {Msg}", testId, message);
            // Asegurar regreso a ventana principal si algo falló antes del SwitchTo
            try { driver.SwitchTo().Window(mainWindow); } catch { }
        }

        await Task.CompletedTask;
        return new TestResult
        {
            TestName       = testId,
            Category       = "Usuarios-Login",
            Passed         = passed,
            Message        = message,
            ScreenshotPath = screenshotPath,
            ExecutedAt     = DateTime.Now
        };
    }

    // ── Restaurar sesión admin ────────────────────────────────────────────────
    /// <summary>
    /// Después de tests con ventanas nuevas que pueden alterar la sesión,
    /// vuelve a autenticarse como admin y regresa a la página de Usuarios.
    /// </summary>
    private void RestoreAdminSession(IWebDriver driver, UsuariosPage page)
    {
        try
        {
            var lp = new LoginPage(driver, _config);
            lp.ClearAndReload();
            lp.EnterUsername(_config["Credentials:Username"] ?? "admin");
            lp.EnterPassword(_config["Credentials:Password"] ?? "admin123");
            lp.ClickLogin();
            lp.IsLoginSuccessful();
            page.NavigateViaUrl();
            _logger.LogInformation("Sesión admin restaurada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo restaurar sesión admin: {Msg}", ex.Message);
        }
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
