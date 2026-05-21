using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BaqueanoAutoTest.Pages;

public class PerfilesPage
{
    private readonly IWebDriver _driver;
    private readonly TimeSpan _wait;
    private readonly IJavaScriptExecutor _js;

    public PerfilesPage(IWebDriver driver, IConfiguration config)
    {
        _driver = driver;
        _js = (IJavaScriptExecutor)driver;
        _wait = TimeSpan.FromSeconds(int.Parse(config["TestSettings:ImplicitWaitSeconds"] ?? "10"));
    }

    private WebDriverWait Waiter => new(_driver, _wait);

    // ── Toast guard ─────────────────────────────────────────────────────────
    // Sonner toasts float over the UI and intercept clicks until they dismiss.
    // We wait until no [data-sonner-toast][data-visible="true"] remains.
    private void WaitForToastsToDisappear()
    {
        try
        {
            new WebDriverWait(_driver, TimeSpan.FromSeconds(6)).Until(d =>
            {
                var visible = d.FindElements(
                    By.CssSelector("[data-sonner-toast][data-visible='true']"));
                return visible.Count == 0;
            });
        }
        catch (WebDriverTimeoutException)
        {
            // If toasts never leave, force-remove them via JS and continue
            _js.ExecuteScript(
                "document.querySelectorAll('[data-sonner-toast]').forEach(e => e.remove());");
        }
    }

    // ── Click helpers ────────────────────────────────────────────────────────

    private IWebElement WaitClickable(By by)
    {
        return Waiter.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return (el.Displayed && el.Enabled) ? el : null;
            }
            catch { return null; }
        })!;
    }

    // Scrolls the element into view then clicks; falls back to JS click on interception.
    private void SafeClick(By by)
    {
        WaitForToastsToDisappear();
        var el = WaitClickable(by);
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        try
        {
            el.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // A remaining overlay (toast, backdrop) still blocks — use JS click
            _js.ExecuteScript("arguments[0].click();", el);
        }
    }

    private void SafeClick(IWebElement el)
    {
        WaitForToastsToDisappear();
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        try
        {
            el.Click();
        }
        catch (ElementClickInterceptedException)
        {
            _js.ExecuteScript("arguments[0].click();", el);
        }
    }

    private IWebElement WaitVisible(By by)
    {
        return Waiter.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed ? el : null;
            }
            catch { return null; }
        })!;
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    public void NavigateViaSidebar() =>
        SafeClick(By.CssSelector("a[href='/baqueano/perfiles'].flex"));

    public void NavigateViaDashboardLink() =>
        SafeClick(By.CssSelector("a.bg-info[href='/baqueano/perfiles']"));

    public bool IsPerfilesPageLoaded()
    {
        try
        {
            WaitVisible(By.XPath("//h1[contains(.,'Perfiles')] | //h2[contains(.,'Perfiles')]"));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // ── ALTA ────────────────────────────────────────────────────────────────

    public void ClickNuevo() =>
        SafeClick(By.XPath("//button[contains(.,'Nuevo')]"));

    public void FillForm(string nombre, string descripcion, string estado = "Activo")
    {
        FillField(By.Id("nombre"),
                  By.XPath("//input[@name='nombre' or @placeholder='Nombre']"),
                  nombre);

        FillField(By.Id("descripcion"),
                  By.XPath("//input[@name='descripcion' or @placeholder='Descripcion' or @placeholder='Descripción']"),
                  descripcion);

        SetEstado(estado);
    }

    private void FillField(By primary, By fallback, string value)
    {
        IWebElement? el = null;
        try { el = _driver.FindElement(primary); }
        catch { el = WaitVisible(fallback); }
        el.Clear();
        el.SendKeys(value);
    }

    private void SetEstado(string estado)
    {
        IWebElement? select = null;
        try { select = _driver.FindElement(By.Id("estado")); }
        catch
        {
            try { select = _driver.FindElement(By.XPath("//select[@name='estado']")); }
            catch { return; }
        }

        var sel = new SelectElement(select);
        try { sel.SelectByText(estado); }
        catch { sel.SelectByValue(estado.ToLower()); }
    }

    public void ClickGuardar()
    {
        SafeClick(By.XPath(
            "//button[@type='submit' or contains(.,'Guardar') or contains(.,'Aceptar')]"));
        // Wait for the success toast to appear and then fully disappear
        // so the next action is not blocked
        WaitForToastsToDisappear();
    }

    // ── Table queries ────────────────────────────────────────────────────────

    public bool IsProfileInTable(string nombre)
    {
        try
        {
            WaitVisible(By.XPath($"//table//td[normalize-space(.)='{nombre}']"));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public int GetTotalPerfiles()
    {
        try
        {
            var el = _driver.FindElement(By.XPath("//*[contains(text(),' en total')]"));
            var parts = el.Text.Trim().Split(' ');
            return int.TryParse(parts[0], out var n) ? n : 0;
        }
        catch { return 0; }
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    public void ClickEditarByNombre(string nombre)
    {
        var row = GetRowByNombre(nombre);
        var btn = row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-pencil')]]"));
        SafeClick(btn);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public void ClickEliminarByNombre(string nombre)
    {
        var row = GetRowByNombre(nombre);
        var btn = row.FindElement(By.XPath(
            ".//button[.//*[contains(@class,'lucide-trash')]]"));
        SafeClick(btn);
    }

    public void ConfirmEliminar()
    {
        SafeClick(By.XPath(
            "//button[contains(.,'Confirmar') or contains(.,'Sí') or (contains(.,'Eliminar') and not(contains(@class,'lucide')))]"));
        WaitForToastsToDisappear();
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private IWebElement GetRowByNombre(string nombre)
    {
        return Waiter.Until(d =>
        {
            var rows = d.FindElements(By.CssSelector("table tbody tr"));
            return rows.FirstOrDefault(r =>
            {
                try { return r.FindElement(By.XPath($".//td[normalize-space(.)='{nombre}']")) != null; }
                catch { return false; }
            });
        }) ?? throw new NoSuchElementException($"Row for '{nombre}' not found in table.");
    }
}
