using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BaqueanoAutoTest.Pages;

public class UsuariosPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly TimeSpan _wait;
    private readonly IJavaScriptExecutor _js;

    public UsuariosPage(IWebDriver driver, IConfiguration config)
    {
        _driver = driver;
        _js = (IJavaScriptExecutor)driver;
        _baseUrl = config["TestSettings:BaseUrl"] ?? "http://localhost:8080/baqueano";
        _wait = TimeSpan.FromSeconds(int.Parse(config["TestSettings:ImplicitWaitSeconds"] ?? "10"));
    }

    private WebDriverWait Waiter => new(_driver, _wait);

    // ── Toast guard ──────────────────────────────────────────────────────────

    private void WaitForToastsToDisappear()
    {
        try
        {
            new WebDriverWait(_driver, TimeSpan.FromSeconds(6)).Until(d =>
                d.FindElements(By.CssSelector("[data-sonner-toast][data-visible='true']")).Count == 0);
        }
        catch (WebDriverTimeoutException)
        {
            _js.ExecuteScript(
                "document.querySelectorAll('[data-sonner-toast]').forEach(e => e.remove());");
        }
    }

    // ── Click helpers ────────────────────────────────────────────────────────

    private IWebElement WaitClickable(By by)
    {
        return Waiter.Until(d =>
        {
            try { var el = d.FindElement(by); return (el.Displayed && el.Enabled) ? el : null; }
            catch { return null; }
        })!;
    }

    private IWebElement WaitVisible(By by)
    {
        return Waiter.Until(d =>
        {
            try { var el = d.FindElement(by); return el.Displayed ? el : null; }
            catch { return null; }
        })!;
    }

    private void SafeClick(By by)
    {
        WaitForToastsToDisappear();
        var el = WaitClickable(by);
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        try { el.Click(); }
        catch (ElementClickInterceptedException) { _js.ExecuteScript("arguments[0].click();", el); }
    }

    private void SafeClick(IWebElement el)
    {
        WaitForToastsToDisappear();
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        try { el.Click(); }
        catch (ElementClickInterceptedException) { _js.ExecuteScript("arguments[0].click();", el); }
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    public void NavigateViaSidebar() =>
        SafeClick(By.CssSelector("a[href='/baqueano/usuarios']"));

    public void NavigateViaUrl() =>
        _driver.Navigate().GoToUrl($"{_baseUrl}/usuarios");

    public bool IsUsuariosPageLoaded()
    {
        try
        {
            WaitVisible(By.XPath(
                "//h1[contains(.,'Usuario')] | //h2[contains(.,'Usuario')]"));
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    // ── ALTA ────────────────────────────────────────────────────────────────

    public void ClickNuevo() =>
        SafeClick(By.XPath("//button[contains(.,'Nuevo')]"));

    /// <summary>
    /// Rellena el formulario de usuario.
    /// Campos según imagen: Username, Email, Contrasenia, Nombre, Apellido, Perfil (select), Activo (checkbox).
    /// En modo edición dejar password = string.Empty para saltear ese campo.
    /// </summary>
    public void FillForm(string username, string nombre, string apellido,
                         string email, string password,
                         string perfil = "ADMIN", bool activo = true)
    {
        // Username
        FillField(By.Id("username"),
                  By.XPath("//input[@name='username' or @placeholder='Username' or @placeholder='Usuario']"),
                  username);

        // Email
        FillField(By.Id("email"),
                  By.XPath("//input[@type='email' or @name='email' or @placeholder='Email']"),
                  email);

        // Contraseña — solo en ALTA (si está presente)
        if (!string.IsNullOrEmpty(password))
            TryFillPassword(password);

        // Nombre
        FillField(By.Id("nombre"),
                  By.XPath("//input[@name='nombre' or @placeholder='Nombre']"),
                  nombre);

        // Apellido — campo requerido visible en el formulario
        FillField(By.Id("apellido"),
                  By.XPath("//input[@name='apellido' or @placeholder='Apellido']"),
                  apellido);

        // Perfil (select dropdown: ADMIN / CONSULTA / GESTOR)
        SetPerfilSelect(perfil);

        // Activo (checkbox)
        SetActivoCheckbox(activo);
    }

    private void FillField(By primary, By fallback, string value)
    {
        IWebElement? el = null;
        try { el = _driver.FindElement(primary); }
        catch { el = WaitVisible(fallback); }
        el.Clear();
        el.SendKeys(value);
    }

    private void TryFillPassword(string password)
    {
        try
        {
            IWebElement? el = null;
            try { el = _driver.FindElement(By.Id("password")); }
            catch
            {
                try { el = _driver.FindElement(By.XPath(
                    "//input[@type='password' and not(@autocomplete='current-password')]")); }
                catch { return; }
            }
            el.Clear();
            el.SendKeys(password);
        }
        catch { /* campo ausente en edición — se ignora */ }
    }

    private void SetPerfilSelect(string perfil)
    {
        IWebElement? select = null;
        try { select = _driver.FindElement(By.Id("perfil")); }
        catch
        {
            try { select = _driver.FindElement(
                By.XPath("//select[@name='perfil' or @name='rol' or @name='role']")); }
            catch { return; }
        }
        var sel = new SelectElement(select);
        try { sel.SelectByText(perfil); }
        catch
        {
            try { sel.SelectByValue(perfil.ToLower()); }
            catch { /* perfil no encontrado — se deja el valor por defecto */ }
        }
    }

    private void SetActivoCheckbox(bool shouldBeChecked)
    {
        IWebElement? cb = null;
        try { cb = _driver.FindElement(By.Id("activo")); }
        catch
        {
            try
            {
                // Buscar checkbox próximo a la etiqueta "Activo"
                cb = _driver.FindElement(By.XPath(
                    "//input[@type='checkbox' and (@name='activo' or " +
                    "following-sibling::*[normalize-space(.)='Activo'] or " +
                    "preceding-sibling::*[normalize-space(.)='Activo'])]"));
            }
            catch { return; }
        }

        bool isChecked = cb.Selected;
        if (isChecked != shouldBeChecked)
            _js.ExecuteScript("arguments[0].click();", cb);
    }

    public void ClickGuardar()
    {
        SafeClick(By.XPath(
            "//button[contains(.,'Guardar') or (@type='submit' and not(contains(.,'Cancel')))]"));
        WaitForToastsToDisappear();
    }

    // ── Table queries ────────────────────────────────────────────────────────

    public bool IsUserInTable(string username)
    {
        try
        {
            WaitVisible(By.XPath($"//table//td[normalize-space(.)='{username}']"));
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    public void ClickEditarByUsername(string username)
    {
        GuardAdminUser(username, "editar");
        var row = GetRowByText(username);
        SafeClick(row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-pencil')]]")));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public void ClickEliminarByUsername(string username)
    {
        GuardAdminUser(username, "eliminar");
        var row = GetRowByText(username);
        SafeClick(row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-trash')]]")));
    }

    public void ConfirmEliminar()
    {
        SafeClick(By.XPath(
            "//button[contains(.,'Confirmar') or contains(.,'Sí') or " +
            "(contains(.,'Eliminar') and not(contains(@class,'lucide')))]"));
        WaitForToastsToDisappear();
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>Lanza excepción si se intenta modificar o eliminar al usuario protegido "admin".</summary>
    private static void GuardAdminUser(string username, string operation)
    {
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"[PROGRAMACION] El usuario 'admin' está protegido y no puede ser {operation}do.");
    }

    private IWebElement GetRowByText(string text)
    {
        return Waiter.Until(d =>
        {
            var rows = d.FindElements(By.CssSelector("table tbody tr"));
            return rows.FirstOrDefault(r =>
            {
                try { return r.FindElement(
                    By.XPath($".//td[normalize-space(.)='{text}']")) != null; }
                catch { return false; }
            });
        }) ?? throw new NoSuchElementException($"Row for '{text}' not found in table.");
    }
}
