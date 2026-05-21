using BaqueanoAutoTest.Infrastructure;
using BaqueanoAutoTest.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Tests;

public class LoginTest : ITest
{
    private readonly IConfiguration _config;
    private readonly ILogger<LoginTest> _logger;
    private readonly ScreenshotService _screenshots;

    public LoginTest(IConfiguration config, ILogger<LoginTest> logger, ScreenshotService screenshots)
    {
        _config      = config;
        _logger      = logger;
        _screenshots = screenshots;
    }

    public async Task<List<TestResult>> RunAsync(IWebDriver driver)
    {
        var results  = new List<TestResult>();
        var page     = new LoginPage(driver, _config);
        var username = _config["Credentials:Username"] ?? "admin";
        var password = _config["Credentials:Password"] ?? "admin123";

        // ── TC-LOGIN-01: credenciales válidas ────────────────────────────────
        results.Add(await RunCase(driver, "TC-LOGIN-01",
            "Login con credenciales válidas", "Login",
            () =>
            {
                page.ClearAndReload();
                page.EnterUsername(username);
                page.EnterPassword(password);
                page.ClickLogin();
                return page.IsLoginSuccessful();
            }));

        // ── TC-LOGIN-02: contraseña incorrecta ───────────────────────────────
        results.Add(await RunCase(driver, "TC-LOGIN-02",
            "Login con contraseña incorrecta", "Login",
            () =>
            {
                page.ClearAndReload();
                page.EnterUsername(username);
                page.EnterPassword("wrongpass");
                page.ClickLogin();
                return !page.IsLoginSuccessful();
            }));

        // ── TC-LOGIN-03: usuario vacío ───────────────────────────────────────
        results.Add(await RunCase(driver, "TC-LOGIN-03",
            "Login con usuario vacío", "Login",
            () =>
            {
                page.ClearAndReload();
                page.EnterUsername(string.Empty);
                page.EnterPassword(password);
                page.ClickLogin();
                return !page.IsLoginSuccessful();
            }));

        // ── Re-autenticación final ────────────────────────────────────────────
        // Deja al driver autenticado para los tests siguientes.
        // Envuelto en try/catch para que un fallo aquí no aborte toda la suite.
        try
        {
            page.ClearAndReload();
            page.EnterUsername(username);
            page.EnterPassword(password);
            page.ClickLogin();
            page.IsLoginSuccessful();
            _logger.LogInformation("Re-autenticación final OK.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Re-autenticación final fallida (no aborta la suite): {Msg}",
                ex.Message);
        }

        await Task.CompletedTask;
        return results;
    }

    // ── RunCase genérico ──────────────────────────────────────────────────────
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
            passed  = action();
            message = passed ? "OK" : $"[ASERCION] Resultado inesperado: {description}";
        }
        catch (Exception ex)
        {
            message = ErrorClassifier.Classify(ex);
            _logger.LogWarning("{Test} → {Msg}", testName, message);
        }
        finally
        {
            try   { screenshotPath = _screenshots.TakeScreenshot(driver, testName); }
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
