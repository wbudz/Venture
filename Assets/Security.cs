using Venture.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Assets
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
            AssetType = definition.InstrumentType;

            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountSrc;
            CustodyAccount = tr.AccountDst;
            Currency = tr.Currency;
            ValuationClass = tr.ValuationClass;

            SecurityDefinition = definition;
        }

        protected override void RecalculateBounds()
        {
            decimal count = 0;
            foreach (Events.Event e in Events)
            {
                if (e is Events.Purchase p)
                {
                    count = p.Count;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (e is Events.Sale s)
                {
                    count -= s.Count;
                    if (count <= 0)
                    {
                        bounds.endDate = s.Timestamp;
                        bounds.endIndex = s.TransactionIndex;
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
            bounds.endDate = Common.FinalDate.AddDays(1);
            bounds.endIndex = -1;
        }

        public override decimal GetCount(TimeArg time)
        {
            decimal count = 0;
            foreach (Events.Event e in GetEvents(time))
            {
                if (e is Events.Purchase p) count += p.Count;
                if (e is Events.Sale s) count -= s.Count;
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

        public override decimal GetNominalAmount()
        {
            var evt = Events.OfType<Events.Purchase>().FirstOrDefault();
            if (evt != null)
            {
                return GetNominalAmount(new TimeArg(TimeArgDirection.End, evt.Timestamp, evt.TransactionIndex));
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetUnrealizedPurchaseFee(TimeArg time)
        {
            decimal count = 0;
            decimal fee = 0;

            foreach (Events.Event e in GetEvents(time))
            {
                if (e is Events.Purchase purchase)
                {
                    count = purchase.Count;
                    fee += purchase.Fee;
                }
                if (e is Events.Sale sale)
                {
                    decimal current = Math.Round(sale.Count / count * fee, 2);
                    count -= sale.Count;
                    fee -= current;
                }
            }
            return fee;
        }
    }
}
