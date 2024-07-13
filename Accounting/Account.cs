﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public enum AccountType
    {
        Unspecified,
        Assets,
        ShareCapital, OtherComprehensiveIncome,
        RealizedResult, Fees
    }

    public class Account
    {
        public string UniqueId
        {
            get
            {
                var id = AccountType.ToString();
                if (AssetType != null) id += "_" + AssetType.ToString();
                id += "_" + Portfolio.UniqueId + "_" + Currency;
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
                    case AccountType.OtherComprehensiveIncome:
                        id += "30" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.RealizedResult:
                        id += "60" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    case AccountType.Fees:
                        id += "80" + GetAssetTypeNumericId(AssetType.GetValueOrDefault(Venture.AssetType.Undefined));
                        break;
                    default:
                        break;
                }
                id += Portfolio.NumericId;
                return id;
            }
        }

        public AccountType AccountType { get; private set; }

        public AssetType? AssetType { get; private set; }

        public PortfolioDefinition Portfolio { get; private set; }

        public string Currency { get; private set; }

        public bool IsResultAccount { get; private set; }

        private List<AccountEntry> entries = new List<AccountEntry>();

        public Account(AccountType type, AssetType? assetType, PortfolioDefinition portfolio, string currency)
        {
            AccountType = type;
            AssetType = assetType;
            Portfolio = portfolio;
            Currency = currency;

            IsResultAccount = AccountType == AccountType.Fees;
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
            foreach (var e in entries.Where(x => x.Date <= date))
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
    }
}
