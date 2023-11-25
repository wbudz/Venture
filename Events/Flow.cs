using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
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
            switch (FlowType)
            {
                case FlowType.Undefined: throw new Exception("Cannot recalculate amount for undefined flow event.");
                case FlowType.Dividend: Amount = Rate * ParentAsset.GetCount(time); Tax = TaxCalculations.CalculateFromDividend(Amount); Amount -= Tax; break;
                case FlowType.Coupon: Amount = Rate * ParentAsset.GetNominalAmount(time); Tax = TaxCalculations.CalculateFromCoupon(Amount); Amount -= Tax; break;
                case FlowType.Redemption: Amount = ParentAsset.GetNominalAmount(time); Tax = 0; break;
                default: throw new Exception("Cannot recalculate amount for undefined flow event.");
            }
        }

        public override string ToString()
        {
            return $"Event:Flow ({FlowType}) @{Timestamp:yyyy-MM-dd}: {Amount:0.00} {ParentAsset.Currency}";
        }

    }
}
