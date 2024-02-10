using Microsoft.SolverFoundation.Services;
using System;
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

        public decimal Tax { get; protected set; } = 0;

        public decimal GrossAmount { get { return Amount + Tax; } }

        public Flow(Assets.Asset parentAsset, DateTime recordDate, DateTime timestamp, FlowType type, decimal rate, decimal fxRate) : base(parentAsset, timestamp)
        {
            UniqueId = $"Flow_{type}_{parentAsset.UniqueId}_{timestamp.ToString("yyyyMMdd")}";
            RecordDate = recordDate;
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
                    // Dividend amount, with potential adjustments
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.DividendAmountAdjustment, Timestamp, ParentAsset.UniqueId ?? "");
                    if (manualAdjustment != null)
                    {
                        Amount = manualAdjustment.Amount1;
                    }
                    else
                    {
                        Amount = Common.Round(Rate * ParentAsset.GetCount(time));
                    }
                    // Tax, with potential adjustments
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.DividendTaxAdjustment, Timestamp, ParentAsset.UniqueId ?? "");
                    if (manualAdjustment != null)
                    { Tax = manualAdjustment.Amount1; }
                    else if (Globals.TaxFreePortfolios.Contains(ParentAsset.Portfolio))
                    { Tax = 0; }
                    else
                    { Tax = TaxCalculations.CalculateFromDividend(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Coupon:
                    // Coupon amount, with potential adjustments
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.CouponAmountAdjustment, Timestamp, ParentAsset.UniqueId ?? "");
                    if (manualAdjustment != null)
                    {
                        Amount = manualAdjustment.Amount1;
                    }
                    else
                    {
                        Amount = Common.Round(Rate * ParentAsset.GetNominalAmount(time));
                    }
                    // Tax, with potential adjustments
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.CouponTaxAdjustment, Timestamp, ParentAsset.UniqueId ?? "");
                    if (manualAdjustment != null)
                    { Tax = manualAdjustment.Amount1; }
                    else if (Globals.TaxFreePortfolios.Contains(ParentAsset.Portfolio))
                    { Tax = 0; }
                    else
                    { Tax = TaxCalculations.CalculateFromCoupon(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Redemption:
                    Amount = Common.Round(Rate * ParentAsset.GetNominalAmount(time));
                    // Tax, with potential adjustments
                    manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.RedemptionTaxAdjustment, Timestamp, ParentAsset.UniqueId ?? "");
                    if (manualAdjustment != null)
                    { Tax = manualAdjustment.Amount1; }
                    else if (Globals.TaxFreePortfolios.Contains(ParentAsset.Portfolio))
                    { Tax = 0; }
                    else
                    {
                        decimal purchaseAmount = ParentAsset.GetPurchaseAmount(time, true);
                        decimal nominalAmount = ParentAsset.GetNominalAmount(time);
                        Tax = TaxCalculations.CalculateFromRedemption(Amount - Math.Min(purchaseAmount, nominalAmount));
                    }
                    Amount -= Tax;
                    break;
                default: throw new Exception("Cannot recalculate amount for undefined flow event.");
            }
        }

    }
}
