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
        public static ObservableCollection<Manual> Manual = new ObservableCollection<Manual>();

        public static void Load()
        {
            Prices.Clear();
            Instruments.Clear();
            Transactions.Clear();
            Dividends.Clear();
            Coupons.Clear();
            Manual.Clear();

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

            var transactions = new List<Transaction>();
            csv = new CSV(Properties.Settings.Default.TransactionsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Transaction>().ToArray())
            {
                transactions.Add(item);
            }
            transactions = transactions.OrderBy(x => x.Timestamp).ThenBy(x => x.Index).ToList();
            CheckTransactionsOrder(transactions);
            transactions.ForEach(x => Transactions.Add(x));

            csv = new CSV(Properties.Settings.Default.DividendsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Dividend>().ToArray())
            {
                Dividends.Add(item);
            }

            var coupons = new List<Coupon>();
            csv = new CSV(Properties.Settings.Default.CouponsSource);
            csv.Read();
            foreach (var item in csv.Interpret<Coupon>().ToArray())
            {
                coupons.Add(item);
            }
            coupons = coupons.OrderBy(x => x.InstrumentId).ThenBy(x => x.Timestamp).ToList();
            coupons.ForEach(x => Coupons.Add(x));

            csv = new CSV(Properties.Settings.Default.ManualSource);
            csv.Read();
            foreach (var item in csv.Interpret<Manual>().ToArray())
            {
                Manual.Add(item);
            }
            CheckManualReferences(Manual, Transactions, Instruments);
        }

        public static Manual? GetManualAdjustment(ManualAdjustmentType type, DateTime timestamp, Transaction tr)
        {
            string id = $"{tr.InstrumentType}_{tr.InstrumentId}_{tr.Index}";
            return GetManualAdjustment(type, timestamp, id);
        }

        public static Manual? GetManualAdjustment(ManualAdjustmentType type, DateTime timestamp, string instrumentId)
        {
            return Manual.FirstOrDefault(x => x.AdjustmentType == type && x.Timestamp == timestamp && x.Text1 == instrumentId);
        }

        public static Manual? GetManualAdjustment(ManualAdjustmentType type, string instrumentId)
        {
            return Manual.FirstOrDefault(x => x.AdjustmentType == type && x.Text1 == instrumentId);
        }

        public static IEnumerable<Manual> GetManualEventSources()
        {
            return Manual.Where(x => x.AdjustmentType == ManualAdjustmentType.EquitySpinOff || 
            x.AdjustmentType == ManualAdjustmentType.EquityRedemption || 
            x.AdjustmentType == ManualAdjustmentType.AccountBalanceInterest);
        }

        private static void CheckTransactionsOrder(List<Data.Transaction> transactions)
        {
            for (int i = 1; i < transactions.Count; i++)
            {
                if (transactions[i].Index <= transactions[i - 1].Index) throw new Exception($"Error ordering transactions: {transactions[i - 1]}, {transactions[i]}.");
                if (transactions[i].Timestamp < transactions[i - 1].Timestamp) throw new Exception($"Error ordering transactions: {transactions[i - 1]}, {transactions[i]}.");
            }
        }

        private static void CheckManualReferences(IEnumerable<Manual> manual, IEnumerable<Transaction> transactions, IEnumerable<Instrument> instruments)
        {
            foreach (var m in manual)
            {
                if (m.AdjustmentType == ManualAdjustmentType.CouponAmountAdjustment ||
                    m.AdjustmentType == ManualAdjustmentType.CouponTaxAdjustment ||
                    m.AdjustmentType == ManualAdjustmentType.IncomeTaxAdjustment ||
                    m.AdjustmentType == ManualAdjustmentType.DividendAmountAdjustment ||
                    m.AdjustmentType == ManualAdjustmentType.RedemptionTaxAdjustment ||
                    m.AdjustmentType == ManualAdjustmentType.DividendTaxAdjustment)
                {
                    string instrumentType;
                    string instrumentId;
                    int transactionIndex;
                    try
                    {
                        instrumentType = m.Text1.Split('_')[0];
                        instrumentId = m.Text1.Split('_')[1];
                        transactionIndex = Int32.Parse(m.Text1.Split('_')[2]);
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {m}.");
                    }
                    var transaction = transactions.SingleOrDefault(x => x.InstrumentType.ToString() == instrumentType && x.InstrumentId == instrumentId && x.Index == transactionIndex);
                    if (transaction == null)
                    {
                        throw new Exception($"Transaction not found for manual event definition: {m}.");
                    }
                }
                if (m.AdjustmentType == ManualAdjustmentType.EquitySpinOff ||
                    m.AdjustmentType == ManualAdjustmentType.PrematureRedemption)
                {
                    string instrumentType;
                    string instrumentId;
                    try
                    {
                        instrumentType = m.Text1.Split('_')[0];
                        instrumentId = m.Text1.Split('_')[1];
                        if (m.Text1.Split('_').Length > 2) throw new Exception("Manual event definition too long.");
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {m}.");
                    }
                    var instrument = instruments.SingleOrDefault(x => x.InstrumentType.ToString() == instrumentType && x.InstrumentId == instrumentId);
                    if (instrument == null)
                    {
                        throw new Exception($"Instrument not found for manual event definition: {m}.");
                    }
                }
                if (m.AdjustmentType == ManualAdjustmentType.AccountBalanceInterest)
                {

                }
            }
        }
    }
}
