using OpenQA.Selenium;

namespace BaqueanoAutoTest.Infrastructure;

/// <summary>
/// Clasifica excepciones distinguiendo errores de la herramienta Selenium
/// de errores propios del código de pruebas.
/// </summary>
public static class ErrorClassifier
{
    public static string Classify(Exception ex)
    {
        var origin = ex switch
        {
            NoSuchElementException              => "[HERRAMIENTA] Elemento no encontrado en el DOM",
            ElementClickInterceptedException    => "[HERRAMIENTA] Clic bloqueado por overlay (toast/modal)",
            ElementNotInteractableException     => "[HERRAMIENTA] Elemento no interactuable (oculto o deshabilitado)",
            StaleElementReferenceException      => "[HERRAMIENTA] Referencia obsoleta al elemento (DOM recargado)",
            WebDriverTimeoutException           => "[HERRAMIENTA] Timeout: condición no cumplida en el tiempo configurado",
            NoSuchWindowException               => "[HERRAMIENTA] Ventana del navegador cerrada inesperadamente",
            UnhandledAlertException             => "[HERRAMIENTA] Alerta del navegador sin manejar",
            WebDriverException                  => "[HERRAMIENTA] Error de WebDriver/ChromeDriver",
            InvalidOperationException           => "[PROGRAMACION] Operación inválida en el flujo de prueba",
            NullReferenceException              => "[PROGRAMACION] Referencia nula — revisar selectores o datos de prueba",
            ArgumentException                   => "[PROGRAMACION] Argumento inválido — revisar parámetros del test",
            NotSupportedException               => "[PROGRAMACION] Operación no soportada",
            _                                   => $"[PROGRAMACION] {ex.GetType().Name}"
        };

        return $"{origin} — {ShortMessage(ex.Message)}";
    }

    /// <summary>Extrae la primera línea útil del mensaje, descartando el ruido HTML de Selenium.</summary>
    private static string ShortMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "(sin mensaje)";

        // Selenium wraps the real cause after the first newline; take that line if shorter
        var lines = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var first = lines[0].Trim();

        // Prefer lines that start with common cause patterns
        var cause = lines.FirstOrDefault(l =>
            l.TrimStart().StartsWith("Caused by:", StringComparison.OrdinalIgnoreCase) ||
            l.TrimStart().StartsWith("Message:", StringComparison.OrdinalIgnoreCase));

        var best = cause is not null
            ? cause.Trim().Replace("Message:", "").Replace("Caused by:", "").Trim()
            : first;

        return best.Length > 220 ? best[..220] + "…" : best;
    }
}
