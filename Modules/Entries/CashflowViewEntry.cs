using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public enum CashflowType
    {
        Inflow_CapitalIncrease, Inflow_TaxPayment, Inflow_OtherPayment,
        Inflow_SaleEquities, Inflow_SaleETF, Inflow_SaleBonds, Inflow_SaleFunds,
        Inflow_Dividend, Inflow_Redemption, Inflow_Coupon,
        Inflow_Futures,
        Outflow_CapitalDecrease, Outflow_TaxPayment, Outflow_OtherPayment,
        Outflow_PurchaseEquities, Outflow_PurchaseETF, Outflow_PurchaseBonds, Outflow_PurchaseFunds,
        Outflow_Futures,
        Unknown
    }

    public class CashflowViewEntry : ModuleEntry
    {
        public string CashAccount { get; set; } = "";

        public string ParentAssetUniqueId { get; set; } = "";

        public string AssociatedEvent { get; set; } = "";

        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        public DateTime RecordDate { get; set; } = DateTime.MinValue;

        public PaymentDirection Direction { get; set; } = PaymentDirection.Unspecified;

        public CashflowType CashflowType { get; set; } = CashflowType.Unknown;

        public string CashflowTypeDescription
        {
            get
            {
                switch (CashflowType)
                {
                    case CashflowType.Inflow_CapitalIncrease: return "Inflow: capital increase";
                    case CashflowType.Inflow_TaxPayment: return "Inflow: tax payment";
                    case CashflowType.Inflow_OtherPayment: return "Inflow: other payment";
                    case CashflowType.Inflow_SaleEquities: return "Inflow: sale of equities";
                    case CashflowType.Inflow_SaleETF: return "Inflow: sale of ETF";
                    case CashflowType.Inflow_SaleBonds: return "Inflow: sale of bonds";
                    case CashflowType.Inflow_SaleFunds: return "Inflow: sale of funds";
                    case CashflowType.Inflow_Dividend: return "Inflow: dividend";
                    case CashflowType.Inflow_Redemption: return "Inflow: redemption";
                    case CashflowType.Inflow_Coupon: return "Inflow: coupon";
                    case CashflowType.Inflow_Futures: return "Inflow: futures";
                    case CashflowType.Outflow_CapitalDecrease: return "Outflow: capital decrease";
                    case CashflowType.Outflow_TaxPayment: return "Outflow: tax payment";
                    case CashflowType.Outflow_OtherPayment: return "Outflow: other payment";
                    case CashflowType.Outflow_PurchaseEquities: return "Outflow: purchase of equities";
                    case CashflowType.Outflow_PurchaseETF: return "Outflow: purchase of ETF";
                    case CashflowType.Outflow_PurchaseBonds: return "Outflow: purchase of bonds";
                    case CashflowType.Outflow_PurchaseFunds: return "Outflow: purchase of funds";
                    case CashflowType.Outflow_Futures: return "Outflow: futures";
                    case CashflowType.Unknown: return "Unknown";
                    default: return "Unspecified";
                }
            }
        }

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

                if (tr is BuyTransactionDefinition)
                {
                    switch (tr.AssetType)
                    {
                        case AssetType.Undefined: break;
                        case AssetType.Cash: break;
                        case AssetType.Equity:
                            CashflowType = CashflowType.Outflow_PurchaseEquities; break;
                        case AssetType.FixedTreasuryBonds:
                        case AssetType.FloatingTreasuryBonds:
                        case AssetType.FixedRetailTreasuryBonds:
                        case AssetType.FloatingRetailTreasuryBonds:
                        case AssetType.IndexedRetailTreasuryBonds:
                        case AssetType.FixedCorporateBonds:
                        case AssetType.FloatingCorporateBonds:
                            CashflowType = CashflowType.Outflow_PurchaseBonds; break;
                        case AssetType.ETF:
                            CashflowType = CashflowType.Outflow_PurchaseETF; break;
                        case AssetType.MoneyMarketFund:
                        case AssetType.EquityMixedFund:
                        case AssetType.TreasuryBondsFund:
                        case AssetType.CorporateBondsFund:
                            CashflowType = CashflowType.Outflow_PurchaseFunds; break;
                        case AssetType.Futures:
                            CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_Futures : CashflowType.Inflow_Futures; break;
                        default: break;
                    }
                }
                else if (tr is SellTransactionDefinition)
                {
                    switch (tr.AssetType)
                    {
                        case AssetType.Undefined: break;
                        case AssetType.Cash: break;
                        case AssetType.Equity:
                            CashflowType = CashflowType.Inflow_SaleEquities; break;
                        case AssetType.FixedTreasuryBonds:
                        case AssetType.FloatingTreasuryBonds:
                        case AssetType.FixedRetailTreasuryBonds:
                        case AssetType.FloatingRetailTreasuryBonds:
                        case AssetType.IndexedRetailTreasuryBonds:
                        case AssetType.FixedCorporateBonds:
                        case AssetType.FloatingCorporateBonds:
                            CashflowType = CashflowType.Inflow_SaleBonds; break;
                        case AssetType.ETF:
                            CashflowType = CashflowType.Inflow_SaleETF; break;
                        case AssetType.MoneyMarketFund:
                        case AssetType.EquityMixedFund:
                        case AssetType.TreasuryBondsFund:
                        case AssetType.CorporateBondsFund:
                            CashflowType = CashflowType.Inflow_SaleFunds; break;
                        case AssetType.Futures:
                            CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_Futures : CashflowType.Inflow_Futures; break;
                        default: break;
                    }
                }
                else if (tr is PayTransactionDefinition)
                {
                    switch (tr.PaymentType)
                    {
                        case PaymentType.Undefined: break;
                        case PaymentType.ShareCapital:
                        case PaymentType.OtherCapital:
                            CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_CapitalDecrease : CashflowType.Inflow_CapitalIncrease;
                            break;
                        case PaymentType.Tax:
                            CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_TaxPayment : CashflowType.Inflow_TaxPayment;
                            break;
                        case PaymentType.Receivables:
                            CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_OtherPayment : CashflowType.Inflow_OtherPayment; // TODO: receivables?
                            break;
                        default: break;
                    }
                }
                else if (tr is TransferTransactionDefinition) throw new Exception("Detected cash flow associated with transfer transaction.");
            }
            else if (p.AssociatedEvent != null)
            {
                if (p.AssociatedEvent is FlowEvent fe2)
                {
                    switch (fe2.FlowType)
                    {
                        case FlowType.Undefined: break;
                        case FlowType.Dividend: CashflowType = CashflowType.Inflow_Dividend; break;
                        case FlowType.Coupon: CashflowType = CashflowType.Inflow_Coupon; break;
                        case FlowType.Redemption: CashflowType = CashflowType.Inflow_Redemption; break;
                        default: break;
                    }
                }
                else if (p.AssociatedEvent is FuturesRevaluationEvent fe3)
                {
                    CashflowType = p.Direction == PaymentDirection.Outflow ? CashflowType.Outflow_Futures : CashflowType.Inflow_Futures;
                }
            }

        }
    }
}
