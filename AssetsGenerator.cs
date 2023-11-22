using Budziszewski.Venture.Assets;
using Budziszewski.Venture.Data;
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
                if (tr.TransactionType == Data.TransactionType.Buy)
                {
                    var security = Data.Definitions.Instruments.FirstOrDefault(x => x.InstrumentId == tr.InstrumentId);
                    if (security == null) throw new Exception("Purchase transaction definition pointed to unknown instrument id.");

                    Asset asset;
                    DateTime date;
                    switch (security.InstrumentType)
                    {
                        case InstrumentType.Undefined: throw new Exception("Tried creating asset with undefined instrument type.");
                        case InstrumentType.Cash: throw new Exception("Tried creating asset with purchase transaction and cash instrument type.");
                        case InstrumentType.Equity: asset = new Assets.Equity(tr, security); date = tr.TradeDate; break;
                        case InstrumentType.Bond: throw new NotImplementedException();
                        case InstrumentType.ETF: throw new NotImplementedException();
                        case InstrumentType.Fund: throw new NotImplementedException();
                        case InstrumentType.Futures: throw new NotImplementedException();
                        default: throw new Exception("Tried creating asset with unknown instrument type.");
                    }

                    AddAsset(output, asset, date);
                        //if (transaction.TradeDate != transaction.SettlementDate)
                        //{
                        //    RegisterPayable(new Events.Inflow(transaction, false), new Events.Outflow(transaction, true)); // create payable if necessary
                        //}
                    // Subtract cash used for purchase
                    RegisterCashDeduction(output, tr);
                }
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

            Common.RefreshReportingYears();
            return output;
        }

        public static void RegisterCashDeduction(List<Assets.Asset> list, Data.Transaction tr)
        {
            decimal remainingAmount = tr.Amount + tr.Fee;

            string portfolio = "";
            switch (tr.TransactionType)
            {
                case TransactionType.Undefined: throw new Exception("Tried deducting cash with undefined transaction type.");
                case TransactionType.Buy: portfolio = tr.PortfolioDst; break;
                case TransactionType.Sell: throw new Exception("Tried deducting cash with sale transaction type.");
                case TransactionType.Cash: portfolio = tr.PortfolioSrc; break;
                default: throw new Exception("Tried deducting cash with unknown transaction type.");
            }
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = list.OfType<Cash>().Where(x => x.Currency == tr.Currency && x.CashAccount == tr.AccountSrc && x.Portfolio == portfolio);
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
