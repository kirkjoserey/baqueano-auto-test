using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BaqueanoAutoTest.Pages;

public class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly TimeSpan _wait;

    public LoginPage(IWebDriver driver, IConfiguration config)
    {
        _driver  = driver;
        _baseUrl = config["TestSettings:BaseUrl"] ?? "http://localhost:8080/baqueano";
        _wait    = TimeSpan.FromSeconds(int.Parse(
            config["TestSettings:ImplicitWaitSeconds"] ?? "10"));
    }

    public void Navigate() => _driver.Navigate().GoToUrl(_baseUrl);

    public void EnterUsername(string username)
    {
        var field = _driver.FindElement(By.Id("username"));
        field.Clear();
        field.SendKeys(username);
    }

    public void EnterPassword(string password)
    {
        var field = _driver.FindElement(By.Id("password"));
        field.Clear();
        field.SendKeys(password);
    }

    public void ClickLogin() =>
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

    public bool IsLoginSuccessful()
    {
        try
        {
            new WebDriverWait(_driver, _wait).Until(d =>
            {
                try   { d.FindElement(By.Id("username")); return false; }
                catch (NoSuchElementException) { return true; }
            });
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    /// <summary>
    /// Borra cookies, navega al login y espera que el formulario esté listo.
    /// Usa un timeout extendido (2× el configurado, mínimo 20s) para tolerar
    /// la emulación móvil o un SPA que tarde en redirigir al login.
    /// Si el primer intento falla, hace un Refresh() y reintenta con el timeout normal.
    /// </summary>
    public void ClearAndReload()
    {
        _driver.Manage().Cookies.DeleteAllCookies();
        _driver.Navigate().GoToUrl(_baseUrl);

        // Timeout extendido: cubre emulación móvil + redirección del SPA React
        var extWait = TimeSpan.FromSeconds(
            Math.Max(_wait.TotalSeconds * 2, 20));

        bool found = WaitForLoginForm(extWait);

        if (!found)
        {
            // Fallback: hard refresh y un segundo intento
            _driver.Navigate().Refresh();
            WaitForLoginForm(_wait);   // si falla aquí lanza excepción normal
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private bool WaitForLoginForm(TimeSpan timeout)
    {
        try
        {
            new WebDriverWait(_driver, timeout).Until(d =>
            {
                try   { return d.FindElement(By.Id("username")).Displayed; }
                catch { return false; }
            });
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }
}
