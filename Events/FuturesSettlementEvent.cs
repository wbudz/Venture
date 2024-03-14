using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Venture
{
    public class FuturesSettlementEvent : Event
    {
        public decimal Count { get; protected set; } = 0;

        public bool IsTotalDerecognition { get; protected set; } = false;

        public FuturesSettlementEvent(Futures parentAsset, decimal count, decimal amount, DateTime date) : base(parentAsset, date)
        {
            UniqueId = $"FuturesSettlement_{parentAsset.UniqueId}_{date.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            Count = count;
            Amount = amount;

            FXRate = FX.GetRate(date, parentAsset.Currency);

            IsTotalDerecognition = (date == parentAsset.MaturityDate);
        }
    }
}
