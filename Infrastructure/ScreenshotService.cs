using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;

namespace BaqueanoAutoTest.Infrastructure;

public class ScreenshotService
{
    private readonly string _screenshotFolder;

    public ScreenshotService(IConfiguration configuration)
    {
        var configured = configuration["TestSettings:ScreenshotFolder"] ?? "Screenshots";

        // If the value is a relative path, anchor it to the executable's directory
        _screenshotFolder = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
    }

    public string ScreenshotFolder => _screenshotFolder;

    /// <summary>Borra todos los .png de la carpeta y la recrea limpia.</summary>
    public void ClearScreenshots()
    {
        if (Directory.Exists(_screenshotFolder))
        {
            foreach (var file in Directory.GetFiles(_screenshotFolder, "*.png"))
                File.Delete(file);
        }
        Directory.CreateDirectory(_screenshotFolder);
    }

    public string TakeScreenshot(IWebDriver driver, string testName)
    {
        Directory.CreateDirectory(_screenshotFolder);

        var safeName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var fullPath = Path.Combine(_screenshotFolder, fileName);

        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
        screenshot.SaveAsFile(fullPath);

        return fullPath;
    }
}
