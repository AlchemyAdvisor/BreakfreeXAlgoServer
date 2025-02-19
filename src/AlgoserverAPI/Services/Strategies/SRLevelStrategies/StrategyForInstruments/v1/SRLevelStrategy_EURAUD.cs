using System.Threading.Tasks;
using Algoserver.API.Services;
using Algoserver.Strategies.LevelStrategy;

namespace Algoserver.Strategies.SRLevelStrategy
{
    public class SRLevelStrategy_EURAUD : SRLevelStrategyBase
    {
        public SRLevelStrategy_EURAUD(StrategyInputContext _context) : base(_context)
        {
        }

        public override async Task<SRLevelStrategyResponse> Calculate()
        {
            var settings = new SRLevelStrategySettings
            {
                UseCatReflex = true,
                CatReflexGranularity = TimeframeHelper.DAILY_GRANULARITY,
                CatReflexPeriodReflex = 18,
                CatReflexPeriodSuperSmoother = 40,
                CatReflexPeriodPostSmooth = 6,
                CatReflexConfirmationPeriod = 10,
                CatReflexMinLevel = 0.1,
                CatReflexMaxLevel = 2.1,
                CatReflexValidateZeroCrossover = false
            };

            var result = await CalculateInternal(settings);
            return result;
        }
    }
}