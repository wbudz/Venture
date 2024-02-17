using Venture.Events;
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

        public CashflowViewEntry(Events.Payment p)
        {
            UniqueId = p.UniqueId;
            ParentAssetUniqueId = p.ParentAsset.UniqueId;
            Portfolio = p.ParentAsset.Portfolio;
            CashAccount = p.ParentAsset.CashAccount;
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

            if (TransactionIndex < 0) throw new Exception("Transaction index missing.");
            Data.Transaction tr = Data.Definitions.Transactions.Single(x => x.Index == TransactionIndex);
            if (tr.TransactionType == Data.TransactionType.Buy) CashflowType = "Purchase";
            if (tr.TransactionType == Data.TransactionType.Sell) CashflowType = "Sale";
            if (tr.TransactionType == Data.TransactionType.Cash) CashflowType = "Cash transfer";

        }

        public CashflowViewEntry(Events.Flow f)
        {
            UniqueId = f.UniqueId;
            ParentAssetUniqueId = f.ParentAsset.UniqueId;
            Portfolio = f.ParentAsset.Portfolio;
            CashAccount = f.ParentAsset.CashAccount;
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

        public CashflowViewEntry(Events.Recognition r)
        {
            UniqueId = r.UniqueId;
            ParentAssetUniqueId = r.ParentAsset.UniqueId;
            Portfolio = r.ParentAsset.Portfolio;
            CashAccount = r.ParentAsset.CashAccount;
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
