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
                    //var index = output.FindIndex(x => x.Events.First().Timestamp > ev.Timestamp);
                    //output.Insert(index == -1 ? output.Count : index, new Cash(ev));
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
                    if (!String.IsNullOrEmpty(tr.AccountSrc)) RegisterCashDeduction(null);
                }
            }

            return output;
        }

        public static Assets.Cash RegisterCashDeduction(Data.Transaction tr)
        {
            if (tr.TransactionType != Data.TransactionType.Cash) throw new ArgumentException("RegisterCashDeduction was called for transaction type other than cash transaction.");
            return null;
        }

        public static void AddAsset(List<Assets.Asset> list, Assets.Asset asset, DateTime timestamp)
        {
            var index = list.FindIndex(x => x.Events.First().Timestamp > timestamp);
            list.Insert(index == -1 ? list.Count : index, asset);
        }
    }
}
