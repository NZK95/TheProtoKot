using AmazingBot;
using Microsoft.Extensions.Options;

namespace AmazingBot
{
    internal sealed class Worker(
        IOptions<TelegramOptions> telegramOptions,
        IOptions<ImageServiceOptions> imageOptions,
        IOptions<ServersServiceOptions> serversOptions,
        IOptions<BlacklistTablesOptions> blacklistsOptions) : BackgroundService
    {
        private TelegramBot _bot;
        private SessionManager _sessionManager;
        private CancellationTokenSource _cts;
        private string? _botToken;
        private string? _imageServiceApiToken;
        private string? _imageServiceUrl;
        private string? _serversServiceUrl;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BlacklistPlayersService.BlackListsTables = blacklistsOptions.Value.Blacklists;
            _cts = new CancellationTokenSource();
            _botToken = telegramOptions.Value.BotToken;
            _imageServiceApiToken = imageOptions.Value.ApiToken;
            _imageServiceUrl = imageOptions.Value.BaseUrl;
            _serversServiceUrl = serversOptions.Value.Url;
            _sessionManager = await SessionManager.CreateAsync(new MainDatabase());
            
            _bot = new TelegramBot(
                _cts,
                _sessionManager,
                _botToken,
                _imageServiceApiToken,
                _imageServiceUrl,
                _serversServiceUrl);


            await _bot.InitAsync();
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
