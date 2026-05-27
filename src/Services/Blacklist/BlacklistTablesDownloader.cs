using clrhost;
using System.Net.Http;

internal sealed class BlacklistTablesDownloader
{
    private HttpClient _httpClient;
    private CancellationTokenSource _cts;
    public bool _isDataLoaded;

    public BlacklistTablesDownloader(CancellationTokenSource cts)
    {
        _cts = cts;
        _httpClient = new HttpClient();
    }

    public void StartDownloader()
    {
        _ = Task.Run(() => RunAsync(_cts.Token));
    }

    public void StopDownloader()
    {
        _cts?.Cancel();
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var table in BlacklistPlayersService.BlackListsTables)
            {
                try
                {
                    string url =
                        $"https://docs.google.com/spreadsheets/d/{table.SpreadsheetId}/export?format=xlsx";

                    string savePath =
                        Path.Combine(PathService.BLACKLISTS_TABLES_PATH, $"{table.Name}.xlsx");

                    using var response = await _httpClient.GetAsync(url, token);
                    response.EnsureSuccessStatusCode();

                    var bytes = await response.Content.ReadAsByteArrayAsync(token);
                    await File.WriteAllBytesAsync(savePath, bytes, token);
                    BlacklistPlayersService.BlockedPlayersPerServer.Add(
                        table.Name, BlacklistPlayersService.ExtractBlockedPlayersPerServer(table.Name, savePath));

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки {table.Name}: {ex.Message}");
                }
            }

            _isDataLoaded = true;
            Console.WriteLine("Загрузка данных завершена.");

            await Task.Delay(Timers.BLACKLIST_DOWNLOADER_TIME, token);
            BlacklistPlayersService.BlockedPlayersPerServer.Clear();
            _isDataLoaded = false;  
        }
    }
}