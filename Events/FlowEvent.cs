using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public enum FlowType { Undefined, Dividend, Coupon, Redemption }

    public class FlowEvent : Event
    {
        public DateTime RecordDate { get; protected set; }

        public FlowType FlowType { get; protected set; }

        public decimal Rate { get; protected set; }

        public decimal Tax { get; protected set; } = 0;

        public decimal GrossAmount { get { return Amount + Tax; } }

        public FlowEvent(StandardAsset parentAsset, DateTime recordDate, DateTime timestamp, FlowType type, decimal rate, string currency, decimal fxRate) : base(parentAsset, timestamp)
        {
            UniqueId = $"Flow_{type}_{parentAsset.UniqueId}_{timestamp.ToString("yyyyMMdd")}";
            RecordDate = recordDate;
            FlowType = type;
            Rate = rate;
            Currency = currency;
            FXRate = fxRate;
            RecalculateAmount();
        }

        public void RecalculateAmount()
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, RecordDate);
            switch (FlowType)
            {
                case FlowType.Undefined: throw new Exception("Cannot recalculate amount for undefined flow event.");
                case FlowType.Dividend:
                    // Dividend amount, with potential adjustments
                    var daa = Definitions.ManualEvents.OfType<DividendAmountAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == Timestamp && x.AssetUniqueId == ParentAsset.UniqueId);
                    if (daa != null)
                    {
                        Amount = daa.Amount;
                    }
                    else
                    {
                        decimal fxRate;
                        if (ParentAsset.CashAccount.Split(':')[2] != Currency)
                        {
                            fxRate = FXRate;
                        }
                        else
                        {
                            fxRate = 1;
                        }
                        Amount = Common.Round(Rate * ParentAsset.GetCount(time) * fxRate);
                    }
                    // Tax, with potential adjustments
                    var dta = Definitions.ManualEvents.OfType<DividendTaxAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == Timestamp && x.AssetUniqueId == ParentAsset.UniqueId);
                    if (dta != null)
                    { Tax = dta.Tax; }
                    else if (Globals.TaxFreePortfolios.Contains(ParentAsset.Portfolio))
                    { Tax = 0; }
                    else
                    { Tax = TaxCalculations.CalculateFromDividend(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Coupon:
                    // Coupon amount, with potential adjustments
                    var caa = Definitions.ManualEvents.OfType<CouponAmountAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == Timestamp && x.AssetUniqueId == ParentAsset.UniqueId);
                    if (caa != null)
                    {
                        Amount = caa.Amount;
                    }
                    else
                    {
                        decimal fxRate;
                        if (ParentAsset.CashAccount.Split(':')[2] != Currency)
                        {
                            fxRate = FXRate;
                        }
                        else
                        {
                            fxRate = 1;
                        }
                        Amount = Common.Round(Rate * ParentAsset.GetNominalAmount(time) * fxRate);
                    }
                    // Tax, with potential adjustments
                    var cta = Definitions.ManualEvents.OfType<CouponTaxAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == Timestamp && x.AssetUniqueId == ParentAsset.UniqueId);
                    if (cta != null)
                    { Tax = cta.Tax; }
                    else if (Globals.TaxFreePortfolios.Contains(ParentAsset.Portfolio))
                    { Tax = 0; }
                    else
                    { Tax = TaxCalculations.CalculateFromCoupon(Amount); }
                    Amount -= Tax;
                    break;
                case FlowType.Redemption:
                    Amount = Common.Round(Rate * ParentAsset.GetNominalAmount(time));
                    // Tax, with potential adjustments
                    var rta = Definitions.ManualEvents.OfType<RedemptionTaxAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == Timestamp && x.AssetUniqueId == ParentAsset.UniqueId);
                    if (rta != null)
                    { Tax = rta.Tax; }
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
