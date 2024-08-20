using System;
using System.Collections.Generic;

namespace Algoserver.API.Models.REST
{
    [Serializable]
    public class AutoTradingInstrumentsResponse
    {
        public string Symbol { get; set; }
        public decimal Risk { get; set; }
        public bool IsDisabled { get; set; }
        public uint TradingStateSR { get; set; }
        public uint TradingStateN { get; set; }  
    }
    
    [Serializable]
    public class AutoTradingInstrumentsDedicationResponse
    {
        public List<AutoTradingInstrumentsResponse> Instruments { get; set; }
        public List<string> DisabledInstruments { get; set; }
        public Dictionary<string, int> Risks { get; set; }
        public Dictionary<string, int> GroupRisks { get; set; }
        public int AccountRisk { get; set; }
        public int DefaultMarketRisk { get; set; }
        public int DefaultGroupRisk { get; set; }
        public bool UseManualTrading { get; set; }
        public bool BotShutDown { get; set; }
    }

    [Serializable]
    public class AutoTradingSymbolInfoResponse
    {
        public float TotalStrength { get; set; }
        public decimal SL { get; set; }
        public decimal HalfBand1M { get; set; }
        public decimal HalfBand5M { get; set; }
        public decimal HalfBand15M { get; set; }
        public decimal HalfBand1H { get; set; }
        public decimal HalfBand4H { get; set; }
        public decimal HalfBand1D { get; set; }
        public decimal Entry1M { get; set; }
        public decimal Entry5M { get; set; }
        public decimal Entry15M { get; set; }
        public decimal Entry1H { get; set; }
        public decimal Entry4H { get; set; }
        public decimal Entry1D { get; set; }
        public decimal TP1M { get; set; }
        public decimal TP5M { get; set; }
        public decimal TP15M { get; set; }
        public decimal TP1H { get; set; }
        public decimal TP4H { get; set; }
        public decimal TP1D { get; set; }
        public decimal Strength1M { get; set; }
        public decimal Strength5M { get; set; }
        public decimal Strength15M { get; set; }
        public decimal Strength1H { get; set; }
        public decimal Strength4H { get; set; }
        public decimal Strength1D { get; set; }
        public decimal Strength1Month { get; set; }
        public decimal Strength1Y { get; set; }
        public decimal Strength10Y { get; set; }
        public int Phase1M { get; set; }
        public int Phase5M { get; set; }
        public int Phase15M { get; set; }
        public int Phase1H { get; set; }
        public int Phase4H { get; set; }
        public int Phase1D { get; set; }
        public int Phase1Month { get; set; }
        public int Phase1Y { get; set; }
        public int Phase10Y { get; set; }
        public int State1M { get; set; }
        public int State5M { get; set; }
        public int State15M { get; set; }
        public int State1H { get; set; }
        public int State4H { get; set; }
        public int State1D { get; set; }
        public int State1Month { get; set; }
        public int State1Y { get; set; }
        public int State10Y { get; set; }
        public decimal Volatility1M { get; set; }
        public decimal Volatility15M { get; set; }
        public decimal Volatility1H { get; set; }
        public decimal Volatility1D { get; set; }
        public int TrendDirection { get; set; }
        public int ShortGroupPhase { get; set; }
        public int MidGroupPhase { get; set; }
        public int LongGroupPhase { get; set; }
        public decimal ShortGroupStrength { get; set; }
        public decimal MidGroupStrength { get; set; }
        public decimal LongGroupStrength { get; set; }
        public int CurrentPhase { get; set; }
        public int NextPhase { get; set; }
        public decimal CurrentPrice { get; set; }
        public long Time { get; set; }
        public uint TradingStateSR { get; set; }
        public uint TradingStateN { get; set; }    
        public decimal OppositeSL { get; set; }
        public decimal OppositeEntry1M { get; set; }
        public decimal OppositeEntry5M { get; set; }
        public decimal OppositeEntry15M { get; set; }
        public decimal OppositeEntry1H { get; set; }
        public decimal OppositeEntry4H { get; set; }
        public decimal OppositeEntry1D { get; set; }
        public int OppositeTrendDirection { get; set; }
        public decimal SL1M { get; set; }
        public decimal SL5M { get; set; }
        public decimal SL15M { get; set; }
        public decimal SL1H { get; set; }
        public decimal SL4H { get; set; }
        public decimal OppositeSL1M { get; set; }
        public decimal OppositeSL5M { get; set; }
        public decimal OppositeSL15M { get; set; }
        public decimal OppositeSL1H { get; set; }
        public decimal OppositeSL4H { get; set; }  
        public bool Skip1MinTrades { get; set; }
        public bool Skip5MinTrades { get; set; }
        public bool Skip15MinTrades { get; set; }
        public bool Skip1HourTrades { get; set; }
        public bool Skip4HourTrades { get; set; }
        public decimal MinStrength1M { get; set; }
        public decimal MinStrength5M { get; set; }
        public decimal MinStrength15M { get; set; }
        public decimal MinStrength1H { get; set; }
        public decimal MinStrength4H { get; set; }
        public bool DDClosePositions { get; set; }
        public int DDCloseInitialInterval { get; set; }
        public int DDCloseIncreasePeriod { get; set; }
        public decimal DDCloseIncreaseThreshold { get; set; }
    }

    [Serializable]
    public class AutoTradingInstrumentConfigResponse
    {
        public string Symbol { get; set; }
        public double Risks { get; set; }
        public double MaxRisks { get; set; }
        public bool IsTradable { get; set; }
        public bool IsDisabled { get; set; }
        public uint TradingStateSR { get; set; }
        public uint TradingStateN { get; set; }    
    }
}