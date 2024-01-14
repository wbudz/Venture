using Venture.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Venture.Assets
{
    public class ETF : Security
    {
        public ETF(Data.Transaction tr, Data.Instrument definition) : base(tr, definition)
        {
            AddEvent(new Events.Recognition(this, tr, tr.Timestamp));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            foreach (var d in Data.Definitions.Dividends)
            {
                if (this.SecurityDefinition.InstrumentId == d.InstrumentId && d.RecordDate >= events.First().Timestamp)
                {
                    AddEvent(new Events.Flow(this, d.RecordDate, d.PaymentDate, Venture.Events.FlowType.Dividend, d.PaymentPerShare, 1));
                }
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

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            Data.Price? price = Data.Definitions.Prices.LastOrDefault(x => x.InstrumentId == this.SecurityDefinition.InstrumentId && x.Timestamp <= time.Date);
            if (price == null)
            {
                throw new Exception($"No price for: {UniqueId} at date: {time.Date:yyyy-MM-dd}.");
            }
            else
            {
                return price.Value;
            }
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
            return Math.Round(GetPurchaseAmount(time, false), 2);
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return Math.Round(GetPurchasePrice(time, dirty) * GetCount(time), 2);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetMarketPrice(time, dirty) * GetCount(time), 2);
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetAmortizedCostPrice(time, dirty) * GetCount(time), 2);
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

            foreach (var e in GetEvents(end))
            {
                if (e is Events.Flow f && f.FlowType == Venture.Events.FlowType.Dividend)
                {
                    income += Math.Round(f.Amount, 2);
                }
            }

            return income;
        }

        public override decimal GetRealizedGainsLossesFromValuation(Events.Event e)
        {
            if (!(e is Events.Derecognition))
            {
                throw new ArgumentException("GetRealizedGainsLossesFromValuation called for different event type than sale.");
            }

            Events.Derecognition s = (Events.Derecognition)e;

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

            foreach (var e in GetEvents(time))
            {
                if (e is Events.Recognition p)
                {
                    count = p.Count;
                    previous = (p.Price, p.Price);
                    current = (p.Price, p.Price);
                }
                if (e is Events.Derecognition s)
                {
                    previous = current;
                    current = (s.Price, GetAmortizedCostPrice(new TimeArg(TimeArgDirection.Start, s.Timestamp, s.TransactionIndex), true));

                    result += Math.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) * count, 2);
                    result -= GetRealizedGainsLossesFromValuation(e);

                    count -= s.Count;
                }
            }

            // End of period
            previous = current;
            current = (GetMarketPrice(time, true), GetAmortizedCostPrice(time, true));
            result += Math.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) * count, 2);

            return result;
        }

        public override decimal GetRealizedGainsLossesFromFX(Events.Event e)
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
