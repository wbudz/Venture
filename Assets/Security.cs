using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture
{
    public abstract class Security : StandardAsset
    {
        /// <summary>
        /// Underlying security (where applicable).
        /// </summary>
        public InstrumentDefinition SecurityDefinition { get; protected set; }

        public Security(BuyTransactionDefinition btd, InstrumentDefinition definition) : base()
        {
            UniqueId = $"{definition.AssetType}_{definition.AssetId}_{btd.Index}";
            AssetType = definition.AssetType;

            Portfolio = btd.PortfolioDst;
            CashAccount = btd.AccountSrc;
            CustodyAccount = btd.AccountDst;
            Currency = btd.Currency;
            ValuationClass = btd.ValuationClass;

            SecurityDefinition = definition;
        }

        public Security(TransferTransactionDefinition ttd, Security originalAsset) : base()
        {
            UniqueId = $"{originalAsset.SecurityDefinition.AssetType}_{originalAsset.SecurityDefinition.AssetId}_{ttd.Index}";
            AssetType = originalAsset.SecurityDefinition.AssetType;

            Portfolio = ttd.PortfolioDst;
            CashAccount = originalAsset.CashAccount;
            CustodyAccount = ttd.AccountDst;
            Currency = ttd.Currency;
            ValuationClass = ttd.ValuationClass;

            SecurityDefinition = originalAsset.SecurityDefinition;
        }

        public Security(Security template, InstrumentDefinition definition, string identifier)
        {
            UniqueId = $"{definition.AssetType}_{definition.AssetId}_{template.UniqueId}_{identifier}";
            AssetType = definition.AssetType;

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
            foreach (Event e in Events)
            {
                if (e is RecognitionEvent p)
                {
                    count = p.Count;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (e is DerecognitionEvent s)
                {
                    count -= s.Count;
                    if (count <= 0)
                    {
                        bounds.endDate = s.Timestamp;
                        bounds.endIndex = s.TransactionIndex;
                        return;
                    }
                }
                if (e is FlowEvent f)
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

        public override decimal GetCount()
        {
            if (Events.FirstOrDefault() is RecognitionEvent p) return p.Count;
            else throw new Exception("First event is not recognition.");
        }

        public override decimal GetCount(TimeArg time)
        {
            decimal count = 0;
            foreach (Event e in GetEventsUntil(time))
            {
                if (e is RecognitionEvent p) count += p.Count;
                if (e is DerecognitionEvent s) count -= s.Count;
                if ((e is FlowEvent f) && f.FlowType == FlowType.Redemption) count = 0;
            }
            return count;
        }

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            var evt = Events.OfType<RecognitionEvent>().FirstOrDefault();

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
            var evt = Events.OfType<RecognitionEvent>().FirstOrDefault();
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

            foreach (Event e in GetEventsUntil(time))
            {
                if (e is RecognitionEvent purchase)
                {
                    count = purchase.Count;
                    fee += purchase.Fee;
                }
                if (e is DerecognitionEvent sale)
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
