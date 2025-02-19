using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algoserver.API.Helpers;
using Algoserver.API.Models.Algo;
using Algoserver.API.Models.REST;
using Microsoft.Extensions.Logging;

namespace Algoserver.API.Services
{
    public class ScannerBacktestService
    {
        private readonly ILogger<ScannerBacktestService> _logger;
        private readonly HistoryService _historyService;
        private readonly ScannerService _scanner;

        public ScannerBacktestService(ILogger<ScannerBacktestService> logger, HistoryService historyService, ScannerService scanner)
        {
            _logger = logger;
            _historyService = historyService;
            _scanner = scanner;
        }

        public Task<ScannerBacktestResponse> BacktestAsync(ScannerBacktestRequest req)
        {
            return Task.Run(async () =>
            {
                return await backtestAsync(req);
            });
        }

        private async Task<ScannerBacktestResponse> backtestAsync(ScannerBacktestRequest req)
        {
            var container = InputDataContainer.MapCalculationRequestToInputDataContainer(req);
            container.setUsdRatio(1);

            var granularity = AlgoHelper.ConvertTimeframeToGranularity(container.TimeframeInterval, container.TimeframePeriod);
            var currentPriceData = await _historyService.GetHistory(container.Symbol, granularity, container.Datafeed, container.Exchange, container.Type, container.ReplayBack);
            HistoryData dailyPriceData = null;

            if (granularity == req.rtd_tf)
            {
                dailyPriceData = new HistoryData
                {
                    Datafeed = currentPriceData.Datafeed,
                    Exchange = currentPriceData.Exchange,
                    Granularity = currentPriceData.Granularity,
                    Symbol = currentPriceData.Symbol,
                    Bars = currentPriceData.Bars.ToList(),
                };
            }
            else
            {
                dailyPriceData = await _historyService.GetHistory(container.Symbol, req.rtd_tf, container.Datafeed, container.Exchange, container.Type, container.ReplayBack);
            }

            var replayBack = container.ReplayBack;
            var response = new ScannerBacktestResponse();
            response.signals = new List<ScannerBacktestSignal>();
            ScanResponse lastSignal = null;
            var sl_ratio = container.InputStoplossRatio;

            var availableBarsCount = currentPriceData.Bars.Count();
            if (replayBack > availableBarsCount - InputDataContainer.MIN_BARS_COUNT)
            {
                replayBack = availableBarsCount - InputDataContainer.MIN_BARS_COUNT;
            }

            while (replayBack >= 0)
            {
                container.AddNext(currentPriceData.Bars, null, dailyPriceData.Bars, replayBack);
                replayBack--;

                var extendedTrendData = TrendDetector.CalculateByMesaBy2TrendAdjusted(container.CloseD, req.global_fast, req.global_slow, req.local_fast, req.local_slow);
                var trend = TrendDetector.MergeTrends(extendedTrendData);

                var scanningHistory = new ScanningHistory
                {
                    Open = container.Open,
                    High = container.High,
                    Low = container.Low,
                    Close = container.Close,
                    Time = container.Time
                };

                var dailyScanningHistory = new ScanningHistory
                {
                    Open = container.OpenD,
                    High = container.HighD,
                    Low = container.LowD,
                    Close = container.CloseD,
                    Time = container.TimeD
                };

                ScanResponse signal = null;

                var levels = TechCalculations.CalculateLevels(container.High, container.Low);

                if (req.type == TradeType.SwingN)
                {
                    signal = _scanner.ScanSwingOldStrategy(scanningHistory, sl_ratio);
                }
                else if (req.type == TradeType.SwingExt)
                {
                    signal = _scanner.ScanSwing(scanningHistory, dailyScanningHistory, extendedTrendData.GlobalTrend, extendedTrendData.LocalTrend, levels.Level128, sl_ratio);
                }
                else if (req.type == TradeType.EXT)
                {
                    signal = _scanner.ScanExt(scanningHistory, dailyScanningHistory, trend, levels.Level128, sl_ratio);
                }
                else if (req.type == TradeType.BRC)
                {
                    signal = _scanner.ScanBRC(scanningHistory, trend, levels.Level128, sl_ratio);
                }

                if (ScanResponse.IsEquals(lastSignal, signal))
                {
                    continue;
                }

                lastSignal = signal;

                var lastBacktestSignal = response.signals.LastOrDefault();
                if (lastBacktestSignal != null && lastBacktestSignal.end_timestamp == 0)
                {
                    lastBacktestSignal.end_timestamp = container.Time.LastOrDefault();
                }

                if (signal == null)
                {
                    continue;
                }

                var size = 1m;
                var sar = SupportAndResistance.Calculate(levels, container.Mintick);
                var result = DataMappingHelper.ToResponseV2(levels, sar, signal, size);
                response.signals.Add(new ScannerBacktestSignal
                {
                    data = result,
                    timestamp = container.Time.LastOrDefault(),
                    local_trend = extendedTrendData.LocalTrend,
                    global_trend = extendedTrendData.GlobalTrend,
                    local_trend_spread = extendedTrendData.LocalTrendSpread,
                    global_trend_spread = extendedTrendData.GlobalTrendSpread,
                    local_trend_spread_value = extendedTrendData.LocalTrendSpreadValue,
                    global_trend_spread_value = extendedTrendData.GlobalTrendSpreadValue,
                    global_fast_value = extendedTrendData.GlobalFastValue,
                    global_slow_value = extendedTrendData.GlobalSlowValue,
                    local_fast_value = extendedTrendData.LocalFastValue,
                    local_slow_value = extendedTrendData.LocalSlowValue,
                });
            }

            var signalProcessor = new ScannerSignalsProcessor(currentPriceData.Bars, response.signals);
            var orders = signalProcessor.Backtest(req.breakeven_candles, req.cancellation_candles, req.single_position);
            response.orders = orders;

            return response;
        }
    }
}
