﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Events
{
    public enum FlowType { Undefined, Dividend, Coupon, Redemption }

    public class Flow : Event
    {
        public DateTime RecordDate { get; protected set; }

        public FlowType FlowType { get; protected set; }

        public decimal Rate { get; protected set; }

        public decimal Tax { get; protected set; }

        public Flow(Assets.Asset parentAsset, DateTime recordDate, DateTime timestamp, FlowType type, decimal rate, decimal fxRate) : base(parentAsset)
        {
            RecordDate = recordDate;
            Timestamp = timestamp;
            FlowType = type;
            Rate = rate;
            FXRate = fxRate;
            RecalculateAmount();
        }

        public void RecalculateAmount()
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, RecordDate);
            Data.Manual? manualAdjustment;
            switch (FlowType)
            {
                case FlowType.Undefined: throw new Exception("Cannot recalculate amount for undefined flow event.");
                case FlowType.Dividend:
                    Amount = Math.Round(Rate * ParentAsset.GetCount(time), 2);
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.DividendTaxAdjustment, Timestamp, (ParentAsset as Assets.Security)?.SecurityDefinition?.InstrumentId ?? "");
                    if (manualAdjustment != null)
                    { Tax = manualAdjustment.Amount1; }
                    else
                    { Tax = TaxCalculations.CalculateFromDividend(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Coupon:
                    Amount = Math.Round(Rate * ParentAsset.GetNominalAmount(time), 2);
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.CouponTaxAdjustment, Timestamp, (ParentAsset as Assets.Security)?.SecurityDefinition?.InstrumentId ?? "");
                    if (manualAdjustment != null)
                    { Tax = manualAdjustment.Amount1; }
                    else
                    { Tax = TaxCalculations.CalculateFromCoupon(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Redemption:
                    Amount = Math.Round(ParentAsset.GetNominalAmount(time), 2);
                    Tax = 0;
                    break;
                default: throw new Exception("Cannot recalculate amount for undefined flow event.");
            }
        }

        public override string ToString()
        {
            return $"Event:Flow ({FlowType}) @{Timestamp:yyyy-MM-dd}: {Amount:0.00} {ParentAsset.Currency}";
        }

    }
}
