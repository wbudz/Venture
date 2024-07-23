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
        public string NumericId { get; private set; }

        public string AccountCategory { get; private set; }

        public string AccountType { get; private set; }

        public string AssetType { get; set; } = "";

        public decimal DebitAmount { get; set; } = 0;

        public decimal CreditAmount { get; set; } = 0;

        public decimal NetAmount { get; set; } = 0;

        public ObservableCollection<AccountEntriesViewEntry> Entries { get; set; } = new ObservableCollection<AccountEntriesViewEntry>();

        public AccountsViewEntry(Account account, DateTime date, bool aggregateAssetTypes, bool aggregateCurrencies, bool aggregatePortfolios, bool aggregateBrokers)
        {
            UniqueId = GenerateUniqueIdForAggregations(account, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers);

            NumericId = GenerateNumericIdForAggregations(account.NumericId, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers);

            AccountCategory = account.AccountCategory.ToString();
            AccountType = account.AccountType.ToString();
            AssetType = aggregateAssetTypes ? "*" : account.AssetType?.ToString() ?? "";
            PortfolioId = aggregatePortfolios ? "*" : account.Portfolio?.UniqueId ?? "";
            Broker = aggregateBrokers ? "*" : account.Portfolio?.Broker ?? "";
            Currency = aggregateCurrencies ? "*" : account.Currency;
            DebitAmount = account.GetDebitAmount(date);
            CreditAmount = account.GetCreditAmount(date);
            NetAmount = account.GetNetAmount(date);
            // Events
            Entries = new(account.GetEntriesAsViewEntries(date));
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