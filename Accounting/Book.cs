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
        public bool ApplyTaxRules { get; private set; } = false;

        private List<Account> accounts = new List<Account>();

        private long globalOperationIndex = 0;

        private Queue<AccountEntry> pendingEntries = new Queue<AccountEntry>();

        public Book(bool tax)
        {
            ApplyTaxRules = tax;
        }

        public void Clear()
        {
            accounts.Clear();
        }

        public void Process()
        {
            Clear();

            var dates = Financial.Calendar.GenerateReportingDates(Common.StartDate, Common.EndDate, Financial.Calendar.TimeStep.Monthly).ToArray();

            for (int i = 1; i < dates.Length; i++)
            {
                // Process accounting schemes
                foreach (var asset in Common.Assets)
                {
                    if (!asset.IsActive(dates[i - 1], dates[i])) continue;

                    //foreach (var a in assetSpecificSchemes)
                    //{
                    //    a.Process(inv, dates[i - 1], dates[i]);
                    //}
                }

                //foreach (var a in generalLedgerSchemes)
                //{
                //    a.Process(dates[i - 1], dates[i]);
                //}

            }
        }

        public Account GetAccount(AccountType type, AssetType? assetType, PortfolioDefinition portfolio, string currency)
        {
            var account = accounts.SingleOrDefault(x=>x.AccountType == type 
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

        public IEnumerable<Modules.AccountsViewEntry> GetAccountsAsViewEntries(DateTime date)
        {
            foreach (var a in accounts.OrderBy(x=>x.NumericId))
            {
                if (a.GetEntriesCount(date) == 0) continue;
                yield return new Modules.AccountsViewEntry(a, date);
            }
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
            if (pendingEntries.First().Date >= new DateTime(2015, 1, 31)) return;// TODO: remove
            var sum = pendingEntries.Sum(x => x.Amount);
             if (sum != 0) throw new Exception("Non-zero sum of bookings."); 

            var operationIndex = globalOperationIndex++;
            foreach (var entry in pendingEntries)
            {
                entry.OperationIndex = operationIndex;
            }

            while (pendingEntries.Count>0)
            {
                AccountEntry accountEntry = pendingEntries.Dequeue();
                accountEntry.Account.Enter(accountEntry);
            }
        }
    }
}
