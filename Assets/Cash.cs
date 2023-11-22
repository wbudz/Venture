using Budziszewski.Venture.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Budziszewski.Financial.Calendar;

namespace Budziszewski.Venture.Assets
{
    public class Cash : Asset
    {
        public Cash(Data.Transaction tr) : base(tr)
        {
            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountDst;
            CustodyAccount = "";
            Currency = tr.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, tr, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Events.Flow fl): base()
        {
            Portfolio = fl.ParentAsset.Portfolio;
            CashAccount = fl.ParentAsset.CashAccount;
            CustodyAccount = "";
            Currency = fl.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, fl, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            // No events to be generated.
        }

        protected override void RecalculateBounds()
        {
            decimal amount = 0;
            foreach (Events.Event e in Events)
            {
                if (e.Direction == Venture.Events.PaymentDirection.Inflow)
                {
                    amount = e.Amount;
                    bounds.startDate = e.Timestamp;
                    bounds.startIndex = e.TransactionIndex;
                }
                if (e.Direction == Venture.Events.PaymentDirection.Outflow)
                {
                    amount -= e.Amount;
                    if (amount <= 0)
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

        public override string ToString()
        {
            return $"Asset:Cash {UniqueId}";
        }

        public override decimal GetCount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetCouponRate(DateTime date)
        {
            return 0;
        }

        #region Price

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }
        public override decimal GetAccruedInterest(DateTime date)
        {
            return 0;
        }

        #endregion

        #region Asset amount

        public override decimal GetNominalAmount(TimeArg time)
        {
            if (!IsActive(time)) return 0;

            decimal amount = 0;
            foreach (Events.Event e in GetEvents(time))
            {
                if (e.Direction == Venture.Events.PaymentDirection.Inflow) amount += e.Amount;
                if (e.Direction == Venture.Events.PaymentDirection.Outflow) amount -= e.Amount;
            }
            return Math.Round(amount, 2);
        }

        public override decimal GetNominalAmount()
        {
            return Events.Where(x=>x.Direction==Venture.Events.PaymentDirection.Inflow).FirstOrDefault()?.Amount ?? 0;
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return GetNominalAmount(time);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Math.Round((decimal)GetMarketPrice(time, dirty) * GetNominalAmount(time), 2);
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Math.Round((decimal)GetAmortizedCostPrice(time, dirty) * GetNominalAmount(time), 2);
        }

        #endregion
    }
}
