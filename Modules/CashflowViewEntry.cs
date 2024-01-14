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

        public FlowType FlowType { get; set; } = FlowType.Undefined;

        public decimal Rate { get; set; }

        public decimal Tax { get; set; }

        public int TransactionIndex { get; set; } = -1;

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
            RecordDate = Financial.Calendar.WorkingDays(p.Timestamp, -2);
            Direction = p.Direction;
            FlowType = FlowType.Undefined;
            Rate = 0;
            Tax = 0;
            TransactionIndex = p.TransactionIndex;
            Amount = p.Amount * (p.Direction == PaymentDirection.Outflow ? -1 : 1);
            FXRate = p.FXRate;
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
            FlowType = f.FlowType;
            Rate = f.Rate;
            Tax = f.Tax;
            TransactionIndex = f.TransactionIndex;
            Amount = f.Amount;
            FXRate = f.FXRate;
        }
    }
}
