namespace BaqueanoAutoTest.Infrastructure;

public class TestResult
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ScreenshotPath { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.Now;
}
