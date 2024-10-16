﻿using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class ValuationEvent : StandardAssetEvent
    {
        public decimal GrossAmount { get { return Amount; } }

        public decimal MarketDirtyPrice { get; protected set; } = 0;

        public decimal MarketCleanPrice { get; protected set; } = 0;

        public decimal AmortizedCostDirtyPrice { get; protected set; } = 0;

        public decimal AmortizedCostCleanPrice { get; protected set; } = 0;

        public decimal MarketDirtyAmount { get; protected set; } = 0;

        public decimal MarketCleanAmount { get; protected set; } = 0;

        public decimal AmortizedCostDirtyAmount { get; protected set; } = 0;

        public decimal AmortizedCostCleanAmount { get; protected set; } = 0;

        public decimal CumulativeMarketValuation { get; protected set; } = 0;

        public decimal CumulativeAmortizedCostValuation { get; protected set; } = 0;

        public bool IsRedemptionValuation { get; protected set; } = false;

        public ValuationEvent(StandardAsset parentAsset, DateTime date, bool redemptionValuation) : base(parentAsset, date)
        {
            UniqueId = $"ValuationEvent_{parentAsset.UniqueId}_{date.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            decimal accruedInterest = parentAsset.GetAccruedInterest(date);

            Count = parentAsset.GetCount(time);
            FXRate = 0; // TODO: Implement FX rates

            if (redemptionValuation)
            {
                MarketDirtyPrice = parentAsset.GetNominalPrice();
                MarketCleanPrice = parentAsset.GetNominalPrice();
                AmortizedCostDirtyPrice = parentAsset.GetNominalPrice();
                AmortizedCostCleanPrice = parentAsset.GetNominalPrice();

                MarketDirtyAmount = parentAsset.GetNominalAmount();
                MarketCleanAmount = parentAsset.GetNominalAmount();
                AmortizedCostDirtyAmount = parentAsset.GetNominalAmount();
                AmortizedCostCleanAmount = parentAsset.GetNominalAmount();

                Amount = MarketDirtyAmount;

                decimal purchaseAmount = parentAsset.GetPurchaseAmount(true);
                CumulativeAmortizedCostValuation = AmortizedCostDirtyAmount - purchaseAmount;
                CumulativeMarketValuation = MarketDirtyAmount - purchaseAmount - CumulativeAmortizedCostValuation;
            }
            else
            {
                MarketDirtyPrice = parentAsset.GetMarketPrice(time, true);
                MarketCleanPrice = MarketDirtyPrice - accruedInterest;
                AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
                AmortizedCostCleanPrice = AmortizedCostDirtyPrice - accruedInterest;

                MarketDirtyAmount = parentAsset.GetMarketValue(time, true);
                MarketCleanAmount = parentAsset.GetMarketValue(time, false);
                AmortizedCostDirtyAmount = parentAsset.GetAmortizedCostValue(time, true);
                AmortizedCostCleanAmount = parentAsset.GetAmortizedCostValue(time, false);

                Amount = MarketDirtyAmount;

                decimal purchaseAmount = parentAsset.GetPurchaseAmount(time, true);
                CumulativeAmortizedCostValuation = AmortizedCostDirtyAmount - purchaseAmount;
                CumulativeMarketValuation = MarketDirtyAmount - purchaseAmount - CumulativeAmortizedCostValuation;
            }

            IsRedemptionValuation = redemptionValuation;

        }
    }
}
