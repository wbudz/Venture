using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Financial.Calendar;

namespace Venture.Data
{
    /// <summary>
    /// Specifies type of cash payment:
    ///     ShareCapital - payment increases or decreases share capital;
    ///     OtherCapital - payment increases or decreases other capital;
    ///     Tax - payment covers tax payables
    /// </summary>
    public enum PaymentType { Undefined, ShareCapital, OtherCapital, Tax }

    /// <summary>
    /// Specifies type of a transaction:
    ///     Buy - purchase of an asset;
    ///     Sell - sale of an asset;
    ///     Cash - payment of cash;
    ///     Transfer - transfer of assets between accounts or portfolios without actual sale.
    /// </summary>
    public enum TransactionType { Undefined, Buy, Sell, Cash, Transfer }

    public class Transaction : DataPoint
    {
        public string UniqueId { get { return $"{Index}_{TransactionType}_{AssetType}_{AssetId}"; } }

        public int Index { get; private set; } = 0;

        public TransactionType TransactionType { get; private set; } = TransactionType.Undefined;

        public AssetType AssetType { get; private set; } = AssetType.Undefined;

        public string AssetId { get; private set; } = "";

        public string InstrumentUniqueId
        {
            get
            {
                return AssetType + "_" + AssetId;
            }
        }

        public DateTime TradeDate { get; private set; } = DateTime.MinValue;

        public DateTime SettlementDate { get; private set; } = DateTime.MinValue;

        public DateTime Timestamp
        {
            get
            {
                bool recognitionOnTradeDate = AssetType == AssetType.Equity ||
                    AssetType == AssetType.ETF;
                return recognitionOnTradeDate ? TradeDate : SettlementDate;
            }
        }

        public decimal Count { get; private set; } = 0.0M;

        public string Currency { get; private set; } = "PLN";

        public decimal NominalAmount { get; private set; } = 0.0M;

        public decimal Price { get; private set; } = 0.0M;

        public decimal Fee { get; private set; } = 0.0M;

        public string AccountSrc { get; private set; } = "";

        public string AccountDst { get; private set; } = "";

        public string PortfolioSrc { get; private set; } = "";

        public string PortfolioDst { get; private set; } = "";

        public ValuationClass ValuationClass { get; private set; } = ValuationClass.Undefined;

        public decimal FXRate { get; private set; } = 1;

        public PaymentType PaymentType { get; private set; } = PaymentType.Undefined;

        public bool Original { get; private set; } = true;

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
                        return Common.Round(Price / 100 * NominalAmount * Count);
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

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            Index = index + 1;
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "index" && !String.IsNullOrEmpty(line[i])) Index = ConvertToInt(line[i]);
                if (headers[i] == "transactiontype") TransactionType = ConvertToEnum<TransactionType>(line[i]);
                if (headers[i] == "assettype") AssetType = ConvertToEnum<AssetType>(line[i]);
                if (headers[i] == "assetid") AssetId = line[i];
                if (headers[i] == "tradedate") TradeDate = ConvertToDateTime(line[i]);
                if (headers[i] == "settlementdate") SettlementDate = ConvertToDateTime(line[i]);
                if (headers[i] == "count") Count = ConvertToDecimal(line[i]);
                if (headers[i] == "currency") Currency = line[i];
                if (headers[i] == "nominalamount") NominalAmount = ConvertToDecimal(line[i]);
                if (headers[i] == "price") Price = ConvertToDecimal(line[i]);
                if (headers[i] == "fee") Fee = ConvertToDecimal(line[i]);
                if (headers[i] == "accountsrc") AccountSrc = line[i];
                if (headers[i] == "accountdst") AccountDst = line[i];
                if (headers[i] == "portfoliosrc") PortfolioSrc = line[i];
                if (headers[i] == "portfoliodst") PortfolioDst = line[i];
                if (headers[i] == "valuationclass")
                {
                    if (line[i].ToLower() == "afs" || line[i].Replace(" ", "").ToLower() == "availableforsale") ValuationClass = ValuationClass.AvailableForSale;
                    if (line[i].ToLower() == "trd" || line[i].Replace(" ", "").ToLower() == "trading") ValuationClass = ValuationClass.Trading;
                    if (line[i].ToLower() == "htm" || line[i].Replace(" ", "").ToLower() == "heldtomaturity") ValuationClass = ValuationClass.HeldToMaturity;
                };
                if (headers[i] == "fxrate")
                {
                    if (line[i] == "") FXRate = 1;
                    else FXRate = ConvertToDecimal(line[i]);
                }
                if (headers[i] == "paymenttype") PaymentType = ConvertToEnum<PaymentType>(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Transaction: {UniqueId}";
        }

        public static Transaction CreateModifiedTransaction(Transaction tr, decimal count, decimal amount, decimal price, decimal fee)
        {
            Transaction output = new Transaction()
            {
                Index = tr.Index,
                TransactionType = tr.TransactionType,
                AssetType = tr.AssetType,
                AssetId = tr.AssetId,
                TradeDate = tr.TradeDate,
                SettlementDate = tr.SettlementDate,
                Count = count,
                Currency = tr.Currency,
                NominalAmount = amount,
                Price = price,
                Fee = fee,
                AccountSrc = tr.AccountSrc,
                AccountDst = tr.AccountDst,
                PortfolioSrc = tr.PortfolioSrc,
                PortfolioDst = tr.PortfolioDst,
                ValuationClass = tr.ValuationClass,
                FXRate = tr.FXRate,
                PaymentType = tr.PaymentType,
                Original = false
            };
            return output;
        }
    }
}
