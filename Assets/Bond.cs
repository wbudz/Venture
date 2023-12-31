﻿using Venture.Data;
using Venture.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Venture.Assets
{
    public class Bond : Security
    {
        public decimal UnitPrice { get; protected set; }

        public decimal CouponRate { get; protected set; }

        public int CouponFreq { get; protected set; }

        public CouponType CouponType { get; protected set; }

        public DateTime MaturityDate { get; protected set; }

        public Bond(Data.Transaction tr, Data.Instrument definition) : base(tr, definition)
        {
            UnitPrice = tr.NominalAmount;
            CouponRate = definition.CouponRate;
            CouponFreq = definition.CouponFreq;
            CouponType = definition.CouponType;
            MaturityDate = definition.Maturity;
            AddEvent(new Events.Recognition(this, tr, definition.RecognitionOnTradeDate ? tr.TradeDate : tr.SettlementDate));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            DateTime? start = events.OfType<Events.Recognition>().FirstOrDefault()?.Timestamp;
            DateTime? end = MaturityDate;

            if (start == null) return;

            int monthStep = 12 / CouponFreq;
            DateTime date = end.Value;

            if (date >= start)
            {
                AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(date, -2), date, Venture.Events.FlowType.Redemption, 1, FX.GetRate(date, Currency)));
            }

            if (CouponType == CouponType.Fixed)
            {
                if (CouponRate > 0)
                {
                    while (date >= start)
                    {
                        AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(date, -2), date, Venture.Events.FlowType.Coupon, CouponRate / CouponFreq, FX.GetRate(date, Currency)));
                        date = ShiftDate(date, monthStep);
                    }
                }
            }
            else
            {
                var coupons = Definitions.Coupons.Where(x => x.InstrumentId == this.SecurityDefinition.InstrumentId);

                while (date >= start)
                {
                    var coupon = coupons.SingleOrDefault(x => x.Timestamp == date);
                    if (coupon == null) throw new Exception($"No coupon rate defined for {SecurityDefinition.InstrumentId} at {date:yyyy-MM-dd}.");

                    AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(date, -2), date, Venture.Events.FlowType.Coupon, coupon.CouponRate / CouponFreq, FX.GetRate(date, Currency)));
                    date = ShiftDate(date, monthStep);
                }
            }
        }

        private DateTime ShiftDate(DateTime date, int monthStep)
        {
            if (SecurityDefinition.EndOfMonthConvention == EndOfMonthConvention.Align && date.Day == DateTime.DaysInMonth(date.Year, date.Month))
            {
                date = date.AddMonths(-monthStep);
                return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            }
            else
            {
                return date.AddMonths(-monthStep);
            }
        }

        public override string ToString()
        {
            return $"Asset:Bond {UniqueId}";
        }

        public override decimal GetCouponRate(DateTime date)
        {
            if (!IsActive(date)) return 0;
            if (date > MaturityDate) return 0;

            return Events.OfType<Events.Flow>().First(x => x.Timestamp >= date).Rate * CouponFreq;
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
                return price.Value + (dirty ? GetAccruedInterest(time.Date) : 0);
            }
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            double price = (double)GetPurchasePrice(time, false);
            double yield = GetYieldToMaturity(GetPurchaseDate(), price);

            if (dirty)
            {
                return (decimal)Financial.FixedIncome.DirtyPrice(time.Date, MaturityDate, (double)GetCouponRate(time.Date), yield, 100, CouponFreq, SecurityDefinition.DayCountConvention);
            }
            else
            {
                return (decimal)Financial.FixedIncome.Price(time.Date, MaturityDate, (double)GetCouponRate(time.Date), yield, 100, CouponFreq, SecurityDefinition.DayCountConvention);
            }
        }

        public override decimal GetAccruedInterest(DateTime date)
        {
            if (!IsActive(date)) return 0;

            //System.Windows.MessageBox.Show(Financial.FixedIncome.Interest(new DateTime(2016, 11, 22), MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention).ToString());
            //System.Windows.MessageBox.Show(Financial.FixedIncome.Interest(new DateTime(2016, 11, 30), MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention).ToString());
            //System.Windows.MessageBox.Show(Financial.FixedIncome.Interest(new DateTime(2016, 12, 30), MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention).ToString());
            //System.Windows.MessageBox.Show(Financial.FixedIncome.Interest(new DateTime(2016, 12, 31), MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention).ToString());

            return (decimal)Financial.FixedIncome.Interest(date, MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention);

        }

        public override decimal GetNominalAmount(TimeArg time)
        {
            return Math.Round(GetCount(time) * UnitPrice, 2);
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return Math.Round(GetAccruedInterest(time.Date) / 100 * GetNominalAmount(time), 2);
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return Math.Round(GetPurchasePrice(time, dirty) / 100 * GetNominalAmount(time), 2);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetMarketPrice(time, dirty) / 100 * GetNominalAmount(time), 2);
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetAmortizedCostPrice(time, dirty) / 100 * GetNominalAmount(time), 2);
        }

        #region Parameters

        public override double GetTenor(DateTime date)
        {
            if (!IsActive(date)) return 0;
            return Financial.DayCount.GetTenor(date, MaturityDate, CouponFreq, SecurityDefinition.DayCountConvention);
        }

        public override double GetModifiedDuration(DateTime date)
        {
            if (!IsActive(date)) return 0;
            return Financial.FixedIncome.MDuration(date, MaturityDate, (double)GetCouponRate(date), GetYieldToMaturity(date, (double)GetMarketPrice(new TimeArg(TimeArgDirection.End, date), false)), 100, CouponFreq, SecurityDefinition.DayCountConvention);
        }

        public override double GetYieldToMaturity(DateTime date, double price)
        {
            if (!IsActive(date)) return 0;
            return Financial.FixedIncome.Yield(date, MaturityDate, (double)GetCouponRate(date), price, 100, CouponFreq, SecurityDefinition.DayCountConvention);
        }

        #endregion

        #region Income

        public override decimal GetTimeValueOfMoneyIncome(TimeArg end)
        {
            decimal result = 0;
            decimal count = 0;
            decimal previousPrice = 0;
            decimal currentPrice = 0;

            foreach (var e in GetEvents(end))
            {
                if (e is Events.Recognition p)
                {
                    count = p.Count;
                    currentPrice = GetAmortizedCostPrice(new TimeArg(TimeArgDirection.End, p.Timestamp, p.TransactionIndex), true);
                    previousPrice = currentPrice;
                }
                if (e is Events.Derecognition s)
                {
                    currentPrice = GetAmortizedCostPrice(new TimeArg(TimeArgDirection.End, s.Timestamp, s.TransactionIndex), true);
                    result += Math.Round((currentPrice - previousPrice) * count / 100 * UnitPrice, 2);
                    previousPrice = currentPrice;
                    count -= s.Count;
                }
                if (e is Events.Flow f && f.FlowType == FlowType.Redemption)
                {
                    currentPrice = 100;
                    result += Math.Round((currentPrice - previousPrice) * count / 100 * UnitPrice, 2);
                    return result;
                }
            }

            // End of period
            currentPrice = GetAmortizedCostPrice(new TimeArg(TimeArgDirection.End, end.Date), true);
            result += Math.Round((currentPrice - previousPrice) * count / 100 * UnitPrice, 2);
            return result;
        }

        public override decimal GetCashflowIncome(TimeArg end)
        {
            decimal income = 0;

            foreach (var e in GetEvents(end))
            {
                if (e is Events.Flow f)
                {
                    if (f.FlowType == FlowType.Redemption)
                    {
                        return income;
                    }
                    else if (f.FlowType == FlowType.Coupon)
                    {
                        income += f.Amount;
                    }
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

                    result += Math.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) / 100 * UnitPrice * count, 2);
                    result -= GetRealizedGainsLossesFromValuation(e);

                    count -= s.Count;
                }
                if (e is Events.Flow f && f.FlowType == FlowType.Redemption)
                {
                    result -= Math.Round((previous.marketPrice - previous.amortizedPrice) / 100 * UnitPrice * count, 2);
                    return result;
                }
            }

            // End of period
            previous = current;
            current = (GetMarketPrice(time, true), GetAmortizedCostPrice(time, true));
            result += Math.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) / 100 * UnitPrice * count, 2);

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
