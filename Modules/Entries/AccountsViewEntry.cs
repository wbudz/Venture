using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public AccountsViewEntry(Account account, DateTime date)
        {
            UniqueId = account.UniqueId;
            NumericId = account.NumericId;
            AccountCategory = account.AccountCategory.ToString();
            AccountType = account.AccountType.ToString();
            AssetType = account.AssetType?.ToString() ?? "";
            PortfolioId = account.Portfolio?.UniqueId ?? "";
            Broker = account.Portfolio?.Broker ?? "";
            Currency = account.Currency;
            DebitAmount = account.GetDebitAmount(date);
            CreditAmount = account.GetCreditAmount(date);
            NetAmount = account.GetNetAmount(date);
            // Events
            Entries = new(account.GetEntriesAsViewEntries(date));
        }
    }
}
