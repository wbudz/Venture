using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Data
{

    public static class Definitions
    {
        public static ObservableCollection<Price> Prices = new ObservableCollection<Price>();
        public static ObservableCollection<Instrument> Instruments = new ObservableCollection<Instrument>();

        public static void Load()
        {
            CSV csv = new CSV(Properties.Settings.Default.PricesSource);
            csv.Read();
            foreach (var item in csv.Interpret<Price>().ToArray())
            {
                Prices.Add(item);
            }

            csv = new CSV(Properties.Settings.Default.InstrumentsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Instrument>().ToArray())
            {
                Instruments.Add(item);
            }
        }
    }
}
