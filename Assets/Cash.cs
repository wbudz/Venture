﻿using Venture.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Assets
{
    public class Cash : Asset
    {
        public Cash(Data.Transaction tr) : base(tr)
        {
            decimal amount;
            switch (tr.TransactionType)
            {
                case Data.TransactionType.Undefined: throw new Exception("Tried creating cash with undefined transaction type.");
                case Data.TransactionType.Buy: throw new Exception("Tried creating cash with buy transaction type.");
                case Data.TransactionType.Sell: Portfolio = tr.PortfolioSrc; amount = tr.Amount - tr.Fee; break;
                case Data.TransactionType.Cash: Portfolio = tr.PortfolioDst; amount = tr.Amount; break;
                default: throw new Exception("Tried creating cash with unknown transaction type.");
            }
            AssetType = AssetType.Cash;
            CashAccount = tr.AccountDst;
            CustodyAccount = "";
            Currency = tr.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, tr, amount, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Events.Flow fl) : base()
        {
            AssetType = AssetType.Cash;
            Portfolio = fl.ParentAsset.Portfolio;
            CashAccount = fl.ParentAsset.CashAccount;
            CustodyAccount = "";
            Currency = fl.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            // Recalculate current amount of flow
            fl.RecalculateAmount();

            if (fl.Amount != 0)
            {
                AddEvent(new Events.Payment(this, fl, Venture.Events.PaymentDirection.Inflow));
                GenerateFlows();
            }
        }

        protected override void GenerateFlows()
        {
            // No events to be generated.
        }

        protected override void RecalculateBounds()
        {
            decimal amount = 0;
            foreach (Events.Payment p in Events.OfType<Events.Payment>())
            {
                if (p.Direction == Venture.Events.PaymentDirection.Inflow)
                {
                    amount = p.Amount;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (p.Direction == Venture.Events.PaymentDirection.Outflow)
                {
                    amount -= p.Amount;
                    if (amount <= 0)
                    {
                        bounds.endDate = p.Timestamp;
                        bounds.endIndex = p.TransactionIndex;
                        return;
                    }
                }
            }
            bounds.endDate = Common.FinalDate.AddDays(1);
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
            foreach (Events.Payment p in GetEvents(time).OfType<Events.Payment>())
            {
                if (p.Direction == Venture.Events.PaymentDirection.Inflow) amount += p.Amount;
                if (p.Direction == Venture.Events.PaymentDirection.Outflow) amount -= p.Amount;
            }
            return Math.Round(amount, 2);
        }

        public override decimal GetNominalAmount()
        {
            return Events.OfType<Events.Payment>().Where(x => x.Direction == Venture.Events.PaymentDirection.Inflow).FirstOrDefault()?.Amount ?? 0;
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

        #region Parameters

        public override double GetTenor(DateTime date)
        {
            return 0;
        }

        public override double GetModifiedDuration(DateTime date)
        {
            return 0;
        }

        public override double GetYieldToMaturity(DateTime date, double price)
        {
            return 0;
        }

        #endregion

        public override decimal GetUnrealizedPurchaseFee(TimeArg time)
        {
            return 0;
        }

        #region Income

        public override decimal GetTimeValueOfMoneyIncome(TimeArg time)
        {
            return 0;
        }

        public override decimal GetCashflowIncome(TimeArg time)
        {
            return 0;
        }

        public override decimal GetRealizedGainsLossesFromValuation(Events.Event e)
        {
            return 0;
        }

        public override decimal GetUnrealizedGainsLossesFromValuation(TimeArg time)
        {
            return 0;
        }

        public override decimal GetRealizedGainsLossesFromFX(Events.Event e)
        {
            throw new NotImplementedException();
        }

        public override decimal GetUnrealizedGainsLossesFromFX(TimeArg time)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
