using OpenQA.Selenium;

namespace BaqueanoAutoTest.Infrastructure;

public interface ITest
{
    Task<List<TestResult>> RunAsync(IWebDriver driver);
}
