using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BaqueanoAutoTest.Pages;

public class ParametrosPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly TimeSpan _wait;
    private readonly IJavaScriptExecutor _js;

    public ParametrosPage(IWebDriver driver, IConfiguration config)
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
        SafeClick(By.CssSelector("a[href='/baqueano/parametros']"));

    public void NavigateViaUrl() =>
        _driver.Navigate().GoToUrl($"{_baseUrl}/parametros");

    public bool IsParametrosPageLoaded()
    {
        try
        {
            WaitVisible(By.XPath(
                "//h1[contains(.,'Parámetro') or contains(.,'Parametro')] | " +
                "//h2[contains(.,'Parámetro') or contains(.,'Parametro')]"));
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    // ── ALTA ────────────────────────────────────────────────────────────────

    public void ClickNuevo() =>
        SafeClick(By.XPath("//button[contains(.,'Nuevo')]"));

    public void FillForm(string clave, string valor, string descripcion, string estado = "Activo")
    {
        FillField(By.Id("clave"),
                  By.XPath("//input[@name='clave' or @placeholder='Clave' or @placeholder='Key']"),
                  clave);

        FillField(By.Id("valor"),
                  By.XPath("//input[@name='valor' or @placeholder='Valor' or @placeholder='Value']"),
                  valor);

        FillField(By.Id("descripcion"),
                  By.XPath("//input[@name='descripcion' or @placeholder='Descripcion' or @placeholder='Descripción'" +
                            " or @name='description'] | //textarea[@name='descripcion']"),
                  descripcion);

        SetSelectField(By.Id("estado"),
                       By.XPath("//select[@name='estado']"),
                       estado);
    }

    private void FillField(By primary, By fallback, string value)
    {
        IWebElement? el = null;
        try { el = _driver.FindElement(primary); }
        catch { el = WaitVisible(fallback); }
        el.Clear();
        el.SendKeys(value);
    }

    private void SetSelectField(By primary, By fallback, string value)
    {
        IWebElement? select = null;
        try { select = _driver.FindElement(primary); }
        catch
        {
            try { select = _driver.FindElement(fallback); }
            catch { return; }
        }
        var sel = new SelectElement(select);
        try { sel.SelectByText(value); }
        catch { sel.SelectByValue(value.ToLower()); }
    }

    public void ClickGuardar()
    {
        SafeClick(By.XPath(
            "//button[@type='submit' or contains(.,'Guardar') or contains(.,'Aceptar')]"));
        WaitForToastsToDisappear();
    }

    // ── Table queries ────────────────────────────────────────────────────────

    public bool IsParametroInTable(string clave)
    {
        try
        {
            WaitVisible(By.XPath($"//table//td[normalize-space(.)='{clave}']"));
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    public void ClickEditarByClave(string clave)
    {
        var row = GetRowByText(clave);
        SafeClick(row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-pencil')]]")));
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public void ClickEliminarByClave(string clave)
    {
        var row = GetRowByText(clave);
        SafeClick(row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-trash')]]")));
    }

    public void ConfirmEliminar()
    {
        SafeClick(By.XPath(
            "//button[contains(.,'Confirmar') or contains(.,'Sí') or (contains(.,'Eliminar') and not(contains(@class,'lucide')))]"));
        WaitForToastsToDisappear();
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private IWebElement GetRowByText(string text)
    {
        return Waiter.Until(d =>
        {
            var rows = d.FindElements(By.CssSelector("table tbody tr"));
            return rows.FirstOrDefault(r =>
            {
                try { return r.FindElement(By.XPath($".//td[normalize-space(.)='{text}']")) != null; }
                catch { return false; }
            });
        }) ?? throw new NoSuchElementException($"Row for '{text}' not found in table.");
    }
}
