using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace Venture
{
    public enum Operation { Unspecified, AssetRecognition }

    public class Book
    {
        public string Name { get; private set; } = "Main";

        public bool ApplyTaxRules { get; private set; } = false;

        private Dictionary<(AccountType type, AssetType? assetType, PortfolioDefinition? portfolio, string currency), Account> accounts = new();

        private long globalOperationIndex = 0;

        private Queue<AccountEntry> pendingEntries = new Queue<AccountEntry>();

        public Book(string name, bool tax)
        {
            Name = name;
            ApplyTaxRules = tax;
        }

        public void Clear()
        {
            accounts.Clear();
        }

        public Account GetAccount(AccountType type, AssetType? assetType, PortfolioDefinition? portfolio, string currency)
        {
            Account? result;
            if (!accounts.TryGetValue((type, assetType, portfolio, currency), out result))
            {
                result = new Account(type, assetType, portfolio, currency);
                accounts.Add((type, assetType, portfolio, currency), result);
            }
            else if (result==null)
            {
                throw new Exception("Null account encountered in GetAccount() method.");
            }
            return result;
        }

        public IEnumerable<Account> GetResultAccounts(PortfolioDefinition? portfolio, string currency)
        {
            return accounts.Values.Where(x => x.IsResultAccount && x.Portfolio == portfolio && x.Currency == currency);
        }

        public decimal GetResult(DateTime date, PortfolioDefinition? portfolio)
        {
            return accounts.Values.Where(x => x.IsResultAccount && (portfolio == null || x.Portfolio == portfolio)).Sum(x => x.GetNetAmount(date));
        }

        public List<Modules.AccountsViewEntry> GetAccountsAsViewEntries(DateTime date, string selectedPortfolio, string selectedBroker, bool aggregateAssetTypes, bool aggregateCurrencies, bool aggregatePortfolios, bool aggregateBrokers)
        {
            Dictionary<string, Modules.AccountsViewEntry> output = new();

            var orderedAccounts = accounts.Values.Where(x => ((IFilterable)x).Filter(selectedPortfolio, selectedBroker)).OrderBy(x => x.NumericId);

            foreach (var a in orderedAccounts)
            {
                if (a.GetEntriesCount(date) == 0) continue;
                string numericId = Modules.AccountsViewEntry.GenerateNumericIdForAggregations(a.NumericId, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers);
                if (!output.ContainsKey(numericId))
                {
                    Modules.AccountsViewEntry ave = new Modules.AccountsViewEntry(a)
                    {
                        UniqueId = Modules.AccountsViewEntry.GenerateUniqueIdForAggregations(a, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers),
                        NumericId = Modules.AccountsViewEntry.GenerateNumericIdForAggregations(a.NumericId, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers),
                        AccountCategory = a.AccountCategory.ToString(),
                        AccountType = a.AccountType.ToString(),
                        AssetType = aggregateAssetTypes ? "*" : a.AssetType?.ToString() ?? "",
                        Currency = aggregateCurrencies ? "*" : a.Currency,
                        DebitAmount = a.GetDebitAmount(date),
                        CreditAmount = a.GetCreditAmount(date),
                        NetAmount = a.GetNetAmount(date)
                    };

                    string portfolioId = aggregatePortfolios ? "*" : a.Portfolio?.UniqueId ?? "";
                    string broker = aggregateBrokers ? "*" : a.Portfolio?.Broker ?? "";

                    ave.SetPortfolio(portfolioId, broker);

                    var accountEntries = a.GetEntriesAsViewEntries(date);
                    foreach (var e in accountEntries)
                    {
                        ave.Entries.Add(e);
                    }

                    output.Add(numericId, ave);
                }
                else
                {
                    Modules.AccountsViewEntry ave = output[numericId];
                    ave.DebitAmount += a.GetDebitAmount(date);
                    ave.CreditAmount += a.GetCreditAmount(date);
                    ave.NetAmount += a.GetNetAmount(date);

                    var accountEntries = a.GetEntriesAsViewEntries(date);
                    foreach (var e in accountEntries)
                    {
                        ave.Entries.Insert(ave.Entries.Count(x => x.Date <= e.Date), e); // keep entries ordered by date
                    }
                }
            }

            return output.Values.ToList();
        }

        public List<Modules.AccountEntriesViewEntry> GetAccountsAsViewEntries(long operationIndex, long transactionIndex)
        {
            List<Modules.AccountEntriesViewEntry> output = new();

            var orderedAccounts = accounts.Values.OrderBy(x => x.NumericId);

            foreach (var a in orderedAccounts)
            {
                foreach (var e in a.GetEntriesAsViewEntries(operationIndex, transactionIndex))
                {
                    output.Add(e);
                }
            }

            return output;
        }

        public void Enqueue(Account account, DateTime date, long transactionIndex, string description, decimal amount)
        {
            if (amount == 0) return;
            AccountEntry accountEntry = new AccountEntry(account, date, transactionIndex, description, amount);
            pendingEntries.Enqueue(accountEntry);
        }

        public void Commit()
        {
            if (pendingEntries.Count == 0) return;
            var sum = pendingEntries.Sum(x => x.Amount);
            if (sum != 0) throw new Exception("Non-zero sum of bookings.");

            var operationIndex = globalOperationIndex++;
            foreach (var entry in pendingEntries)
            {
                entry.OperationIndex = operationIndex;
            }

            while (pendingEntries.Count > 0)
            {
                AccountEntry accountEntry = pendingEntries.Dequeue();
                accountEntry.Account.Enter(accountEntry);
            }
        }
    }
}
