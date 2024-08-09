using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class AccountEntriesViewEntry : ModuleEntry
    {
        public string AccountId { get; private set; }

        public DateTime Date { get; private set; }

        public long OperationIndex { get; set; }

        public long TransactionIndex { get; private set; }

        public string Description { get; private set; } = "";

        public decimal Amount { get; private set; }

        public AccountEntriesViewEntry(AccountEntry entry)
        {
            UniqueId = entry.UniqueId;
            AccountId = entry.Account.NumericId;
            PortfolioId = entry.Account.Portfolio?.UniqueId ?? "";
            Broker = entry.Account.Portfolio?.Broker ?? "";
            Currency = entry.Account.Currency;
            Date = entry.Date;
            OperationIndex = entry.OperationIndex;
            TransactionIndex = entry.TransactionIndex;
            Description = entry.Description;
            Amount = entry.Amount;
        }
    }
}
