using BaqueanoAutoTest.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BaqueanoAutoTest;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly TestRunner _testRunner;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, TestRunner testRunner, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _testRunner = testRunner;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BaqueanoAutoTest iniciado — {Time}", DateTimeOffset.Now);

        try
        {
            await _testRunner.RunAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error fatal en TestRunner");
        }
        finally
        {
            _logger.LogInformation("BaqueanoAutoTest finalizado — {Time}", DateTimeOffset.Now);
            _lifetime.StopApplication();
        }
    }
}
