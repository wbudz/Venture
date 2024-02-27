using Venture.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Venture.Events;

namespace Venture.Assets
{
    public class Futures : Asset
    {
        public Data.Instrument SecurityDefinition { get; protected set; }

        public string AssociatedTicker { get; set; }

        public decimal Multiplier { get; set; }

        public DateTime MaturityDate { get; protected set; }

        public Futures(Data.Transaction tr, Data.Instrument definition)
        {
            UniqueId = $"{definition.AssetType}_{definition.AssetId}_{tr.Index}";
            AssetType = definition.AssetType;

            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountSrc;
            CustodyAccount = tr.AccountDst;
            Currency = tr.Currency;
            ValuationClass = tr.ValuationClass;

            SecurityDefinition = definition;

            AssociatedTicker = definition.Ticker;
            Multiplier = definition.UnitPrice;
            MaturityDate = definition.Maturity;

            AddEvent(new Events.Recognition(this, tr, tr.Timestamp));
            GenerateFlows();
        }
        public override void AddEvent(Events.Event e)
        {
            var index = events.FindIndex(x => x.Timestamp > e.Timestamp || (e.TransactionIndex > -1 && x.TransactionIndex > e.TransactionIndex));
            if (index == -1)
            { index = events.FindIndex(x => x.Timestamp == e.Timestamp && x is Flow f && f.FlowType == FlowType.FuturesSettlement); }
            if (index == -1)
            { index = events.Count; }
            events.Insert(index, e);

            // Adjust later events.
            if (index > -1 && e is Events.Derecognition dr)
            {
                for (int i = events.Count - 1; i > index; i--)
                {
                    events.RemoveAt(i);
                }
            }
            RecalculateBounds();
        }

        protected override void RecalculateBounds()
        {
            bounds.startDate = Events.First().Timestamp;
            bounds.startIndex = Events.First().TransactionIndex;
            foreach (Events.Event e in Events)
            {
                if (e is Events.Derecognition s)
                {
                    bounds.endDate = s.Timestamp;
                    bounds.endIndex = s.TransactionIndex;
                    return;
                }
            }
            bounds.endDate = MaturityDate < Common.FinalDate ? MaturityDate : Common.FinalDate.AddDays(1);
            bounds.endIndex = -1;
        }

        protected override void GenerateFlows()
        {
            // Happens at creation of futures, so there is only one Recognition event and MaturityDate.
            var prices = Definitions.Prices.Where(x => x.AssetId == AssociatedTicker);
            decimal previousPrice = GetPurchasePrice(false);
            decimal currentPrice = GetPurchasePrice(false);

            DateTime previousEnd = GetPurchaseDate();
            DateTime currentEnd = Financial.Calendar.GetEndDate(previousEnd, Financial.Calendar.TimeStep.Monthly);

            decimal count = GetCount();

            bool finished = false;

            while (!finished)
            {
                if (currentEnd >= MaturityDate)
                {
                    currentEnd = MaturityDate;
                    finished = true;
                }
                // Account for monthly valuations / maturity settlement.
                var price = prices.LastOrDefault(x => x.Timestamp <= currentEnd); // if no coupon is defined, use the last available
                if (price == null) throw new Exception($"No price defined for {AssociatedTicker} at {currentEnd:yyyy-MM-dd}.");
                currentPrice = price.Value;

                decimal amount = Common.Round((currentPrice - previousPrice) * count * Multiplier);
                AddEvent(new Events.Flow(this, currentEnd, currentEnd, Venture.Events.FlowType.FuturesSettlement, amount, Currency, FX.GetRate(currentEnd, Currency)));
                previousPrice = currentPrice;

                previousEnd = currentEnd;
                currentEnd = Financial.Calendar.GetEndDate(currentEnd.AddDays(1), Financial.Calendar.TimeStep.Monthly);
            }
        }

        public void RecalculateFlows()
        {
            var prices = Definitions.Prices.Where(x => x.AssetId == AssociatedTicker);
            decimal previousPrice = GetPurchasePrice(false);
            decimal currentPrice = GetPurchasePrice(false);
            decimal count = GetCount();

            foreach (var evt in Events.Skip(1))
            {
                if (evt is Events.Derecognition)
                {
                    continue;
                }
                if (evt is Events.Recognition r1)
                {
                    currentPrice = r1.CleanPrice;
                }
                if (evt is Events.Flow f)
                {
                    var price = prices.LastOrDefault(x => x.Timestamp <= f.Timestamp); // if no coupon is defined, use the last available
                    if (price == null) throw new Exception($"No price defined for {AssociatedTicker} at {f.Timestamp:yyyy-MM-dd}.");
                    currentPrice = price.Value;
                }

                decimal amount = Common.Round((currentPrice - previousPrice) * count * Multiplier);
                evt.Amount = amount;

                if (evt is Events.Recognition r2)
                {
                    count += r2.Count;
                }
                previousPrice = currentPrice;
            }
        }

        public override string ToString()
        {
            return $"Asset:Futures {UniqueId}";
        }

        public override decimal GetCount()
        {
            if (Events.FirstOrDefault() is Events.Recognition p) return p.Count;
            else throw new Exception("First event is not recognition.");
        }

        public override decimal GetCount(TimeArg time)
        {
            decimal count = 0;
            foreach (Events.Event e in GetEvents(time))
            {
                if (e is Events.Recognition p) count += p.Count;
                if (e is Events.Derecognition s) count = 0;
                if ((e is Events.Flow f) && f.FlowType == Venture.Events.FlowType.Redemption) count = 0;
            }
            return count;
        }

        public override decimal GetCouponRate(DateTime date)
        {
            return 0;
        }

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            //if (!IsActive(time)) return 0;

            //Data.Price? price = Data.Definitions.Prices.LastOrDefault(x => x.InstrumentId == AssociatedTicker && x.Timestamp <= time.Date);
            //if (price == null)
            //{
            //    throw new Exception($"No price for: {UniqueId} at date: {time.Date:yyyy-MM-dd}.");
            //}
            //else
            //{
            //    return price.Value;
            //}
            return 0;
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            //return GetPurchasePrice(time, dirty);
            return 0;
        }

        public override decimal GetAccruedInterest(DateTime date)
        {
            return 0;
        }

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            var events = GetEvents(time).OfType<Events.Recognition>();

            return events.Sum(x => x.Count * x.CleanPrice) / events.Sum(x => x.Count);
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
            return Common.Round(GetPurchasePrice(time, dirty) * GetCount(time) * Multiplier);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            //return Common.Round(GetMarketPrice(time, dirty) * GetCount(time));
            return 0;
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            //return Common.Round(GetAmortizedCostPrice(time, dirty) * GetCount(time));
            return 0;
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
                    income += Common.Round(f.Amount);
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
                    previous = (p.CleanPrice, p.CleanPrice);
                    current = (p.CleanPrice, p.CleanPrice);
                }
                if (e is Events.Derecognition s)
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

        public override decimal GetRealizedGainsLossesFromFX(Events.Event e)
        {
            throw new NotImplementedException();
        }

        public override decimal GetUnrealizedGainsLossesFromFX(TimeArg end)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override decimal GetUnrealizedPurchaseFee(TimeArg time)
        {
            // TODO: Come up with better formula.
            decimal fee = 0;

            foreach (Events.Event e in GetEvents(time))
            {
                if (e is Events.Recognition evt)
                {
                    fee += evt.Fee;
                }
                if (e is Events.Derecognition sale)
                {
                    return 0;
                }
            }
            return fee;
        }
    }
}
