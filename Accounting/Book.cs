using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public enum Operation { Unspecified, AssetRecognition }

    public class Book
    {
        public string Name { get; private set; } = "Main";

        public bool ApplyTaxRules { get; private set; } = false;

        private List<Account> accounts = new List<Account>();

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
            var account = accounts.SingleOrDefault(x => x.AccountType == type
                && (assetType == null || x.AssetType == assetType)
                && x.Portfolio == portfolio
                && x.Currency == currency);
            if (account == null)
            {
                account = new Account(type, assetType, portfolio, currency);
                accounts.Add(account);
            }
            return account;
        }

        public IEnumerable<Account> GetResultAccounts(PortfolioDefinition? portfolio, string currency)
        {
            return accounts.Where(x => x.IsResultAccount && x.Portfolio == portfolio && x.Currency == currency);
        }

        public List<Modules.AccountsViewEntry> GetAccountsAsViewEntries(DateTime date, bool aggregateAssetTypes, bool aggregateCurrencies, bool aggregatePortfolios, bool aggregateBrokers)
        {
            List<Modules.AccountsViewEntry> output = new List<Modules.AccountsViewEntry>();

            foreach (var a in accounts.OrderBy(x => x.NumericId))
            {
                if (a.GetEntriesCount(date) == 0) continue;

                string numericId = Modules.AccountsViewEntry.GenerateNumericIdForAggregations(a.NumericId, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers);
                Modules.AccountsViewEntry? entry = null;

                entry = output.SingleOrDefault(x => x.NumericId == numericId);

                if (entry == null)
                {
                    entry = new Modules.AccountsViewEntry(a, date, aggregateAssetTypes, aggregateCurrencies, aggregatePortfolios, aggregateBrokers);
                    output.Add(entry);
                }
                else
                {
                    entry.DebitAmount += a.GetDebitAmount(date);
                    entry.CreditAmount += a.GetCreditAmount(date);
                    entry.NetAmount += a.GetNetAmount(date);
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
