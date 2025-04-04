﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Venture
{
    public class Fund : Security
    {
        public Fund(BuyTransactionDefinition btd, InstrumentDefinition definition) : base(btd, definition)
        {
            AddEvent(new RecognitionEvent(this, btd));
            GenerateFlows();
        }

        public Fund(TransferTransactionDefinition ttd, Fund originalAsset) : base(ttd, originalAsset)
        {
            AddEvent(new RecognitionEvent(this, ttd, originalAsset));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            foreach (var d in Definitions.Dividends.Where(x => x.InstrumentUniqueId == InstrumentUniqueId && x.RecordDate >= events.First().Timestamp))
            {
                decimal count = GetCount(new TimeArg(TimeArgDirection.End, d.RecordDate));
                AddEvent(new FlowEvent(this, d.RecordDate, d.PaymentDate, FlowType.Dividend, count, d.PaymentPerShare, d.Currency, d.FXRate));
            }
        }

        public override string ToString()
        {
            return $"Asset:Equity {UniqueId}";
        }

        public override decimal GetCouponRate(DateTime date)
        {
            return 0;
        }

        public override decimal GetNominalPrice()
        {
            return 1;
        }

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            decimal price = Definitions.GetPrice(this.SecurityDefinition, time.Date);

            //PriceDefinition? price = Definitions.Prices.LastOrDefault(x => x.InstrumentUniqueId == this.InstrumentUniqueId && x.Timestamp <= time.Date);
            //if (price == null)
            //{
            //    throw new Exception($"No price for: {UniqueId} at date: {time.Date:yyyy-MM-dd}.");
            //}
            //else
            //{
            //    return price.Value;
            //}

            return price;
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            return GetPurchasePrice(time, dirty);
        }

        public override decimal GetAccruedInterest(DateTime date)
        {
            return 0;
        }

        public override decimal GetNominalAmount(TimeArg time)
        {
            return Common.Round(GetPurchaseAmount(time, false));
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return Common.Round(GetPurchasePrice(time, dirty) * GetCount(time));
        }

        public override decimal GetPurchaseAmount(bool dirty)
        {
            return Common.Round(GetPurchasePrice(dirty) * GetCount());
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Common.Round(GetMarketPrice(time, dirty) * GetCount(time));
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Common.Round(GetAmortizedCostPrice(time, dirty) * GetCount(time));
        }

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

        public override double GetYieldToMaturity(DateTime date)
        {
            return 0;
        }

        #endregion

        #region Income

        public override decimal GetTimeValueOfMoneyIncome(TimeArg end)
        {
            return 0;
        }

        public override decimal GetCashflowIncome(TimeArg end)
        {
            decimal income = 0;

            foreach (var e in GetEventsUntil(end))
            {
                if (e is FlowEvent f && f.FlowType == FlowType.Dividend)
                {
                    income += Common.Round(f.Amount);
                }
            }

            return income;
        }

        public override decimal GetRealizedGainsLossesFromValuation(Event e)
        {
            if (!(e is DerecognitionEvent))
            {
                throw new ArgumentException("GetRealizedGainsLossesFromValuation called for different event type than sale.");
            }

            DerecognitionEvent s = (DerecognitionEvent)e;

            decimal factor = s.Count / GetCount(new TimeArg(TimeArgDirection.Start, s.Timestamp, s.TransactionIndex));
            decimal result = factor * GetUnrealizedGainsLossesFromValuation(new TimeArg(TimeArgDirection.Start, s.Timestamp, s.TransactionIndex));

            return result;
        }

        public override decimal GetUnrealizedGainsLossesFromValuation(TimeArg time)
        {
            decimal result = 0;
            decimal count = 0;
            (decimal marketPrice, decimal amortizedPrice) previous = (0, 0);
            (decimal marketPrice, decimal amortizedPrice) current = (0, 0);

            foreach (var e in GetEventsUntil(time))
            {
                if (e is RecognitionEvent p)
                {
                    count = p.Count;
                    previous = (p.CleanPrice, p.CleanPrice);
                    current = (p.CleanPrice, p.CleanPrice);
                }
                if (e is DerecognitionEvent s)
                {
                    previous = current;
                    current = (s.CleanPrice, s.AmortizedCostCleanPrice);

                    result += Common.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) * count);
                    result -= GetRealizedGainsLossesFromValuation(e);

                    count -= s.Count;
                }
            }

            // End of period
            previous = current;
            current = (GetMarketPrice(time, true), GetAmortizedCostPrice(time, true));
            result += Common.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) * count);

            return result;
        }

        public override decimal GetRealizedGainsLossesFromFX(Event e)
        {
            throw new NotImplementedException();
        }

        public override decimal GetUnrealizedGainsLossesFromFX(TimeArg end)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
