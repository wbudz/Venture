using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Venture.Data
{

    public static class Definitions
    {
        public static ObservableCollection<Price> Prices = new ObservableCollection<Price>();
        public static ObservableCollection<Instrument> Instruments = new ObservableCollection<Instrument>();
        public static ObservableCollection<Transaction> Transactions = new ObservableCollection<Transaction>();
        public static ObservableCollection<Dividend> Dividends = new ObservableCollection<Dividend>();
        public static ObservableCollection<Coupon> Coupons = new ObservableCollection<Coupon>();

        public static void Load()
        {
            Prices.Clear();
            Instruments.Clear();
            Transactions.Clear();
            Dividends.Clear();
            Coupons.Clear();

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

            csv = new CSV(Properties.Settings.Default.TransactionsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Transaction>().ToArray())
            {
                Transactions.Add(item);
            }

            csv = new CSV(Properties.Settings.Default.DividendsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Dividend>().ToArray())
            {
                Dividends.Add(item);
            }

            csv = new CSV(Properties.Settings.Default.CouponsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Coupon>().ToArray())
            {
                Coupons.Add(item);
            }
        }
    }
}
