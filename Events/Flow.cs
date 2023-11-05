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

        public DateTime ExDate { get; protected set; }

        public FlowType Type { get; protected set; }

        public decimal Amount { get; protected set; } // coupon rate * nominal amount

        public decimal FXRate { get; protected set; } = 1;

        public Flow(Assets.Asset parentAsset, DateTime timestamp, FlowType type, decimal amount, decimal fxRate) : base(parentAsset)
        {
            Timestamp = timestamp;
            Type = type;
            Amount = amount;
            FXRate = fxRate;
        }

        public override string ToString()
        {
            return $"Event:Flow ({Type}) @{Timestamp:yyyy-MM-dd}: {Amount:0.00} {ParentAsset.Currency}";
        }

    }
}
