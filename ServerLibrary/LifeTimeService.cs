using SharedLibrary;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerLibrary.HubsProvider;
using Microsoft.AspNetCore.SignalR;

namespace ServerLibrary;

public sealed class LifeTimeService : IHostedService
{
    private readonly ILogger<LifeTimeService> _logger;
    private readonly SharedHub? _hubContext;
    public LifeTimeService(ILogger<LifeTimeService> logger, SharedHub hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{service} is starting.", nameof(LifeTimeService));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{service} is stopping.", nameof(LifeTimeService));
        if (_hubContext != null)
        {
            await _hubContext.SendTopic(DaprMessage.Fire_AllUserLogout, "");
        }        
    }

}
