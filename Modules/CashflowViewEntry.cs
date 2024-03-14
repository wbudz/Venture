using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class CashflowViewEntry
    {
        public string UniqueId { get; set; } = "";

        public string ParentAssetUniqueId { get; set; } = "";

        public string AssociatedEvent { get; set; } = "";

        public string Portfolio { get; set; } = "";

        public string CashAccount { get; set; } = "";

        public string Broker { get; set; } = "";

        public string Currency { get; set; } = "PLN";

        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        public DateTime RecordDate { get; set; } = DateTime.MinValue;

        public PaymentDirection Direction { get; set; } = PaymentDirection.Unspecified;

        public string CashflowType { get; set; } = "Unknown";

        public decimal Rate { get; set; }

        public decimal Tax { get; set; }

        public int TransactionIndex { get; set; } = -1;

        public decimal GrossAmount { get; set; }

        public decimal Amount { get; set; }

        public decimal FXRate { get; set; } = 1;

        public CashflowViewEntry(PaymentEvent p)
        {
            UniqueId = p.UniqueId;
            ParentAssetUniqueId = p.ParentAsset.UniqueId;
            AssociatedEvent = p.AssociatedEvent?.UniqueId ?? "";
            Portfolio = p.ParentAsset.Portfolio;
            CashAccount = p.ParentAsset.CashAccount;
            Broker = CashAccount.Split(':')[1];
            Currency = p.ParentAsset.Currency;
            Timestamp = p.Timestamp;
            RecordDate = p.Timestamp;
            Direction = p.Direction;
            Rate = 0;
            Tax = 0;
            TransactionIndex = p.TransactionIndex;
            Amount = p.Amount * (p.Direction == PaymentDirection.Outflow ? -1 : 1);
            GrossAmount = Amount;
            FXRate = p.FXRate;

            if (p.TransactionIndex > -1)
            {
                TransactionDefinition tr = Definitions.Transactions.Single(x => x.Index == TransactionIndex);
                if (tr is BuyTransactionDefinition) CashflowType = "Purchase";
                if (tr is SellTransactionDefinition) CashflowType = "Sale";
                if (tr is PayTransactionDefinition) CashflowType = "Cash payment";
                if (tr is TransferTransactionDefinition) CashflowType = "Asset transfer";
            }
            else if (p.AssociatedEvent != null)
            {
                CashflowType = p.AssociatedEvent.GetType().ToString();
            }

        }

        public CashflowViewEntry(FlowEvent f)
        {
            UniqueId = f.UniqueId;
            ParentAssetUniqueId = f.ParentAsset.UniqueId;
            Portfolio = f.ParentAsset.Portfolio;
            CashAccount = f.ParentAsset.CashAccount;
            Broker = CashAccount.Split(':')[1];
            Currency = f.ParentAsset.Currency;
            Timestamp = f.Timestamp;
            RecordDate = f.RecordDate;
            Direction = PaymentDirection.Inflow;
            Rate = f.Rate;
            Tax = f.Tax;
            TransactionIndex = f.TransactionIndex;
            Amount = f.Amount;
            GrossAmount = f.GrossAmount;
            FXRate = f.FXRate;

            CashflowType = f.FlowType.ToString();
        }

        public CashflowViewEntry(RecognitionEvent r)
        {
            UniqueId = r.UniqueId;
            ParentAssetUniqueId = r.ParentAsset.UniqueId;
            Portfolio = r.ParentAsset.Portfolio;
            CashAccount = r.ParentAsset.CashAccount;
            Broker = CashAccount.Split(':')[1];
            Currency = r.ParentAsset.Currency;
            Timestamp = r.Timestamp;
            RecordDate = r.Timestamp;
            Direction = r.Amount > 0 ? PaymentDirection.Inflow : PaymentDirection.Outflow;
            Rate = 0;
            Tax = 0;
            TransactionIndex = r.TransactionIndex;
            Amount = r.Amount;
            GrossAmount = r.GrossAmount;
            FXRate = r.FXRate;

            CashflowType = "Futures settlement";
        }
    }
}
