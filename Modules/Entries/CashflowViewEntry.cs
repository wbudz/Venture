using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class CashflowViewEntry : ModuleEntry
    {
        public string CashAccount { get; set; } = "";

        public string ParentAssetUniqueId { get; set; } = "";

        public string AssociatedEvent { get; set; } = "";

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
            PortfolioId = p.ParentAsset.PortfolioId;
            CashAccount = p.ParentAsset.CashAccount;
            Broker = p.ParentAsset.Broker;
            Currency = p.ParentAsset.Currency;
            Timestamp = p.Timestamp;
            RecordDate = p.Timestamp;
            Direction = p.Direction;
            Rate = 0;

            if (p.AssociatedEvent is FlowEvent fe)
                Tax = fe.Tax;
            else if (p.AssociatedEvent is DerecognitionEvent de)
                Tax = de.Tax;
            else
                Tax = 0;

            TransactionIndex = p.TransactionIndex;
            Amount = p.Amount * (p.Direction == PaymentDirection.Outflow ? -1 : 1);
            GrossAmount = Amount + Tax;
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
                if (p.AssociatedEvent is FlowEvent) CashflowType = "Flow";
                else CashflowType = p.AssociatedEvent.GetType().ToString();
            }

        }
    }
}
