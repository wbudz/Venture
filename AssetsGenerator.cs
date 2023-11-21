using Budziszewski.Venture.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Budziszewski.Venture
{
    public static class AssetsGenerator
    {
        public static List<Assets.Asset> GenerateAssets()
        {
            List<Assets.Asset> output = new List<Assets.Asset>();
            Queue<Data.Transaction> transactions = new Queue<Data.Transaction>(Data.Definitions.Transactions);
            HashSet<Events.Flow> events = new HashSet<Events.Flow>();

            while (transactions.Count > 0)
            {
                Data.Transaction tr = transactions.Dequeue();

                // Go through pending events that come before (influence) current transaction - e.g. dividends, coupons that may add new cash
                foreach (var ev in events.Where(x => x.Timestamp <= tr.SettlementDate))
                {
                    AddAsset(output, new Cash(ev), ev.Timestamp);
                    events.Remove(ev);
                }

                // Process the transaction
                if (tr.TransactionType == Data.TransactionType.Cash)
                {
                    // Register cash addition
                    if (!String.IsNullOrEmpty(tr.AccountDst))
                    {
                        AddAsset(output, new Cash(tr), tr.SettlementDate);
                    }
                    // Register cash deduction
                    if (!String.IsNullOrEmpty(tr.AccountSrc))
                    {
                        RegisterCashDeduction(output, tr);
                    }
                }
            }

            return output;
        }

        public static void RegisterCashDeduction(List<Assets.Asset> list, Data.Transaction tr)
        {
            if (tr.TransactionType != Data.TransactionType.Cash) throw new ArgumentException("RegisterCashDeduction was called for transaction type other than cash transaction.");

            decimal remainingAmount = tr.NominalAmount;

            var cash = list.OfType<Cash>().Where(x => x.Currency == tr.Currency && x.CashAccount == tr.AccountSrc && x.Portfolio == tr.PortfolioSrc).OrderBy(y => y.Index);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.Start, tr.SettlementDate, tr.Index));
                if (currentAmount == 0) continue;
                var evt = new Events.Payment(c, tr, Math.Min(currentAmount, remainingAmount), Events.PaymentDirection.Outflow);
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                //Log.Report(Severity.Error, "No possible source for cash deduction.", e);
                throw new Exception($"No possible source for cash deduction: {tr}.");
            }
        }

        public static void AddAsset(List<Assets.Asset> list, Assets.Asset asset, DateTime timestamp)
        {
            var index = list.FindIndex(x => x.Events.First().Timestamp > timestamp);
            list.Insert(index == -1 ? list.Count : index, asset);
        }
    }
}
