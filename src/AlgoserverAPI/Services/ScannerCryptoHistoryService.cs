using System;
using System.Collections.Generic;
using System.Linq;
using Algoserver.API.Helpers;
using Algoserver.API.Models.REST;

namespace Algoserver.API.Services
{
    public class ScannerCryptoHistoryService : ScannerHistoryService
    {
        public ScannerCryptoHistoryService(HistoryService historyService, InstrumentService instrumentService): base(historyService, instrumentService)
        {
        }

        protected override List<IInstrument> _getInstruments() 
        {
            var instruments = new List<IInstrument>();
            var cryptoInstruments = _instrumentService.GetKaikoInstruments();
            var allowedCrypto = InstrumentsHelper.CryptoInstrumentList;

            foreach (var instrument in cryptoInstruments) {
                if (allowedCrypto.Any(_ => String.Equals(_.Symbol, instrument.Symbol, StringComparison.InvariantCultureIgnoreCase) && String.Equals(_.Exchange, instrument.Exchange, StringComparison.InvariantCultureIgnoreCase))) {
                    if (!instruments.Any(_ => String.Equals(_.Symbol, instrument.Symbol, StringComparison.InvariantCultureIgnoreCase) && String.Equals(_.Exchange, instrument.Exchange, StringComparison.InvariantCultureIgnoreCase))) {
                        instruments.Add(instrument);
                    }
                }
            }
            
            return instruments;
        }
    }
}