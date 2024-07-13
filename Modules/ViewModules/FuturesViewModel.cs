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
    public class FuturesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FuturesViewEntry> FuturesEntries { get; set; } = new ObservableCollection<FuturesViewEntry>();

        private FuturesViewEntry? currentEntry;
        public FuturesViewEntry? CurrentEntry
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
