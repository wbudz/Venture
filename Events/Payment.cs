using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Venture.Events
{

    public class Payment : Event
    {
        public PaymentDirection Direction { get; protected set; } = PaymentDirection.Unspecified;

        public string Description { get; protected set; } = "";

        public Payment(Assets.Asset parentAsset, Data.Transaction tr, decimal amount, PaymentDirection direction) : base(parentAsset, tr.Timestamp)
        {
            UniqueId = $"Payment_{direction}_{tr.TransactionType}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            TransactionIndex = tr.Index;
            Amount = amount;
            FXRate = tr.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Data.Transaction tr, IEnumerable<Events.Derecognition> dr, PaymentDirection direction) : base(parentAsset, tr.Timestamp)
        {
            UniqueId = $"Payment_{direction}_Sale_{dr.First().ParentAsset.UniqueId}_{tr.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            TransactionIndex = tr.Index;
            Amount = dr.Sum(x => x.Amount) - dr.Sum(x => x.Tax) - dr.Sum(x => x.Fee);
            FXRate = tr.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Events.Flow fl, PaymentDirection direction) : base(parentAsset, fl.Timestamp)
        {
            UniqueId = $"Payment_{direction}_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Amount = fl.Amount;
            FXRate = fl.FXRate;
        }
    }
}