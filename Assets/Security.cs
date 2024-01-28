using Venture.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
            UniqueId = $"{definition.InstrumentType}_{definition.InstrumentId}_{tr.Index}";
            Index = tr.Index;
            AssetType = definition.InstrumentType;

            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountSrc;
            CustodyAccount = tr.AccountDst;
            Currency = tr.Currency;
            ValuationClass = tr.ValuationClass;

            SecurityDefinition = definition;
        }

        public Security(Security template, Data.Instrument definition, string identifier)
        {
            UniqueId = $"{definition.InstrumentType}_{definition.InstrumentId}_{template.Index}_{identifier}";
            AssetType = definition.InstrumentType;

            Portfolio = template.Portfolio;
            CashAccount = template.CashAccount;
            CustodyAccount = template.CustodyAccount;
            Currency = template.Currency;
            ValuationClass = template.ValuationClass;

            SecurityDefinition = definition;
        }

        protected override void RecalculateBounds()
        {
            decimal count = 0;
            foreach (Events.Event e in Events)
            {
                if (e is Events.Recognition p)
                {
                    count = p.Count;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (e is Events.Derecognition s)
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
                if (e is Events.Recognition p) count += p.Count;
                if (e is Events.Derecognition s) count -= s.Count;
                if ((e is Events.Flow f) && f.FlowType == Venture.Events.FlowType.Redemption) count = 0;
            }
            return count;
        }

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            var evt = Events.OfType<Events.Recognition>().FirstOrDefault();

            if (evt != null)
            {
                return dirty ? evt.DirtyPrice : evt.CleanPrice;
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetNominalAmount()
        {
            var evt = Events.OfType<Events.Recognition>().FirstOrDefault();
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
                if (e is Events.Recognition purchase)
                {
                    count = purchase.Count;
                    fee += purchase.Fee;
                }
                if (e is Events.Derecognition sale)
                {
                    decimal current = Common.Round(sale.Count / count * fee);
                    count -= sale.Count;
                    fee -= current;
                }
            }
            return fee;
        }
    }
}
