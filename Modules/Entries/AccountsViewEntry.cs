using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Venture;

namespace Venture.Modules
{
    public class AccountsViewEntry : ModuleEntry
    {
        public string NumericId { get; set; } = "";

        public string AccountCategory { get; set; } = "";

        public string AccountType { get; set; } = "";

        public string AssetType { get; set; } = "";

        public decimal DebitAmount { get; set; } = 0;

        public decimal CreditAmount { get; set; } = 0;

        public decimal NetAmount { get; set; } = 0;

        public ObservableCollection<AccountEntriesViewEntry> Entries { get; set; } = new ObservableCollection<AccountEntriesViewEntry>();

        public AccountsViewEntry(Account account)
        {
            UniqueId = account.UniqueId;
        }

        public void SetPortfolio(PortfolioDefinition portfolio)
        {
            PortfolioId = portfolio?.UniqueId ?? "";
            Broker = portfolio?.Broker ?? "";
        }

        public void SetPortfolio(string portfolioId, string broker)
        {
            PortfolioId = portfolioId;
            Broker = broker;
        }

        public static string GenerateUniqueIdForAggregations(Account a, bool aggregateAssetTypes, bool aggregateCurrencies, bool aggregatePortfolios, bool aggregateBrokers)
        {
            string id = a.AccountType.ToString();

            if (!aggregateAssetTypes && a.AssetType != null)
            {
                id += "_" + a.AssetType.ToString();
            }
            if (!aggregatePortfolios && a.Portfolio != null)
            {
                id += "_" + a.Portfolio.UniqueId;
            }
            if (!aggregateBrokers && a.Portfolio != null)
            {
                id += "_" + a.Portfolio.Broker;
            }
            if (!aggregateCurrencies)
            {
                id += "_" + a.Currency;
            }

            return id;
        }

        public static string GenerateNumericIdForAggregations(string numericId, bool aggregateAssetTypes, bool aggregateCurrencies, bool aggregatePortfolios, bool aggregateBrokers)
        {
            if (aggregateAssetTypes)
            {
                numericId = numericId.Remove(2, 2);
                numericId = numericId.Insert(2, "**");
            }
            if (aggregateCurrencies)
            {
                numericId = numericId.Remove(4, 1);
                numericId = numericId.Insert(4, "*");
            }
            if (aggregatePortfolios)
            {
                numericId = numericId.Remove(5, 1);
                numericId = numericId.Insert(5, "*");
            }
            if (aggregateBrokers)
            {
                numericId = numericId.Remove(6, 1);
                numericId = numericId.Insert(6, "*");
            }

            return numericId;
        }
    }
}