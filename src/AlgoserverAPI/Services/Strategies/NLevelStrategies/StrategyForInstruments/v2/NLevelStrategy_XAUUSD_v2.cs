using System.Threading.Tasks;
using Algoserver.API.Services;
using Algoserver.Strategies.LevelStrategy;

namespace Algoserver.Strategies.NLevelStrategy.V2
{
    public class NLevelStrategy_XAUUSD_v2 : NLevelStrategyBase
    {
        public NLevelStrategy_XAUUSD_v2(StrategyInputContext _context) : base(_context)
        {
        }

        public override async Task<NLevelStrategyResponse> Calculate()
        {
            var settings = new NLevelStrategySettings
            {
                UseVolatilityFilter = false,
                // VolatilityGranularity = TimeframeHelper.MIN15_GRANULARITY,
                // VolatilityMin = -47,
                // VolatilityMax = 103,
                UseVolatilityFilter2 = false,
                // VolatilityGranularity2 = TimeframeHelper.MIN1_GRANULARITY,
                // VolatilityMin2 = -50,
                // VolatilityMax2 = 94,
                UseOverheatZone1DFilter = true,
                OverheatZone1DThreshold = 5,
                CheckTrends = false,
                // TrendFilters = new TrendFiltersSettings {
                //     strengthConditionFilter1h = true
                // },
                CheckTrendsStrength = true,
                LowGroupStrength = 0,
                HighGroupStrength = 1,
                CheckRSI = false,
                // RSIMin = 0,
                // RSIMax = 75,
                // RSIPeriod = 57,
                CheckStrengthIncreasing = false,
                // CheckStrengthReducePeriod = 4,
                // CheckStrengthResetPeriod = 6,
                // CheckStrengthReduceGranularity = TimeframeHelper.MIN1_GRANULARITY * -1,
                // CheckStrengthResetGranularity = TimeframeHelper.MIN1_GRANULARITY * -1,
                CheckPeaks = false,
                // PeakDetectionGranularity = TimeframeHelper.DAILY_GRANULARITY,
                // PeakDetectionPeriod = 37,
                // PeakDetectionThreshold = 90,
                CheckStochastic = false,
                // StochasticGranularity = TimeframeHelper.HOUR4_GRANULARITY, // 8H in settings, we dont have this TF
                // StochasticPeriodK = 120, // 60 in settings for 8H TF, 120 for 4H
                // StochasticPeriodD = 60, // 30 in settings for 8H TF, 60 for 4H
                // StochasticSmooth = 78, // 39 in settings for 8H TF, 78 for 4H
                // StochasticThreshold = 39,

                UseCatReflex = true,
                CatReflexGranularity = TimeframeHelper.MIN1_GRANULARITY,
                CatReflexPeriodReflex = 42,
                CatReflexPeriodSuperSmoother = 4,
                CatReflexPeriodPostSmooth = 29,
                CatReflexConfirmationPeriod = 4,
                CatReflexMinLevel = 0.07,
                CatReflexMaxLevel = 2.2,
                CatReflexValidateZeroCrossover = true,

                UseCatReflex2 = true,
                CatReflexGranularity2 = TimeframeHelper.HOUR4_GRANULARITY,
                CatReflexPeriodReflex2 = 120,
                CatReflexPeriodSuperSmoother2 = 148,
                CatReflexPeriodPostSmooth2 = 113,
                CatReflexConfirmationPeriod2 = 4,
                CatReflexMinLevel2 = 0.01,
                CatReflexMaxLevel2 = 2.0,
                CatReflexValidateZeroCrossover2 = true
            };

            var result = await CalculateInternal(settings);
            result.DDClosePositions = true;
            result.DDCloseInitialInterval = 176;
            result.DDCloseIncreasePeriod = 135;
            result.DDCloseIncreaseThreshold = 0.5m;
            
            return result;
        }

    }
}