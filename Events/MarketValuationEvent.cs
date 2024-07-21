using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class MarketValuationEvent : StandardAssetEvent
    {
        public decimal Count { get; protected set; } = 0;

        public decimal GrossAmount { get { return Amount; } }

        public decimal MarketDirtyPrice { get; protected set; } = 0;

        public decimal MarketCleanPrice { get; protected set; } = 0;

        public decimal AmortizedCostDirtyPrice { get; protected set; } = 0;

        public decimal AmortizedCostCleanPrice { get; protected set; } = 0;

        public decimal MarketDirtyAmount { get; protected set; } = 0;

        public decimal MarketCleanAmount { get; protected set; } = 0;

        public decimal AmortizedCostDirtyAmount { get; protected set; } = 0;

        public decimal AmortizedCostCleanAmount { get; protected set; } = 0;

        public MarketValuationEvent(StandardAsset parentAsset, DateTime date) : base(parentAsset, date)
        {
            UniqueId = $"ValuationEvent_{parentAsset.UniqueId}_{date.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            decimal accruedInterest = parentAsset.GetAccruedInterest(date);

            Count = parentAsset.GetCount(time);
            FXRate = 0; // TODO: Implement FX rates

            MarketDirtyPrice = parentAsset.GetMarketPrice(time, true);
            MarketCleanPrice = MarketDirtyPrice - accruedInterest;
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = AmortizedCostDirtyPrice - accruedInterest;

            if (parentAsset.IsBond)
            {
                MarketDirtyAmount = Common.Round(MarketDirtyPrice / 100 * Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                MarketCleanAmount = Common.Round(MarketCleanPrice / 100 * Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice / 100 * Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice / 100 * Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                MarketDirtyAmount = Common.Round(MarketDirtyPrice * Count);
                MarketCleanAmount = Common.Round(MarketCleanPrice * Count);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice * Count);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice * Count);
            }

            Amount = MarketDirtyAmount - AmortizedCostDirtyAmount;
        }
    }
}
