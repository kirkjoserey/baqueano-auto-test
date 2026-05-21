using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BaqueanoAutoTest.Pages;

public class ContactosPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly TimeSpan _wait;
    private readonly IJavaScriptExecutor _js;

    public static readonly string[] Estados = { "NUEVO", "LEIDO", "RESPONDIDO", "ARCHIVADO" };

    // XPath que localiza cualquiera de los 4 botones de cambio de estado
    private const string StateButtonsXPath =
        "//button[normalize-space(.)='NUEVO' or normalize-space(.)='LEIDO' or " +
        "normalize-space(.)='RESPONDIDO' or normalize-space(.)='ARCHIVADO']";

    public ContactosPage(IWebDriver driver, IConfiguration config)
    {
        _driver  = driver;
        _js      = (IJavaScriptExecutor)driver;
        _baseUrl = config["TestSettings:BaseUrl"] ?? "http://localhost:8080/baqueano";
        _wait    = TimeSpan.FromSeconds(int.Parse(config["TestSettings:ImplicitWaitSeconds"] ?? "10"));
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

    // ── Modal state detection ────────────────────────────────────────────────
    // PRINCIPIO: detectar el modal por su CONTENIDO ÚNICO (botones de estado),
    // NO por el contenedor genérico role='dialog' que puede no existir en React.

    /// <summary>El modal de detalle está abierto si algún botón de estado es visible.</summary>
    private bool IsModalOpen()
    {
        try
        {
            return _driver
                .FindElements(By.XPath(StateButtonsXPath))
                .Any(b => b.Displayed && b.Enabled);
        }
        catch { return false; }
    }

    /// <summary>Espera hasta que el modal esté abierto (botones de estado visibles).</summary>
    private bool WaitModalOpen()
    {
        try
        {
            Waiter.Until(_ => IsModalOpen());
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    /// <summary>Espera hasta que el modal esté cerrado (botones de estado desaparecen).</summary>
    private bool WaitModalClosed()
    {
        try
        {
            new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
                .Until(_ => !IsModalOpen());
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    /// <summary>
    /// Si hay un modal abierto del test anterior, lo cierra antes de continuar.
    /// Así cada test empieza siempre con la tabla visible.
    /// </summary>
    private void TryCloseExistingModal()
    {
        if (!IsModalOpen()) return;
        CloseModal();
        Thread.Sleep(300);
    }

    // ── Click helpers ────────────────────────────────────────────────────────

    private IWebElement? TryFind(By by)
    {
        try { var el = _driver.FindElement(by); return el.Displayed ? el : null; }
        catch { return null; }
    }

    private void JsClick(IWebElement el)
    {
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
        _js.ExecuteScript("arguments[0].click();", el);
    }

    private void SafeClick(By by)
    {
        WaitForToastsToDisappear();
        var waiter = Waiter;
        var el = waiter.Until(d =>
        {
            try { var e = d.FindElement(by); return (e.Displayed && e.Enabled) ? e : null; }
            catch { return null; }
        })!;
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
        SafeClick(By.CssSelector("a[href='/baqueano/contactos']"));

    public void NavigateViaUrl() =>
        _driver.Navigate().GoToUrl($"{_baseUrl}/contactos");

    public bool IsContactosPageLoaded()
    {
        try
        {
            Waiter.Until(d =>
            {
                try
                {
                    var el = d.FindElement(By.XPath(
                        "//h1[contains(.,'Contacto')] | //h2[contains(.,'Contacto')]"));
                    return el.Displayed;
                }
                catch { return false; }
            });
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
    }

    // ── Table ────────────────────────────────────────────────────────────────

    /// <summary>Filas de datos reales (excluye vacías / mensajes de "sin registros").</summary>
    public int GetContactCount()
    {
        try
        {
            return _driver
                .FindElements(By.CssSelector("table tbody tr"))
                .Count(r =>
                {
                    try
                    {
                        return r.Displayed &&
                               !string.IsNullOrWhiteSpace(r.Text) &&
                               r.FindElements(By.CssSelector("td")).Count > 1;
                    }
                    catch { return false; }
                });
        }
        catch { return 0; }
    }

    // ── Abrir modal de detalle ───────────────────────────────────────────────

    /// <summary>
    /// Abre el modal "Detalle de contacto" para la fila en posición rowIndex (1-based).
    /// Detecta apertura esperando los botones de cambio de estado, NO el contenedor dialog.
    /// </summary>
    public bool OpenDetailModal(int rowIndex)
    {
        // 1. Cerrar modal previo si quedó abierto
        TryCloseExistingModal();
        WaitForToastsToDisappear();

        // 2. Obtener filas reales (re-fetch tras cierre para evitar StaleElement)
        var rows = _driver
            .FindElements(By.CssSelector("table tbody tr"))
            .Where(r =>
            {
                try { return r.Displayed && r.FindElements(By.CssSelector("td")).Count > 1; }
                catch { return false; }
            })
            .ToList();

        if (rowIndex < 1 || rowIndex > rows.Count)
            return false;

        var row = rows[rowIndex - 1];
        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", row);
        Thread.Sleep(150);

        // 3. Estrategias de clic en orden de preferencia
        bool clicked = TryClickViewButton(row)
                    || TryClickCell(row, "td:first-child")
                    || TryClickCell(row, "td:nth-child(2)")
                    || TryJsClickRow(row);

        if (!clicked) return false;

        // 4. Confirmar apertura por la PRESENCIA DE LOS BOTONES DE ESTADO
        return WaitModalOpen();
    }

    private bool TryClickViewButton(IWebElement row)
    {
        var candidates = new[]
        {
            ".//button[.//*[contains(@class,'lucide-eye')]]",
            ".//button[contains(@aria-label,'ver') or contains(@aria-label,'detalle') or contains(@aria-label,'abrir')]",
            ".//a[contains(@aria-label,'detalle')]"
        };
        foreach (var xpath in candidates)
        {
            try
            {
                var btn = row.FindElement(By.XPath(xpath));
                if (btn.Displayed) { JsClick(btn); return true; }
            }
            catch { }
        }
        return false;
    }

    private bool TryClickCell(IWebElement row, string cssSelector)
    {
        try
        {
            var cell = row.FindElement(By.CssSelector(cssSelector));
            if (cell.Displayed) { JsClick(cell); return true; }
        }
        catch { }
        return false;
    }

    private bool TryJsClickRow(IWebElement row)
    {
        try { JsClick(row); return true; }
        catch { return false; }
    }

    // ── Leer estado actual ───────────────────────────────────────────────────

    /// <summary>Lee el estado actual del modal (texto que sigue a "Estado actual:").</summary>
    public string GetCurrentState()
    {
        // Buscar el texto junto a la etiqueta "Estado actual"
        var candidateXPaths = new[]
        {
            "//*[contains(normalize-space(.),'Estado actual')]/following-sibling::*[1]",
            "//*[contains(normalize-space(.),'Estado actual:')]/following::*[self::span or self::p or self::div][1]",
            "//*[contains(normalize-space(.),'Estado actual')]/following::text()[normalize-space()!=''][1]/.."
        };

        foreach (var xpath in candidateXPaths)
        {
            try
            {
                var el = _driver.FindElement(By.XPath(xpath));
                var text = el.Text.Trim().ToUpperInvariant();
                if (Estados.Contains(text)) return text;
            }
            catch { }
        }

        // Fallback: buscar cualquier elemento que contenga exactamente uno de los estados
        // y NO sea uno de los botones de cambio
        foreach (var estado in Estados)
        {
            try
            {
                var matches = _driver.FindElements(By.XPath(
                    $"//*[normalize-space(text())='{estado}' and not(self::button)]"));
                if (matches.Any(m => m.Displayed))
                    return estado;
            }
            catch { }
        }

        return "DESCONOCIDO";
    }

    // ── Cambiar estado ───────────────────────────────────────────────────────

    /// <summary>
    /// Hace clic en el botón de estado indicado dentro del modal abierto.
    /// Espera a que los toasts desaparezcan para confirmar el cambio.
    /// </summary>
    public void ClickChangeStateTo(string estado)
    {
        var target = estado.ToUpperInvariant();

        // Buscar el botón exacto (texto exacto, no solo contains, para evitar
        // confusión entre "NUEVO" y "CREAR NUEVO" si existiera)
        var btn = Waiter.Until(d =>
        {
            try
            {
                var elements = d.FindElements(By.XPath(
                    $"//button[normalize-space(.)='{target}']"));
                return elements.FirstOrDefault(e => e.Displayed && e.Enabled);
            }
            catch { return null; }
        }) ?? throw new NoSuchElementException(
            $"Botón de estado '{target}' no encontrado o no clicable en el modal.");

        _js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", btn);
        _js.ExecuteScript("arguments[0].click();", btn);
        WaitForToastsToDisappear();
    }

    public bool IsStateButtonVisible(string estado)
    {
        try
        {
            return _driver
                .FindElements(By.XPath($"//button[normalize-space(.)='{estado.ToUpperInvariant()}']"))
                .Any(b => b.Displayed && b.Enabled);
        }
        catch { return false; }
    }

    // ── Cerrar modal ─────────────────────────────────────────────────────────

    public void CloseModal()
    {
        if (!IsModalOpen()) return;

        WaitForToastsToDisappear();

        // Intentar el botón × con varios selectores
        var closeXPaths = new[]
        {
            "//button[@aria-label='Close']",
            "//button[@aria-label='Cerrar']",
            "//button[.//*[contains(@class,'lucide-x')]]",
            "//button[normalize-space(.)='×']",
            "//button[normalize-space(.)='✕']",
            "//button[contains(@class,'close')]",
            // Botón × sin texto, dentro de un contenedor que tenga los botones de estado
            "//*[.//button[normalize-space(.)='NUEVO']]//button[not(normalize-space(.)) or normalize-space(.)='']"
        };

        bool closed = false;
        foreach (var xpath in closeXPaths)
        {
            try
            {
                var btn = _driver.FindElement(By.XPath(xpath));
                if (btn.Displayed)
                {
                    _js.ExecuteScript("arguments[0].click();", btn);
                    closed = true;
                    break;
                }
            }
            catch { }
        }

        // Fallback: tecla Escape
        if (!closed)
        {
            try { _driver.FindElement(By.TagName("body")).SendKeys(Keys.Escape); }
            catch { }
        }

        // Esperar hasta que los botones de estado desaparezcan (modal realmente cerrado)
        WaitModalClosed();
        Thread.Sleep(200);
    }

    // ── Eliminar ─────────────────────────────────────────────────────────────

    public bool ClickEliminarFirstRow()
    {
        TryCloseExistingModal();
        try
        {
            var rows = _driver.FindElements(By.CssSelector("table tbody tr"))
                .Where(r =>
                {
                    try { return r.Displayed && r.FindElements(By.CssSelector("td")).Count > 1; }
                    catch { return false; }
                })
                .ToList();

            if (rows.Count == 0) return false;

            var delBtn = rows[0].FindElement(By.XPath(
                ".//button[.//*[contains(@class,'lucide-trash')]]"));
            SafeClick(delBtn);
            return true;
        }
        catch { return false; }
    }

    public void ConfirmEliminar()
    {
        SafeClick(By.XPath(
            "//button[contains(.,'Confirmar') or contains(.,'Sí') or " +
            "(contains(.,'Eliminar') and not(contains(@class,'lucide')))]"));
        WaitForToastsToDisappear();
    }
}
