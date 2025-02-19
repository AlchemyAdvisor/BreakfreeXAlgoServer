using System.Threading.Tasks;
using Algoserver.API.Services;
using Algoserver.Strategies.LevelStrategy;

namespace Algoserver.Strategies.NLevelStrategy.V3
{
    // Settings version SPX500_combined_v4.2_2209.set
    public class NLevelStrategy_SPX500_v3 : NLevelStrategyBase
    {
        public NLevelStrategy_SPX500_v3(StrategyInputContext _context) : base(_context)
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
                //     strengthConditionFilter15m = true
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
                // PeakDetectionGranularity = TimeframeHelper.MIN15_GRANULARITY,
                // PeakDetectionPeriod = 93,
                // PeakDetectionThreshold = 80,
                CheckStochastic = false,
                // StochasticGranularity = TimeframeHelper.HOUR4_GRANULARITY, // 8H in settings, we dont have this TF
                // StochasticPeriodK = 120, // 60 in settings for 8H TF, 120 for 4H
                // StochasticPeriodD = 60, // 30 in settings for 8H TF, 60 for 4H
                // StochasticSmooth = 78, // 39 in settings for 8H TF, 78 for 4H
                // StochasticThreshold = 39,

                UseCatReflex = true,
                CatReflexGranularity = TimeframeHelper.MIN1_GRANULARITY,
                CatReflexPeriodReflex = 39,
                CatReflexPeriodSuperSmoother = 12,
                CatReflexPeriodPostSmooth = 246,
                CatReflexConfirmationPeriod = 3,
                CatReflexMinLevel = 0,
                CatReflexMaxLevel = 3.4,
                CatReflexValidateZeroCrossover = false,

                UseCatReflex2 = true,
                CatReflexGranularity2 = TimeframeHelper.HOURLY_GRANULARITY,
                CatReflexPeriodReflex2 = 75,
                CatReflexPeriodSuperSmoother2 = 57,
                CatReflexPeriodPostSmooth2 = 552,
                CatReflexConfirmationPeriod2 = 5,
                CatReflexMinLevel2 = 0,
                CatReflexMaxLevel2 = 1.8,
                CatReflexValidateZeroCrossover2 = false,

                UseCatReflex3 = true,
                CatReflexGranularity3 = TimeframeHelper.HOUR4_GRANULARITY,
                CatReflexPeriodReflex3 = 12,
                CatReflexPeriodSuperSmoother3 = 39,
                CatReflexPeriodPostSmooth3 = 201,
                CatReflexConfirmationPeriod3 = 3,
                CatReflexMinLevel3 = 0,
                CatReflexMaxLevel3 = 3.4,
                CatReflexValidateZeroCrossover3 = false,
            };

            var result = await CalculateInternal(settings);
            
            return result;
        }

    }
}