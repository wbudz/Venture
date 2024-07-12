using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class FuturesEventViewEntry : ModuleEntry
    {
        public string Type { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public decimal Amount { get; set; }

        public decimal FXRate { get; set; }

        public decimal? CountChange { get; set; } = null;

        public decimal CountPrevious { get; set; }

        public decimal CountNew { get; set; }

        public decimal? Price { get; set; } = null;

        public decimal? Fee { get; set; } = null;

        public decimal Multiplier { get; set; }

        public FuturesEventViewEntry(FuturesEvent e, decimal previousCount, decimal multiplier)
        {
            UniqueId = e.UniqueId;

            if (e is FuturesRecognitionEvent) Type = "Recognition";
            if (e is FuturesRevaluationEvent) Type = "Revaluation";

            Timestamp = e.Timestamp;
            Amount = e.Amount;
            Currency = e.Currency;
            FXRate = e.FXRate;
            Price = e.Price;
            CountPrevious = previousCount;
            CountNew = CountPrevious;
            Multiplier = multiplier;

            if (e is FuturesRecognitionEvent fre)
            {
                CountChange = fre.Count;
                CountNew += fre.Count;
                Fee = fre.Fee;
            }
            
        }
    }
}
