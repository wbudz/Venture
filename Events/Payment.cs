using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;
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

        public Payment(Assets.Asset parentAsset, Events.Flow fl, decimal amount, PaymentDirection direction) : base(parentAsset, fl.Timestamp)
        {
            UniqueId = $"Payment_{direction}_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Amount = amount;
            FXRate = fl.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Events.Recognition r, decimal amount, PaymentDirection direction) : base(parentAsset, r.Timestamp)
        {
            UniqueId = $"Payment_Futures_{r.ParentAsset.UniqueId}_{r.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Amount = amount;
            FXRate = r.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Manual mn, PaymentDirection direction) : base(parentAsset, mn.Timestamp)
        {
            if (mn.AdjustmentType != ManualAdjustmentType.AccountBalanceInterest) 
                throw new Exception($"Unexpected manual adjustment type used for creating cash: {mn.AdjustmentType}.");

            UniqueId = $"Payment_{mn.AdjustmentType}_{mn.Text1}_{mn.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Amount = mn.Amount1;
            FXRate = mn.Amount2 == 0 ? 1 : mn.Amount2;
        }

        public Payment(Assets.Asset parentAsset, Manual mn, decimal amount, PaymentDirection direction) : base(parentAsset, mn.Timestamp)
        {
            if (mn.AdjustmentType != ManualAdjustmentType.EquityRedemption)
                throw new Exception($"Unexpected manual adjustment type used for creating cash: {mn.AdjustmentType}.");

            UniqueId = $"Payment_{mn.AdjustmentType}_{mn.Text1}_{mn.Timestamp.ToString("yyyyMMdd")}";
            Direction = direction;
            Amount = amount;
            FXRate = 1; //TODO: Implement looking for FX rate.
        }
    }
}