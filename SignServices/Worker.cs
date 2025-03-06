using SignServices.Services;

namespace SignServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TokenService _tokenService;
        private readonly IHostApplicationLifetime _appLifetime;

        public Worker(ILogger<Worker> logger, TokenService tokenService, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _tokenService = tokenService;
            _appLifetime = appLifetime;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SignServices at: {time}", DateTimeOffset.Now);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("SignServices running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("SignServices started successfully");

                // Keep the service running until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, no need to log as error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running SignServices");
                // Initiate application shutdown
                _appLifetime.StopApplication();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SignServices at: {time}", DateTimeOffset.Now);
            await base.StopAsync(cancellationToken);
        }
    }
}