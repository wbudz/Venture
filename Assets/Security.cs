using Budziszewski.Venture.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Assets
{
    public abstract class Security : Asset
    {
        /// <summary>
        /// Underlying security (where applicable).
        /// </summary>
        public Data.Instrument SecurityDefinition { get; protected set; }

        public Security(Data.Transaction tr, Data.Instrument definition)
        {
            Index = tr.Index;

            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountSrc;
            CustodyAccount = tr.AccountDst;
            Currency = tr.Currency;
            ValuationClass = tr.ValuationClass;

            SecurityDefinition = definition;
        }

        protected override void RecalculateBounds()
        {
            decimal amount = 0;
            foreach (Events.Event e in Events)
            {
                if (e is Events.Purchase)
                {
                    amount = e.Amount;
                    bounds.startDate = e.Timestamp;
                    bounds.startIndex = e.TransactionIndex;
                }
                if (e is Events.Sale)
                {
                    amount -= e.Amount;
                    if (amount <= 0)
                    {
                        bounds.endDate = e.Timestamp;
                        bounds.endIndex = e.TransactionIndex;
                        return;
                    }
                }
                if (e is Events.Flow f)
                {
                    if (f.FlowType == FlowType.Redemption)
                    {
                        bounds.endDate = e.Timestamp;
                        bounds.endIndex = e.TransactionIndex;
                        return;
                    }
                }
            }
            bounds.endDate = Common.FinalDate;
            bounds.endIndex = -1;
        }

        public override decimal GetCount(TimeArg time)
        {
            decimal count = 0;
            foreach (Events.Event e in GetEvents(time))
            {
                if (e.Direction == Venture.Events.PaymentDirection.Inflow) count += e.Count;
                if (e.Direction == Venture.Events.PaymentDirection.Outflow) count -= e.Count;
                if ((e is Events.Flow f) && f.FlowType == Venture.Events.FlowType.Redemption) count = 0;
            }
            return count;
        }

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            decimal price = Events.OfType<Events.Purchase>().FirstOrDefault()?.Price ?? 0;
            if (!dirty) { price -= GetAccruedInterest(time.Date); }
            return price;
        }
    }
}
