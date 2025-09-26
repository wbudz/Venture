using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using static Financial.Calendar;

namespace Venture
{
    /// <summary>
    /// Specifies type of cash payment:
    ///     ShareCapital - payment increases or decreases share capital;
    ///     OtherCapital - payment increases or decreases other capital;
    ///     Tax - payment covers tax payables
    ///     Receivables - no actual flow; only receivables are recognized
    /// </summary>
    public enum PaymentType { Undefined, ShareCapital, OtherCapital, Tax, Receivables }

    public abstract class TransactionDefinition : Definition
    {
        public abstract string UniqueId { get; }

        public string TransactionType { get { return GetType().Name.Replace("TransactionDefinition", ""); } }

        public int Index { get; private set; } = 0;

        public AssetType AssetType { get; protected set; } = AssetType.Undefined;

        public string AssetId { get; protected set; } = "";

        public string InstrumentUniqueId
        {
            get
            {
                return AssetType + "_" + AssetId;
            }
        }

        public DateTime TradeDate { get; protected set; } = DateTime.MinValue;

        public DateTime SettlementDate { get; protected set; } = DateTime.MinValue;

        public DateTime Timestamp
        {
            get
            {
                bool recognitionOnTradeDate = AssetType == AssetType.Equity ||
                    AssetType == AssetType.ETF;
                return recognitionOnTradeDate ? TradeDate : SettlementDate;
            }
        }

        public decimal Count { get; protected set; } = 0.0M;

        public string Currency { get; protected set; } = "PLN";

        public decimal Price { get; protected set; } = 0.0M;

        public decimal Fee { get; protected set; } = 0.0M;

        public decimal NominalAmount { get; protected set; } = 0.0M;

        public string PortfolioSrc { get; protected set; } = "";

        public string PortfolioDst { get; protected set; } = "";

        public abstract string AccountSrc { get; }

        public abstract string AccountDst { get; }

        public ValuationClass ValuationClass { get; protected set; } = ValuationClass.Undefined;

        public decimal FXRate { get; protected set; } = 1;

        public PaymentType PaymentType { get; protected set; } = PaymentType.Undefined;

        public bool Original { get; protected set; } = true;

        public decimal Amount
        {
            get
            {
                switch (AssetType)
                {
                    case AssetType.Undefined: throw new Exception("Cannot give amount for undefined instrument type.");
                    case AssetType.Cash: return Common.Round(NominalAmount);
                    case AssetType.Equity: return Common.Round(Price * Count);
                    case AssetType.FixedTreasuryBonds:
                    case AssetType.FloatingTreasuryBonds:
                    case AssetType.FixedRetailTreasuryBonds:
                    case AssetType.FloatingRetailTreasuryBonds:
                    case AssetType.IndexedRetailTreasuryBonds:
                    case AssetType.FixedCorporateBonds:
                    case AssetType.FloatingCorporateBonds:
                        return Common.Round(Price / 100 * Count * Definitions.Instruments.Single(x => x.UniqueId == InstrumentUniqueId).UnitPrice);
                    case AssetType.ETF:
                        return Common.Round(Price * Count);
                    case AssetType.MoneyMarketFund:
                    case AssetType.EquityMixedFund:
                    case AssetType.TreasuryBondsFund:
                    case AssetType.CorporateBondsFund:
                        return Common.Round(Price * Count);
                    case AssetType.Futures:
                        return 0;
                    default:
                        throw new Exception("Cannot give amount for unknown instrument type.");
                }
            }
        }

        public TransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            Index = ConvertToInt(data["index"]);
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);

            TradeDate = ConvertToDateTime(data["tradedate"]);
            SettlementDate = ConvertToDateTime(data["settlementdate"]);

            Currency = data["currency"];

            FXRate = GetFXRateFromData(data["fxrate"]);
        }

        public override string ToString()
        {
            return $"Transaction: {UniqueId}";
        }

        public static T CreateModifiedTransaction<T>(T tr, decimal count, decimal price, decimal fee) where T : TransactionDefinition
        {
            var mtr = (T)tr.MemberwiseClone();
            mtr.Count = count;
            mtr.Price = price;
            mtr.Fee = fee;
            mtr.Original = false;
            return mtr;
        }
    }

    public class BuyTransactionDefinition : TransactionDefinition
    {
        public override string UniqueId { get { return $"{Index}_Buy_{AssetType}_{AssetId}"; } }

        public override string AccountSrc { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioDst)?.CashAccount ?? ""; } }

        public override string AccountDst { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioDst)?.CustodyAccount ?? ""; } }

        public BuyTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];

            Count = ConvertToDecimal(data["count"]);

            Price = ConvertToDecimal(data["price"]);
            Fee = ConvertToDecimal(data["fee"]);

            PortfolioDst = data["portfoliodst"];

            ValuationClass = GetValuationClassFromData(data["valuationclass"]);
        }
    }

    public class SellTransactionDefinition : TransactionDefinition
    {
        public override string UniqueId { get { return $"{Index}_Sell_{AssetType}_{AssetId}"; } }

        public override string AccountSrc { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioSrc)?.CustodyAccount ?? ""; } }

        public override string AccountDst { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioSrc)?.CashAccount ?? ""; } }

        public SellTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];

            Count = ConvertToDecimal(data["count"]);

            Price = ConvertToDecimal(data["price"]);
            Fee = ConvertToDecimal(data["fee"]);

            PortfolioSrc = data["portfoliosrc"];
            if (AssetType == AssetType.Futures) PortfolioDst = data["portfoliodst"];

            ValuationClass = GetValuationClassFromData(data["valuationclass"]);
        }
    }

    public class PayTransactionDefinition : TransactionDefinition
    {
        public override string UniqueId { get { return $"{Index}_Pay"; } }

        public override string AccountSrc { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioSrc)?.CashAccount ?? ""; } }

        public override string AccountDst { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioDst)?.CashAccount ?? ""; } }

        public PayTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);

            NominalAmount = ConvertToDecimal(data["nominalamount"]);

            PortfolioSrc = data["portfoliosrc"];
            PortfolioDst = data["portfoliodst"];

            PaymentType = ConvertToEnum<PaymentType>(data["paymenttype"]);
        }
    }

    public abstract class TransferTransactionDefinition : TransactionDefinition
    {
        public override string AccountSrc { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioSrc)?.CustodyAccount ?? ""; } }

        public override string AccountDst { get { return Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == PortfolioDst)?.CustodyAccount ?? ""; } }

        public TransferTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];

            Count = ConvertToDecimal(data["count"]);

            Price = ConvertToDecimal(data["price"]);
            Fee = ConvertToDecimal(data["fee"]);

            PortfolioSrc = data["portfoliosrc"];
            PortfolioDst = data["portfoliodst"];

            ValuationClass = GetValuationClassFromData(data["valuationclass"]);
        }
    }

    public class PortfolioTransferTransactionDefinition : TransferTransactionDefinition
    {
        public override string UniqueId { get { return $"{Index}_PortfolioTransfer_{AssetType}_{AssetId}"; } }

        public PortfolioTransferTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class AssetSwitchTransactionDefinition : TransferTransactionDefinition
    {
        public override string UniqueId { get { return $"{Index}_Switch_{AssetType}_{AssetId}"; } }

        public string InstrumentUniqueIdTarget
        {
            get
            {
                return AssetTypeTarget + "_" + AssetIdTarget;
            }
        }

        public AssetType AssetTypeTarget { get; private set; }

        public string AssetIdTarget { get; private set; }

        public decimal CountTarget { get; private set; }

        public decimal PriceTarget { get; private set; }

        public AssetSwitchTransactionDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetTypeTarget = ConvertToEnum<AssetType>(data["assettype2"]);
            AssetIdTarget = data["assetid2"];
            CountTarget = ConvertToDecimal(data["count2"]);
            PriceTarget = ConvertToDecimal(data["price2"]);

            if (Math.Abs(Common.Round(Count * Price) - Common.Round(CountTarget * PriceTarget)) > 0.01M)
            {
                throw new Exception("Switch transaction definition has different source and target amounts.");
            }
        }
    }
}
