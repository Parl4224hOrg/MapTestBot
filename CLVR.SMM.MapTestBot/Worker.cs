using CLVR.SMM.MapTestBot.Services;

namespace CLVR.SMM.MapTestBot;

public class Worker(ILogger<Worker> logger, IMapTestService mapTestService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Map test service ready: {ServiceType}", mapTestService.GetType().Name);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
