using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture
{
    public enum AccountType
    {
        Unspecified,
        Assets,
        ShareCapital, PriorPeriodResult, NonTaxableResult, PrechargedTax,
        OtherComprehensiveIncomeProfit, OtherComprehensiveIncomeLoss,
        TaxLiabilities, TaxReserves,
        OrdinaryIncomeValuation, OrdinaryIncomeInflows,
        RealizedProfit, RealizedLoss,
        Fees, Tax
    }

    public class Account: IFilterable
    {
        public string UniqueId
        {
            get
            {
                var id = AccountType.ToString();
                if (AssetType != null) id += "_" + AssetType.ToString();
                if (Portfolio != null) id += "_" + Portfolio.UniqueId + "_" + Portfolio.Broker;
                id += "_" + Currency;
                return id;
            }
        }

        public string NumericId
        {
            get
            {
                string id = "";
                switch (AccountType)
                {
                    case AccountType.Unspecified:
                        id += "0000";
                        break;
                    case AccountType.Assets:
                        id += "10" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.ShareCapital:
                        id += "2000";
                        break;
                    case AccountType.PriorPeriodResult:
                        id += "2200";
                        break;
                    case AccountType.NonTaxableResult:
                        id += "28" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.PrechargedTax:
                        id += "29" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.OtherComprehensiveIncomeProfit:
                        id += "31" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.OtherComprehensiveIncomeLoss:
                        id += "32" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.TaxLiabilities:
                        id += "4090";
                        break;
                    case AccountType.TaxReserves:
                        id += "4990";
                        break;
                    case AccountType.OrdinaryIncomeValuation:
                        id += "50" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.OrdinaryIncomeInflows:
                        id += "51" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.RealizedProfit:
                        id += "61" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.RealizedLoss:
                        id += "62" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.Fees:
                        id += "80" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.Tax:
                        id += "90" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    default:
                        break;
                }
                id += "0" + (Portfolio?.NumericId ?? "00");
                return id;
            }
        }

        public string AccountCategory
        {
            get
            {
                switch (AccountType)
                {
                    case AccountType.Assets:
                        return "Assets";
                    case AccountType.ShareCapital:
                    case AccountType.PriorPeriodResult:
                    case AccountType.NonTaxableResult:
                    case AccountType.PrechargedTax:
                    case AccountType.OtherComprehensiveIncomeProfit:
                    case AccountType.OtherComprehensiveIncomeLoss:
                        return "Equity";
                    case AccountType.TaxLiabilities:
                    case AccountType.TaxReserves:
                        return "ReservesAndLiabilities";
                    case AccountType.OrdinaryIncomeValuation:
                    case AccountType.OrdinaryIncomeInflows:
                    case AccountType.RealizedProfit:
                    case AccountType.RealizedLoss:
                    case AccountType.Fees:
                    case AccountType.Tax:
                        return "ProfitAndLoss";
                    default:
                        return "Unspecified";
                }
            }
        }

        public bool IsResultAccount { get { return AccountCategory == "ProfitAndLoss"; } }

        public AccountType AccountType { get; private set; }

        public AssetType? AssetType { get; private set; }

        protected PortfolioDefinition? portfolio;
        public PortfolioDefinition? Portfolio
        {
            get
            {
                return portfolio;
            }
            set
            {
                portfolio = value;
                PortfolioId = value?.UniqueId ?? "";
                Broker = value?.Broker ?? "";
            }
        }

        public string PortfolioId { get; protected set; } = "";

        public string Broker { get; protected set; } = "";

        public string Currency { get; private set; }

        private List<AccountEntry> entries = new List<AccountEntry>();

        public Account(AccountType type, AssetType? assetType, PortfolioDefinition? portfolio, string currency)
        {
            AccountType = type;
            AssetType = assetType;
            Portfolio = portfolio;
            Currency = currency;
        }

        private static string GetAssetTypeNumericId(AssetType assetType)
        {
            switch (assetType)
            {
                case Venture.AssetType.Undefined: return "00";
                case Venture.AssetType.Cash: return "10";
                case Venture.AssetType.Equity: return "20";
                case Venture.AssetType.FixedTreasuryBonds: return "30";
                case Venture.AssetType.FloatingTreasuryBonds: return "31";
                case Venture.AssetType.FixedRetailTreasuryBonds: return "33";
                case Venture.AssetType.FloatingRetailTreasuryBonds: return "34";
                case Venture.AssetType.IndexedRetailTreasuryBonds: return "35";
                case Venture.AssetType.FixedCorporateBonds: return "40";
                case Venture.AssetType.FloatingCorporateBonds: return "41";
                case Venture.AssetType.ETF: return "50";
                case Venture.AssetType.MoneyMarketFund: return "70";
                case Venture.AssetType.EquityMixedFund: return "72";
                case Venture.AssetType.TreasuryBondsFund: return "73";
                case Venture.AssetType.CorporateBondsFund: return "74";
                case Venture.AssetType.Futures: return "00";
                default: return "00";
            }
        }

        public decimal GetDebitAmount(DateTime date)
        {
            return entries.Where(x => x.Date <= date && x.Amount > 0 && (!IsResultAccount || x.Date.Year == date.Year)).Sum(x => x.Amount);
        }

        public decimal GetCreditAmount(DateTime date)
        {
            return entries.Where(x => x.Date <= date && x.Amount < 0 && (!IsResultAccount || x.Date.Year == date.Year)).Sum(x => x.Amount);
        }

        public decimal GetNetAmount(DateTime date)
        {
            return entries.Where(x => x.Date <= date).Sum(x => x.Amount);
        }

        public int GetEntriesCount(DateTime date)
        {
            return entries.Count(x => x.Date <= date);
        }

        public IEnumerable<Modules.AccountEntriesViewEntry> GetEntriesAsViewEntries(DateTime date)
        {
            foreach (var e in entries.Where(x => x.Date <= date).OrderBy(x=>x.Date))
            {
                yield return new Modules.AccountEntriesViewEntry(e);
            }
        }

        public void Clear()
        {
            entries.Clear();
        }

        public void Enter(AccountEntry entry)
        {
            entries.Add(entry);
        }

        public override string ToString()
        {
            return "Account: " + UniqueId;
        }
    }
}
