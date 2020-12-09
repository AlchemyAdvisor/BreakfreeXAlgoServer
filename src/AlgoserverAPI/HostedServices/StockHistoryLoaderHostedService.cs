using System;
using System.Threading;
using System.Threading.Tasks;
using Algoserver.API.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Algoserver.API.HostedServices
{
    public class StockHistoryLoaderHostedService : BackgroundService
    {
        private int _preHour = -1;
        private readonly ILogger<StockHistoryLoaderHostedService> _logger;
        private readonly ScannerHistoryService _scannerHistory;
        private readonly ScannerCacheService _scannerCache;
        private Timer _timer;

        public StockHistoryLoaderHostedService(ILogger<StockHistoryLoaderHostedService> logger, ScannerStockHistoryService scannerHistory, ScannerStockCacheService scannerCache)
        {
            _logger = logger;
            _scannerHistory = scannerHistory;
            _scannerCache = scannerCache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try {
                    var currentHour = DateTime.UtcNow.Hour;
                    if (currentHour != _preHour) {
                        var result = await _scannerHistory.RefreshAll();
                        _scannerCache.RefreshAllMarketsTime = result;
                    } else {
                        var result = await _scannerHistory.Refresh();
                        _scannerCache.RefreshMarketsTime = result;
                    }
                    _preHour = currentHour;
                    _scannerCache.ScanMarkets();
                } catch(Exception ex) {
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}