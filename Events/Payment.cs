using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Venture.Events
{

    public class Payment: Event
    {
        public PaymentDirection Direction { get; protected set; } = PaymentDirection.Unspecified;

        public Payment(Assets.Asset parentAsset, Data.Transaction tr, decimal amount, PaymentDirection direction) : base(parentAsset)
        {
            UniqueId = $"Payment_{direction}_{tr.TransactionType}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            TransactionIndex = tr.Index;
            Timestamp = tr.Timestamp;
            Amount = amount;
            FXRate = tr.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Events.Flow fl, PaymentDirection direction) : base(parentAsset)
        {
            UniqueId = $"Payment_{direction}_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Timestamp = fl.Timestamp;
            Amount = fl.Amount;
            FXRate = fl.FXRate;
        }
    }
}
