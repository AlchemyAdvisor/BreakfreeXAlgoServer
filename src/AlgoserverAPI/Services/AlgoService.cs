﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algoserver.API.Helpers;
using Algoserver.API.Models.Algo;
using Algoserver.API.Models.REST;
using Algoserver.API.Services.CacheServices;
using Microsoft.Extensions.Logging;

namespace Algoserver.API.Services
{
    public class AlgoService
    {

        private readonly ILogger<AlgoService> _logger;
        private readonly HistoryService _historyService;
        private readonly ScannerService _scanner;
        private readonly LevelsPredictionService _levelsPrediction;
        private readonly ICacheService _cache;
        private string _cachePrefix = "MarketInfo_";
        private string _cachePrefixV2 = "MarketInfoV2_";
        private readonly PriceRatioCalculationService _priceRatioCalculationService;

        public AlgoService(ILogger<AlgoService> logger, HistoryService historyService, PriceRatioCalculationService priceRatioCalculationService, ScannerService scanner, LevelsPredictionService levelsPrediction, ICacheService cache)
        {
            _logger = logger;
            _historyService = historyService;
            _scanner = scanner;
            _cache = cache;
            _priceRatioCalculationService = priceRatioCalculationService;
            _levelsPrediction = levelsPrediction;
        }

        public async Task<InputDataContainer> InitAsync(CalculationRequest req, int minBarsCount = 0)
        {
            var container = InputDataContainer.MapCalculationRequestToInputDataContainer(req);
            // if (container.Datafeed != "twelvedata" && container.Datafeed != "oanda")
            // {
            //     throw new ApiException(HttpStatusCode.BadRequest,
            //         $"Unsupported '{container.Datafeed}' datafeed. Available 'twelvedata' or 'oanda' only.");
            // }

            if (container.Type == "forex")
            {
                var usdRatio = await _priceRatioCalculationService.GetSymbolRatio(container.Symbol, req.AccountCurrency, container.Datafeed, container.Type, container.Exchange);
                container.setUsdRatio(usdRatio);
            }
            else
            {
                container.setUsdRatio(1);
            }

            var granularity = AlgoHelper.ConvertTimeframeToGranularity(container.TimeframeInterval, container.TimeframePeriod);
            var currentPriceData = await _historyService.GetHistory(container.Symbol, granularity, container.Datafeed, container.Exchange, container.Type, container.ReplayBack, minBarsCount);
            HistoryData dailyPriceData = null;

            var highTFGranularity = TimeframeHelper.DAILY_GRANULARITY;

            if (granularity == TimeframeHelper.MIN1_GRANULARITY)
            {
                highTFGranularity = TimeframeHelper.HOURLY_GRANULARITY;
            }
            else if (granularity == TimeframeHelper.MIN5_GRANULARITY)
            {
                highTFGranularity = TimeframeHelper.HOUR4_GRANULARITY;
            }

            if (granularity == highTFGranularity)
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
                dailyPriceData = await _historyService.GetHistory(container.Symbol, highTFGranularity, container.Datafeed, container.Exchange, container.Type, container.ReplayBack, minBarsCount);
            }

            container.InsertHistory(currentPriceData.Bars, null, dailyPriceData.Bars, container.ReplayBack, minBarsCount);

            return container;
        }

        public Task<CalculatePositionSizeResponse> CalculatePositionSize(CalculatePositionSizeRequest req)
        {
            return Task.Run(async () =>
            {
                return await calculatePositionSize(req);
            });
        }

        public Task<CalculatePriceRatioResponse> CalculatePriceRatio(CalculatePriceRatioRequest req)
        {
            return Task.Run(async () =>
            {
                return await calculatePriceRatio(req);
            });
        }

        // Legacy
        public async Task<CalculationResponse> CalculateAsync(CalculationRequest req)
        {
            var container = await InitAsync(req);
            var levels = TechCalculations.CalculateLevels(container.High, container.Low);
            var sar = SupportAndResistance.Calculate(levels, container.Mintick);
            var trend = TrendDetector.CalculateByMesa(container.CloseD);
            var calculationData = new TradeEntryCalculationData
            {
                container = container,
                hma_period = 200,
                levels = levels,
                randomize = true,
                sar = sar,
                trend = trend
            };

            var trade = TradeEntry.Calculate(calculationData);
            var result = DataMappingHelper.ToResponse(levels, sar, trade);
            return result;
        }

        // Legacy
        public Task<CalculationMarketInfoResponse> CalculateMarketInfoAsync(Instrument instrument)
        {
            return Task.Run(() =>
            {
                return calculateMarketInfoAsync(instrument);
            });
        }

        public Task<decimal?> CalculateCVarAsync(Instrument instrument)
        {
            return Task.Run(() =>
            {
                return calculateCVarAsync(instrument);
            });
        }

        public Task<CalculationMarketInfoResponse> CalculateMarketInfoV2Async(MarketInfoCalculationRequest request)
        {
            return Task.Run(() =>
            {
                return calculateMarketInfoV2Async(request);
            });
        }

        public Task<CalculationResponseV2> CalculateV2Async(CalculationRequest req)
        {
            return Task.Run(() =>
            {
                return calculateV2Async(req);
            });
        }

        public Task<CalculationResponseV3> CalculateV3Async(CalculationRequestV3 req)
        {
            return Task.Run(() =>
            {
                return calculateV3Async(req);
            });
        }

        private async Task<decimal?> calculateCVarAsync(Instrument instrument)
        {
            var cvar = tryGetCVarFromCache(instrument);

            if (!cvar.HasValue)
            {
                var dataDaily = await _historyService.GetHistory(instrument.Id, TimeframeHelper.DAILY_GRANULARITY, instrument.Datafeed, instrument.Exchange, instrument.Type);
                var closeDaily = dataDaily.Bars.Select(_ => _.Close).ToList();
                cvar = TechCalculations.CalculateCVAR(closeDaily);
                tryAddCVarInCache(instrument, cvar.Value);
            }

            return cvar;
        }

        private async Task<CalculationMarketInfoResponse> calculateMarketInfoAsync(Instrument instrument)
        {
            var cachedResponse = tryGetCalculateMarketInfoFromCache(instrument);

            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            var data = await _historyService.GetHistory(instrument.Id, TimeframeHelper.DAILY_GRANULARITY, instrument.Datafeed, instrument.Exchange, instrument.Type);
            var high = data.Bars.Select(_ => _.High);
            var low = data.Bars.Select(_ => _.Low);
            var close = data.Bars.Select(_ => _.Close).ToList();

            var levels = TechCalculations.CalculateLevel128(high, low);
            var trend = TrendDetector.CalculateByMesaBy2TrendAdjusted(close);
            var cvar = tryGetCVarFromCache(instrument);

            if (!cvar.HasValue)
            {
                cvar = TechCalculations.CalculateCVAR(close);
                tryAddCVarInCache(instrument, cvar.Value);
            }

            var topExt = levels.Plus18;
            var natural = levels.FourEight;
            var bottomExt = levels.Minus18;
            var support = levels.ZeroEight;
            var resistance = levels.EightEight;
            var result = new CalculationMarketInfoResponse
            {
                global_trend = trend.GlobalTrend,
                local_trend = trend.LocalTrend,
                is_overhit = trend.IsOverhit,
                natural = natural,
                resistance = resistance,
                support = support,
                daily_natural = natural,
                daily_resistance = resistance,
                daily_support = support,
                last_price = close.LastOrDefault(),
                global_trend_spread = trend.GlobalTrendSpread,
                local_trend_spread = trend.LocalTrendSpread,
                cvar = cvar.GetValueOrDefault(0)
            };

            tryAddCalculateMarketInfoInCache(instrument, result);

            return result;
        }

        private async Task<CalculationMarketInfoResponse> calculateMarketInfoV2Async(MarketInfoCalculationRequest request)
        {
            var instrument = request.Instrument;
            var timeframe = request.Granularity.GetValueOrDefault(TimeframeHelper.DAILY_GRANULARITY);
            var cachedResponse = tryGetCalculateMarketInfoV2FromCache(instrument, timeframe);

            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            var dataDaily = await _historyService.GetHistory(instrument.Id, TimeframeHelper.DAILY_GRANULARITY, instrument.Datafeed, instrument.Exchange, instrument.Type);
            var highDaily = dataDaily.Bars.Select(_ => _.High);
            var lowDaily = dataDaily.Bars.Select(_ => _.Low);
            var closeDaily = dataDaily.Bars.Select(_ => _.Close).ToList();

            var levelsDaily = TechCalculations.CalculateLevel128(highDaily, lowDaily);
            var exactTFLevels = levelsDaily;

            if (timeframe != TimeframeHelper.DAILY_GRANULARITY)
            {
                var dataTF = await _historyService.GetHistory(instrument.Id, timeframe, instrument.Datafeed, instrument.Exchange, instrument.Type);
                var highFT = dataTF.Bars.Select(_ => _.High);
                var lowFT = dataTF.Bars.Select(_ => _.Low);
                exactTFLevels = TechCalculations.CalculateLevel128(highFT, lowFT);
            }


            var topDailyExt = levelsDaily.Plus18;
            var naturalDaily = levelsDaily.FourEight;
            var bottomDailyExt = levelsDaily.Minus18;
            var supportDaily = levelsDaily.ZeroEight;
            var resistanceDaily = levelsDaily.EightEight;

            var topTFExt = exactTFLevels.Plus18;
            var naturalTF = exactTFLevels.FourEight;
            var bottomTFExt = exactTFLevels.Minus18;
            var supportTF = exactTFLevels.ZeroEight;
            var resistanceTF = exactTFLevels.EightEight;

            var trend = TrendDetector.CalculateByMesaBy2TrendAdjusted(closeDaily);
            var cvar = tryGetCVarFromCache(instrument);

            if (!cvar.HasValue)
            {
                cvar = TechCalculations.CalculateCVAR(closeDaily);
                tryAddCVarInCache(instrument, cvar.Value);
            }

            var result = new CalculationMarketInfoResponse
            {
                global_trend = trend.GlobalTrend,
                local_trend = trend.LocalTrend,
                is_overhit = trend.IsOverhit,
                daily_natural = naturalDaily,
                daily_resistance = resistanceDaily,
                daily_support = supportDaily,
                natural = naturalTF,
                resistance = resistanceTF,
                support = supportTF,
                last_price = closeDaily.LastOrDefault(),
                global_trend_spread = trend.GlobalTrendSpread,
                local_trend_spread = trend.LocalTrendSpread,
                cvar = cvar.GetValueOrDefault(0)
            };

            tryAddCalculateMarketInfoV2InCache(instrument, timeframe, result);

            return result;
        }

        private async Task<CalculationResponseV2> calculateV2Async(CalculationRequest req)
        {
            var container = await InitAsync(req);
            var levels = TechCalculations.CalculateLevels(container.High, container.Low);
            var levels128 = levels.Level128;
            var sar = SupportAndResistance.Calculate(levels, container.Mintick);
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

            var isForex = container.Type == "forex";
            var symbol = container.Symbol;
            var accountSize = container.InputAccountSize * container.UsdRatio;
            var suggestedRisk = container.InputRisk;
            var sl_ratio = container.InputStoplossRatio;
            var granularity = AlgoHelper.ConvertTimeframeToGranularity(container.TimeframeInterval, container.TimeframePeriod);

            ScanResponse scanRes = null;

            var extendedTrendData = TrendDetector.CalculateByMesaBy2TrendAdjusted(container.CloseD);
            var trend = TrendDetector.MergeTrends(extendedTrendData);

            if (container.TimeframePeriod != "d" && container.TimeframePeriod != "w")
            {
                scanRes = _scanner.ScanExt(scanningHistory, dailyScanningHistory, trend, levels128, sl_ratio);
                // if (scanRes == null && granularity >= TimeframeHelper.MIN15_GRANULARITY)
                // {
                //     scanRes = _scanner.ScanBRC(scanningHistory, trend, levels128, sl_ratio);
                // }
            }

            if (scanRes == null)
            {
                if (container.TimeframePeriod == "d")
                {
                    scanRes = _scanner.ScanSwingOldStrategy(scanningHistory, sl_ratio);
                }
                if (container.TimeframePeriod == "h" && container.TimeframeInterval == 4)
                {
                    if (!extendedTrendData.IsOverhit)
                    {
                        scanRes = _scanner.ScanSwing(scanningHistory, dailyScanningHistory, extendedTrendData.GlobalTrend, extendedTrendData.LocalTrend, levels128, sl_ratio);
                    }
                }
            }

            var size = 0m;

            if (scanRes != null)
            {
                scanRes.risk = suggestedRisk;
                size = AlgoHelper.CalculatePositionValue(container.Type, symbol, accountSize, suggestedRisk, scanRes.entry, scanRes.stop, container.ContractSize);
            }

            var result = DataMappingHelper.ToResponseV2(levels, sar, scanRes, size);
            result.id = container.Id;
            return result;
        }

        private async Task<CalculationResponseV3> calculateV3Async(CalculationRequestV3 req)
        {
            var container = await InitAsync(req, req.BarsCount.GetValueOrDefault(0));
            var dates = container.Time;
            var levelsList = TechCalculations.CalculateLevelsBasedOnTradeZone(container.High, container.Low);
            var levels = levelsList.LastOrDefault();
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

            var isForex = container.Type == "forex";
            var symbol = container.Symbol;
            var accountSize = container.InputAccountSize * container.UsdRatio;
            var suggestedRisk = container.InputRisk;
            var sl_ratio = container.InputStoplossRatio;
            var granularity = AlgoHelper.ConvertTimeframeToGranularity(container.TimeframeInterval, container.TimeframePeriod);

            ScanResponse scanRes = null;

            var extendedTrendData = TrendDetector.CalculateByMesaBy2TrendAdjusted(container.CloseD);
            var trend = TrendDetector.MergeTrends(extendedTrendData);

            if (container.TimeframePeriod != "d" && container.TimeframePeriod != "w")
            {
                scanRes = _scanner.ScanExt(scanningHistory, dailyScanningHistory, trend, levels, sl_ratio);
                // if (scanRes == null && granularity >= TimeframeHelper.MIN15_GRANULARITY)
                // {
                //     scanRes = _scanner.ScanBRC(scanningHistory, trend, levels, sl_ratio);
                // }
            }

            if (scanRes == null)
            {
                if (container.TimeframePeriod == "d")
                {
                    scanRes = _scanner.ScanSwingOldStrategy(scanningHistory, sl_ratio);
                }
                if (container.TimeframePeriod == "h" && container.TimeframeInterval == 4)
                {
                    if (!extendedTrendData.IsOverhit)
                    {
                        scanRes = _scanner.ScanSwing(scanningHistory, dailyScanningHistory, extendedTrendData.GlobalTrend, extendedTrendData.LocalTrend, levels, sl_ratio);
                    }
                }
            }

            var size = 0m;

            if (scanRes != null)
            {
                scanRes.risk = suggestedRisk;
                size = AlgoHelper.CalculatePositionValue(container.Type, symbol, accountSize, suggestedRisk, scanRes.entry, scanRes.stop, container.ContractSize);
            }

            var result = DataMappingHelper.ToResponseV3(levels, scanRes, size);
            result.id = container.Id;

            var sar = new List<SaRResponse>();
            for (var i = 0; i < dates.Count; i++)
            {
                var l = levelsList[i];
                sar.Add(new SaRResponse
                {
                    date = dates[i],
                    r_p28 = l.Plus28,
                    r_p18 = l.Plus18,
                    r = l.EightEight,
                    n = l.FourEight,
                    s = l.ZeroEight,
                    s_m18 = l.Minus18,
                    s_m28 = l.Minus28
                });
            }

            if (req.BarsCount.HasValue)
            {
                sar = sar.TakeLast(req.BarsCount.GetValueOrDefault(5000)).ToList();
            }

            result.sar = sar;


            var trendDates = container.TimeD.ToList();
            var barsBack = extendedTrendData.Slow.Count;
            if (trendDates.Count > barsBack)
            {
                trendDates = trendDates.TakeLast(barsBack).ToList();
            }

            result.rtd = new RTDCalculationResponse
            {
                dates = trendDates,
                fast = extendedTrendData.Fast,
                slow = extendedTrendData.Slow,
                fast_2 = extendedTrendData.Fast2,
                slow_2 = extendedTrendData.Slow2,
                global_trend_spread = extendedTrendData.GlobalTrendSpread,
                local_trend_spread = extendedTrendData.LocalTrendSpread,
                global_avg = extendedTrendData.GlobalAvg,
                local_avg = extendedTrendData.LocalAvg,
                id = req.Id
            };

            try
            {
                // var prediction = await _levelsPrediction.Predict(scanningHistory, levelsList);
                // if (prediction != null)
                // {
                //     result.support_prob = prediction.support;
                //     result.support_ext_prob = prediction.support_ext;
                //     result.resistance_prob = prediction.resistance;
                //     result.resistance_ext_prob = prediction.resistance_ext;
                // } 

                var predict = req.Predict.GetValueOrDefault(false);
                var additionalLevels = req.AdditionalLevels.GetValueOrDefault(false);
                result.prediction_exists = predict;

                if (additionalLevels)
                {
                    result.sar_additional = await CalculateTradeZoneLevels(container);
                }

                if (predict)
                {
                    try
                    {
                        if (granularity == TimeframeHelper.MIN15_GRANULARITY || granularity == TimeframeHelper.HOURLY_GRANULARITY ||
                            granularity == TimeframeHelper.HOUR4_GRANULARITY || granularity == TimeframeHelper.DAILY_GRANULARITY)
                        {
                            var lvls = await _levelsPrediction.PredictLgbm(scanningHistory, container.Symbol, granularity);
                            var lastTime = sar.LastOrDefault().date;
                            if (lvls != null)
                            {
                                result.sar_prediction = new List<SaRResponse>();
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_1, lvls.upper_2_step_1, lvls.lower_1_step_1, lvls.lower_2_step_1, lastTime + (granularity)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_2, lvls.upper_2_step_2, lvls.lower_1_step_2, lvls.lower_2_step_2, lastTime + (granularity * 2)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_3, lvls.upper_2_step_3, lvls.lower_1_step_3, lvls.lower_2_step_3, lastTime + (granularity * 3)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_4, lvls.upper_2_step_4, lvls.lower_1_step_4, lvls.lower_2_step_4, lastTime + (granularity * 4)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_5, lvls.upper_2_step_5, lvls.lower_1_step_5, lvls.lower_2_step_5, lastTime + (granularity * 5)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_6, lvls.upper_2_step_6, lvls.lower_1_step_6, lvls.lower_2_step_6, lastTime + (granularity * 6)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_7, lvls.upper_2_step_7, lvls.lower_1_step_7, lvls.lower_2_step_7, lastTime + (granularity * 7)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_8, lvls.upper_2_step_8, lvls.lower_1_step_8, lvls.lower_2_step_8, lastTime + (granularity * 8)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_9, lvls.upper_2_step_9, lvls.lower_1_step_9, lvls.lower_2_step_9, lastTime + (granularity * 9)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_10, lvls.upper_2_step_10, lvls.lower_1_step_10, lvls.lower_2_step_10, lastTime + (granularity * 10)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_11, lvls.upper_2_step_11, lvls.lower_1_step_11, lvls.lower_2_step_11, lastTime + (granularity * 11)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_12, lvls.upper_2_step_12, lvls.lower_1_step_12, lvls.lower_2_step_12, lastTime + (granularity * 12)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_13, lvls.upper_2_step_13, lvls.lower_1_step_13, lvls.lower_2_step_13, lastTime + (granularity * 13)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_14, lvls.upper_2_step_14, lvls.lower_1_step_14, lvls.lower_2_step_14, lastTime + (granularity * 14)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_15, lvls.upper_2_step_15, lvls.lower_1_step_15, lvls.lower_2_step_15, lastTime + (granularity * 15)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_16, lvls.upper_2_step_16, lvls.lower_1_step_16, lvls.lower_2_step_16, lastTime + (granularity * 16)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_17, lvls.upper_2_step_17, lvls.lower_1_step_17, lvls.lower_2_step_17, lastTime + (granularity * 17)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_18, lvls.upper_2_step_18, lvls.lower_1_step_18, lvls.lower_2_step_18, lastTime + (granularity * 18)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_19, lvls.upper_2_step_19, lvls.lower_1_step_19, lvls.lower_2_step_19, lastTime + (granularity * 19)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_20, lvls.upper_2_step_20, lvls.lower_1_step_20, lvls.lower_2_step_20, lastTime + (granularity * 20)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_21, lvls.upper_2_step_21, lvls.lower_1_step_21, lvls.lower_2_step_21, lastTime + (granularity * 21)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_22, lvls.upper_2_step_22, lvls.lower_1_step_22, lvls.lower_2_step_22, lastTime + (granularity * 22)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_23, lvls.upper_2_step_23, lvls.lower_1_step_23, lvls.lower_2_step_23, lastTime + (granularity * 23)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_24, lvls.upper_2_step_24, lvls.lower_1_step_24, lvls.lower_2_step_24, lastTime + (granularity * 24)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_25, lvls.upper_2_step_25, lvls.lower_1_step_25, lvls.lower_2_step_25, lastTime + (granularity * 25)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_26, lvls.upper_2_step_26, lvls.lower_1_step_26, lvls.lower_2_step_26, lastTime + (granularity * 26)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_27, lvls.upper_2_step_27, lvls.lower_1_step_27, lvls.lower_2_step_27, lastTime + (granularity * 27)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_28, lvls.upper_2_step_28, lvls.lower_1_step_28, lvls.lower_2_step_28, lastTime + (granularity * 28)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_29, lvls.upper_2_step_29, lvls.lower_1_step_29, lvls.lower_2_step_29, lastTime + (granularity * 29)));
                                result.sar_prediction.Add(toSar(lvls.upper_1_step_30, lvls.upper_2_step_30, lvls.lower_1_step_30, lvls.lower_2_step_30, lastTime + (granularity * 30)));

                            }
                        }
                    }
                    catch (Exception ex)
                    { }

                    try
                    {
                        if (granularity == TimeframeHelper.MIN15_GRANULARITY || granularity == TimeframeHelper.HOURLY_GRANULARITY ||
                            granularity == TimeframeHelper.HOUR4_GRANULARITY || granularity == TimeframeHelper.DAILY_GRANULARITY)
                        {
                            var tr = await _levelsPrediction.PredictTrend(scanningHistory, container.Symbol, granularity);
                            if (tr != null)
                            {
                                result.mema_prediction = tr.mama;
                                result.fama_prediction = tr.fama;
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prediction error");
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private async Task<Dictionary<int, List<SaRResponse>>> CalculateTradeZoneLevels(InputDataContainer container)
        {
            var levelsResult = new Dictionary<int, List<SaRResponse>>();

            var granularity = AlgoHelper.ConvertTimeframeToGranularity(container.TimeframeInterval, container.TimeframePeriod);
            var granularity_list = new List<int>();
            if (granularity == TimeframeHelper.MIN1_GRANULARITY)
            {
                granularity_list.Add(TimeframeHelper.MIN5_GRANULARITY);
                granularity_list.Add(TimeframeHelper.MIN15_GRANULARITY);
                granularity_list.Add(TimeframeHelper.HOURLY_GRANULARITY);
            }
            else if (granularity == TimeframeHelper.MIN5_GRANULARITY)
            {
                granularity_list.Add(TimeframeHelper.MIN15_GRANULARITY);
                granularity_list.Add(TimeframeHelper.HOURLY_GRANULARITY);
                granularity_list.Add(TimeframeHelper.HOUR4_GRANULARITY);
            }
            else if (granularity == TimeframeHelper.MIN15_GRANULARITY || granularity == TimeframeHelper.MIN30_GRANULARITY)
            {
                granularity_list.Add(TimeframeHelper.HOURLY_GRANULARITY);
                granularity_list.Add(TimeframeHelper.HOUR4_GRANULARITY);
                granularity_list.Add(TimeframeHelper.DAILY_GRANULARITY);
            }
            else if (granularity == TimeframeHelper.HOURLY_GRANULARITY)
            {
                granularity_list.Add(TimeframeHelper.HOUR4_GRANULARITY);
                granularity_list.Add(TimeframeHelper.DAILY_GRANULARITY);
            }
            else if (granularity == TimeframeHelper.HOUR4_GRANULARITY)
            {
                granularity_list.Add(TimeframeHelper.DAILY_GRANULARITY);
            }
            else
            {
                return levelsResult;
            }

            var tasksToWait = new List<Task<HistoryData>>();

            foreach (var g in granularity_list)
            {
                try
                {
                    var task = _historyService.GetHistory(container.Symbol, g, container.Datafeed, container.Exchange, container.Type, container.ReplayBack);
                    tasksToWait.Add(task);
                }
                catch (Exception ex)
                {

                }
            }
            try
            {
                var historicalDataArray = await Task.WhenAll<HistoryData>(tasksToWait);

                foreach (var historicalData in historicalDataArray)
                {
                    var high = historicalData.Bars.Select(_ => _.High);
                    var low = historicalData.Bars.Select(_ => _.Low);
                    var dates = historicalData.Bars.Select(_ => _.Timestamp).ToList();
                    var levelsList = TechCalculations.CalculateLevelsBasedOnTradeZone(high, low);
                    var sar = new List<SaRResponse>();
                    for (var i = 0; i < levelsList.Count; i++)
                    {
                        var l = levelsList[i];
                        sar.Add(new SaRResponse
                        {
                            date = dates[i],
                            r_p28 = l.Plus28,
                            r_p18 = l.Plus18,
                            r = l.EightEight,
                            n = l.FourEight,
                            s = l.ZeroEight,
                            s_m18 = l.Minus18,
                            s_m28 = l.Minus28
                        });
                    }
                    levelsResult.Add((int)(historicalData.Granularity), sar.TakeLast(5).ToList());
                }
            }
            catch (Exception ex)
            {

            }

            return levelsResult;
        }

        private SaRResponse toSar(decimal upper_1, decimal upper_2, decimal lower_1, decimal lower_2, long date)
        {
            return new SaRResponse
            {
                r = upper_1,
                r_p28 = upper_2,
                r_p18 = (upper_1 + upper_2) / 2,
                s = lower_1,
                s_m28 = lower_2,
                s_m18 = (lower_1 + lower_2) / 2,
                n = (upper_1 + lower_1) / 2,
                date = date
            };
        }

        private async Task<CalculatePositionSizeResponse> calculatePositionSize(CalculatePositionSizeRequest req)
        {
            var type = req.Instrument.Type.ToLowerInvariant();
            var datafeed = req.Instrument.Datafeed.ToLowerInvariant();
            var exchange = req.Instrument.Exchange.ToLowerInvariant();
            var symbol = req.Instrument.Id;
            var suggestedRisk = req.InputRisk;
            var priceDiff = req.PriceDiff;
            var contractSize = req.ContractSize;
            var usdRatio = 1m;

            if (type == "forex")
            {
                usdRatio = await _priceRatioCalculationService.GetSymbolRatio(symbol, req.AccountCurrency, datafeed, type, exchange);
            }

            var accountSize = req.InputAccountSize * usdRatio;

            var size = AlgoHelper.CalculatePositionValue(type, symbol, accountSize, suggestedRisk, priceDiff, contractSize);

            return new CalculatePositionSizeResponse
            {
                size = size
            };
        }

        private async Task<CalculatePriceRatioResponse> calculatePriceRatio(CalculatePriceRatioRequest req)
        {
            var type = req.Instrument.Type.ToLowerInvariant();
            var datafeed = req.Instrument.Datafeed.ToLowerInvariant();
            var exchange = req.Instrument.Exchange.ToLowerInvariant();
            var symbol = req.Instrument.Id;
            var usdRatio = 1m;

            if (type == "forex")
            {
                usdRatio = await _priceRatioCalculationService.GetSymbolRatio(symbol, req.AccountCurrency, datafeed, type, exchange);
            }

            return new CalculatePriceRatioResponse
            {
                ratio = usdRatio
            };
        }

        private CalculationMarketInfoResponse tryGetCalculateMarketInfoFromCache(Instrument instrument)
        {
            var hash = instrument.ToString() + "_marketinfo";
            try
            {
                if (_cache.TryGetValue(_cachePrefix, hash, out CalculationMarketInfoResponse cachedResponse))
                {
                    return cachedResponse;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get cached response for marketinfo");
                _logger.LogError(e.Message);
            }

            return null;
        }

        private CalculationMarketInfoResponse tryGetCalculateMarketInfoV2FromCache(Instrument instrument, int timeframe)
        {
            var hash = instrument.ToString() + timeframe + "_marketinfoV2";
            try
            {
                if (_cache.TryGetValue(_cachePrefix, hash, out CalculationMarketInfoResponse cachedResponse))
                {
                    return cachedResponse;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get cached response for marketinfo");
                _logger.LogError(e.Message);
            }

            return null;
        }

        private decimal? tryGetCVarFromCache(Instrument instrument)
        {
            var hash = instrument.ToString() + "_cvar";
            try
            {
                if (_cache.TryGetValue(_cachePrefix, hash, out decimal cachedResponse))
                {
                    return cachedResponse;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get cached response for cvar");
                _logger.LogError(e.Message);
            }

            return null;
        }

        private void tryAddCalculateMarketInfoInCache(Instrument instrument, CalculationMarketInfoResponse data)
        {
            var hash = instrument.ToString() + "_marketinfo";
            try
            {
                _cache.Set(_cachePrefix, hash, data, TimeSpan.FromMinutes(10));
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to add cached response for marketinfo");
                _logger.LogError(e.Message);
            }
        }

        private void tryAddCalculateMarketInfoV2InCache(Instrument instrument, int timeframe, CalculationMarketInfoResponse data)
        {
            var hash = instrument.ToString() + timeframe + "_marketinfoV2";
            try
            {
                _cache.Set(_cachePrefix, hash, data, TimeSpan.FromMinutes(10));
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to add cached response for marketinfo");
                _logger.LogError(e.Message);
            }
        }

        private void tryAddCVarInCache(Instrument instrument, decimal value)
        {
            var hash = instrument.ToString() + "_cvar";
            try
            {
                _cache.Set(_cachePrefix, hash, value, TimeSpan.FromDays(1));
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to add cached response for marketinfo");
                _logger.LogError(e.Message);
            }
        }

        // -------- for ML //

        public Task<MLDataResponse> CalculateSRAsync(string symbol, int granularity)
        {
            return Task.Run(async () =>
            {
                return await calculateSRAsync(symbol, granularity);
            });
        }

        private async Task<MLDataResponse> calculateSRAsync(string symbol, int granularity)
        {
            var res = new MLDataResponse();
            res.data = new List<MLDataResponseItem>();

            var data = await _historyService.GetHistory(symbol, granularity, "Oanda", "Oanda", "Forex");
            var bars = data.Bars.ToList();

            for (var i = 0; i < bars.Count; i++)
            {
                var item = new MLDataResponseItem
                {
                    open = bars[i].Open,
                    high = bars[i].High,
                    low = bars[i].Low,
                    close = bars[i].Close,
                    time = bars[i].Timestamp
                };

                res.data.Add(item);

                if (i > 128)
                {
                    var high = res.data.Select(_ => _.high);
                    var low = res.data.Select(_ => _.low);
                    var levels = TechCalculations.CalculateLevel128(high, low);

                    item.upExt1 = levels.Plus18;
                    item.upExt2 = levels.Plus28;
                    item.downExt1 = levels.Minus18;
                    item.downExt2 = levels.Minus28;

                    item.n = levels.FourEight;
                    item.s = levels.ZeroEight;
                    item.r = levels.EightEight;
                }
            }

            return res;
        }

    }
}
