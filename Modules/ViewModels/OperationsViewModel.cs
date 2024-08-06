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
    public class OperationsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AccountEntriesViewEntry> OperationsEntries { get; set; } = new ObservableCollection<AccountEntriesViewEntry>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
