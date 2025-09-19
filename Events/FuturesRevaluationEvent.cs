using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Venture
{
    public class FuturesRevaluationEvent : FuturesEvent
    {
        public FuturesRevaluationEvent(Futures parentAsset, decimal count, decimal price, decimal amount, DateTime date) : base(parentAsset, date)
        {
            UniqueId = $"FuturesRevaluation_{parentAsset.UniqueId}_{date.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            Amount = amount;
            Price = price;

            FXRate = FX.GetRate(date, parentAsset.Currency);

            IsTotalDerecognition = (date == parentAsset.MaturityDate);
        }
    }
}
