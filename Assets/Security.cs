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

            Portfolio = Definitions.Portfolios.Single(x => x.UniqueId == btd.PortfolioDst);

            Currency = btd.Currency;
            ValuationClass = btd.ValuationClass;

            SecurityDefinition = definition;
        }

        public Security(AssetSwitchTransactionDefinition astd, InstrumentDefinition definition) : base()
        {
            UniqueId = $"{definition.AssetType}_{definition.AssetId}_{astd.Index}";
            AssetType = definition.AssetType;

            Portfolio = Definitions.Portfolios.Single(x => x.UniqueId == astd.PortfolioDst);

            Currency = astd.Currency;
            ValuationClass = astd.ValuationClass;

            SecurityDefinition = definition;
        }

        public Security(TransferTransactionDefinition ttd, Security originalAsset) : base()
        {
            UniqueId = $"{originalAsset.SecurityDefinition.AssetType}_{originalAsset.SecurityDefinition.AssetId}_{ttd.Index}";
            AssetType = originalAsset.SecurityDefinition.AssetType;

            Portfolio = Definitions.Portfolios.Single(x => x.UniqueId == ttd.PortfolioDst);

            Currency = ttd.Currency;
            ValuationClass = ttd.ValuationClass;

            SecurityDefinition = originalAsset.SecurityDefinition;
        }

        public Security(Security template, InstrumentDefinition definition, string identifier)
        {
            UniqueId = $"{definition.AssetType}_{definition.AssetId}_{template.UniqueId}_{identifier}";
            AssetType = definition.AssetType;

            Portfolio = template.Portfolio;
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
            bounds.endDate = Common.EndDate.AddDays(1);
            bounds.endIndex = -1;
        }

        public override ValuationEvent? GenerateValuation(DateTime date, bool redemption)
        {
            ValuationEvent e = new ValuationEvent(this, date, redemption);
            AddEvent(e);
            return e;
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

        public override decimal GetPurchasePrice(TimeArg time, bool dirty, bool original)
        {
            if (!IsActive(time)) return 0;

            var evt = Events.OfType<RecognitionEvent>().FirstOrDefault();

            if (evt != null)
            {
                if (original)
                {
                    if (dirty)
                    {
                        return evt.OriginalDirtyPrice;
                    }
                    else
                    {
                        return evt.OriginalCleanPrice;

                    }
                }
                else
                {
                    if (dirty)
                    {
                        return evt.DirtyPrice;

                    }
                    else
                    {
                        return evt.CleanPrice;

                    }
                }
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
                    fee += purchase.OriginalFee;
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
