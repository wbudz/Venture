using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public enum PaymentDirection { Unspecified, Neutral, Inflow, Outflow }

    public class PaymentEvent : Event
    {
        public PaymentDirection Direction { get; protected set; } = PaymentDirection.Unspecified;

        public Event? AssociatedEvent { get; set; }

        public string Description { get; protected set; } = "";

        public PaymentType PaymentType { get; protected set; } = PaymentType.Undefined;

        public PaymentEvent(Cash parentAsset, PayTransactionDefinition ptd, decimal amount, PaymentDirection direction) : base(parentAsset, ptd.Timestamp)
        {
            UniqueId = $"Payment_{direction}_{ptd.Index}_{ptd.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = null;
            Direction = direction;
            TransactionIndex = ptd.Index;
            Amount = amount;
            FXRate = ptd.FXRate;
            PaymentType = ptd.PaymentType;
        }

        public PaymentEvent(Cash parentAsset, BuyTransactionDefinition btd, decimal amount, Event e) : base(parentAsset, btd.Timestamp)
        {
            UniqueId = $"Payment_Purchase_{btd.AssetType}_{btd.AssetId}_{btd.Index}_{btd.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = e;
            Direction = PaymentDirection.Outflow;
            TransactionIndex = btd.Index;
            Amount = amount;
            FXRate = btd.FXRate;
        }

        public PaymentEvent(Cash parentAsset, SellTransactionDefinition std, decimal amount, Event e) : base(parentAsset, std.Timestamp)
        {
            // Used in rare case when sale means net outgoing payment (e.g. when selling futures).
            UniqueId = $"Payment_Sale_{std.AssetType}_{std.AssetId}_{std.Index}_{std.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = e;
            Direction = PaymentDirection.Outflow;
            TransactionIndex = std.Index;
            Amount = amount;
            FXRate = std.FXRate;
        }

        public PaymentEvent(Cash parentAsset, SellTransactionDefinition std, IEnumerable<DerecognitionEvent> dr) : base(parentAsset, std.Timestamp)
        {
            UniqueId = $"Payment_Sale_{std.AssetType}_{std.AssetId}_{std.Index}_{std.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = dr.First();
            Direction = PaymentDirection.Inflow;
            TransactionIndex = std.Index;
            Amount = dr.Sum(x => x.Amount) - dr.Sum(x => x.Tax) - dr.Sum(x => x.Fee);
            if (Amount < 0) throw new Exception($"Cash gained from sale {std} is less than tax and fees.");
            FXRate = std.FXRate;
        }

        public PaymentEvent(Cash parentAsset, FlowEvent fl, decimal amount) : base(parentAsset, fl.Timestamp)
        {
            UniqueId = $"Payment_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = fl;
            Direction = PaymentDirection.Inflow;
            Amount = amount;
            FXRate = fl.FXRate;
        }

        public PaymentEvent(Cash parentAsset, FuturesTransactionEvent fr, decimal amount, PaymentDirection direction) : base(parentAsset, fr.Timestamp)
        {
            UniqueId = $"Payment_FuturesRecognition_{fr.ParentAsset.InstrumentUniqueId}_{fr.TransactionIndex}_{fr.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = fr;
            Direction = direction;
            TransactionIndex = fr.TransactionIndex;
            Amount = amount;
            FXRate = fr.FXRate;
        }

        public PaymentEvent(Cash parentAsset, FuturesRevaluationEvent fs, decimal amount, PaymentDirection direction) : base(parentAsset, fs.Timestamp)
        {
            UniqueId = $"Payment_FuturesSettlement_{fs.ParentAsset.UniqueId}_{fs.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = fs;
            Direction = direction;
            Amount = amount;
            FXRate = fs.FXRate;
        }

        public PaymentEvent(Cash parentAsset, AdditionalPremiumEventDefinition mn) : base(parentAsset, mn.Timestamp)
        {
            UniqueId = $"Payment_AdditionalPremium_{mn.Portfolio}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = null;
            Direction = PaymentDirection.Inflow;
            Amount = mn.Amount;
            FXRate = mn.FXRate;

        }

        public PaymentEvent(Cash parentAsset, AdditionalChargeEventDefinition mn, decimal amount) : base(parentAsset, mn.Timestamp)
        {
            UniqueId = $"Payment_AdditionalCharge_{mn.Portfolio}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = null;
            Direction = PaymentDirection.Outflow;
            Amount = amount;
            FXRate = mn.FXRate;
        }

        public PaymentEvent(Cash parentAsset, EquityRedemptionEventDefinition mn, decimal amount) : base(parentAsset, mn.Timestamp)
        {
            UniqueId = $"Payment_EquityRedemption_{mn.InstrumentUniqueId}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssociatedEvent = null; // should point to equity redemption event (derecognition)
            Direction = PaymentDirection.Inflow;
            Amount = amount;
            FXRate = FX.GetRate(mn.Timestamp, parentAsset.Currency);
        }
    }
}