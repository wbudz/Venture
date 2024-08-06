using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class AccountsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AccountsViewEntry> AccountEntries { get; set; } = new ObservableCollection<AccountsViewEntry>();

        private AccountsViewEntry? currentEntry;
        public AccountsViewEntry? CurrentEntry
        {
            get { return currentEntry; }
            set { currentEntry = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
