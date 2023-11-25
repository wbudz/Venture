using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Budziszewski.Venture.Events
{

    public class Payment: Event
    {
        public PaymentDirection Direction { get; protected set; } = PaymentDirection.Unspecified;

        public Payment(Assets.Asset parentAsset, Data.Transaction tr, decimal amount, PaymentDirection direction) : base(parentAsset)
        {
            Direction = direction;
            TransactionIndex = tr.Index;
            Timestamp = tr.SettlementDate;
            Amount = amount;
            FXRate = tr.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Events.Flow fl, PaymentDirection direction) : base(parentAsset)
        {
            Direction = direction;
            Timestamp = fl.Timestamp;
            Amount = fl.Amount;
            FXRate = fl.FXRate;
        }

        public override string ToString()
        {
            return $"Event:Payment @{Timestamp:yyyy-MM-dd}, amount: {Amount:0.00}";
        }
    }
}
