using Venture.Data;
using Venture.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Venture.Assets
{
    public class Bond : Security
    {
        public decimal UnitPrice { get; protected set; }

        public decimal CouponRate { get; protected set; }

        public int CouponFreq { get; protected set; }

        public CouponType CouponType { get; protected set; }

        public DateTime MaturityDate { get; protected set; }

        private List<(DateTime startDate, double ytm)> YieldsToMaturity = new List<(DateTime startDate, double ytm)>();

        public Bond(Data.Transaction tr, Data.Instrument definition) : base(tr, definition)
        {
            UnitPrice = tr.NominalAmount;
            CouponRate = definition.CouponRate;
            CouponFreq = definition.CouponFreq;
            CouponType = definition.CouponType;
            MaturityDate = definition.Maturity;
            AddEvent(new Events.Recognition(this, tr, tr.Timestamp));
            GenerateFlows();
            GenerateYields();
        }

        protected override void GenerateFlows()
        {
            DateTime start = GetPurchaseDate();
            DateTime end = MaturityDate;
            decimal redemption = 1;

            // Check for premature redemption
            var manual = Data.Definitions.GetManualAdjustment(ManualAdjustmentType.PrematureRedemption, SecurityDefinition.UniqueId);
            if (manual != null)
            {
                end = manual.Timestamp;
                redemption = manual.Amount1 / 100;
            }

            if (start > end) throw new Exception("Bond purchased after maturity date.");

            int monthStep = 12 / CouponFreq;
            DateTime date = MaturityDate;

            // Coupons
            if (CouponType == CouponType.Fixed)
            {
                if (CouponRate > 0)
                {
                    while (date >= start)
                    {
                        if (date <= end)
                        {
                            AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(date, -2), date, Venture.Events.FlowType.Coupon, CouponRate / CouponFreq, FX.GetRate(date, Currency)));
                        }
                        date = ShiftDate(date, monthStep);
                    }
                }
            }
            else
            {
                var coupons = Definitions.Coupons.Where(x => x.InstrumentId == this.InstrumentId);

                while (date >= start)
                {
                    var coupon = coupons.LastOrDefault(x => x.Timestamp <= date); // if no coupon is defined, use the last available
                    if (coupon == null) throw new Exception($"No coupon rate defined for {SecurityDefinition.InstrumentId} at {date:yyyy-MM-dd}.");

                    if (date <= end)
                    {
                        AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(date, -2), date, Venture.Events.FlowType.Coupon, coupon.CouponRate / CouponFreq, FX.GetRate(date, Currency)));
                    }
                    date = ShiftDate(date, monthStep);
                }
            }

            // Redemption
            AddEvent(new Events.Flow(this, Financial.Calendar.WorkingDays(end, -2), end, Venture.Events.FlowType.Redemption, redemption, FX.GetRate(end, Currency)));
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

        private void GenerateYields()
        {
            YieldsToMaturity.Clear();
            // Purchase YTM
            DateTime date = GetPurchaseDate();
            double price = (double)GetPurchasePrice(false);
            double couponRate = (double)GetCouponRate(date);
            double ytm = Financial.FixedIncome.Yield(date, MaturityDate, couponRate, price, 100, CouponFreq, SecurityDefinition.DayCountConvention);
            YieldsToMaturity.Add((date, ytm));
            DateTime prevDate = date;
            double prevYtm = ytm;
            double prevCouponRate = couponRate;

            // For fixed-coupon securities, one ytm for the whole period
            if (this.AssetType == AssetType.FixedCorporateBonds || this.AssetType == AssetType.FixedTreasuryBonds || this.AssetType == AssetType.FixedRetailTreasuryBonds) return;

            // For floaters, coupon-based yields
            bool firstCoupon = true; // we skip first coupon, because we will save second coupon's calculation on first coupon's date, and so on
            foreach (var fl in Events.OfType<Flow>().Where(x => x.FlowType == FlowType.Coupon && x.Timestamp > date).OrderBy(x => x.Timestamp))
            {
                if (!firstCoupon)
                {
                    date = fl.Timestamp;
                    couponRate = (double)GetCouponRate(date);
                    price = Financial.FixedIncome.Price(prevDate, MaturityDate, prevCouponRate, prevYtm, 100, CouponFreq, SecurityDefinition.DayCountConvention);
                    ytm = Financial.FixedIncome.Yield(prevDate, MaturityDate, couponRate, price, 100, CouponFreq, SecurityDefinition.DayCountConvention);
                    YieldsToMaturity.Add((prevDate, ytm));
                }
                prevDate = fl.Timestamp;
                prevYtm = ytm;
                prevCouponRate = couponRate;
                firstCoupon = false;
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

            try
            {
                // Derive from the next flow
                var nextFlow = Events.OfType<Events.Flow>().Where(x=>x.FlowType == FlowType.Coupon).FirstOrDefault(x => x.Timestamp >= date);
                if (nextFlow != null)
                {
                    return nextFlow.Rate * CouponFreq;
                }

                // If no next flow is defined (e.g. in case of impending sale), try defined coupons.
                if (SecurityDefinition.CouponType == CouponType.Fixed)
                {
                    return SecurityDefinition.CouponRate;
                }
                else if (SecurityDefinition.CouponType == CouponType.Floating)
                {
                    var coupons = Definitions.Coupons.Where(x => x.InstrumentId == this.InstrumentId);
                    var coupon = coupons.FirstOrDefault(x => x.Timestamp >= date) ?? coupons.Last(x => x.Timestamp < date);
                    return coupon.CouponRate;
                }
                else throw new Exception("Unexpected coupon type.");
            }
            catch
            {
                throw new Exception($"No coupon rate for: {this} at date: {date:yyyy-MM-dd}.");
            }
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

            double yield = GetYieldToMaturity(time.Date);
            double rate = (double)GetCouponRate(time.Date);

            if (dirty)
            {
                return (decimal)Financial.FixedIncome.DirtyPrice(time.Date, MaturityDate, rate, yield, 100, CouponFreq, SecurityDefinition.DayCountConvention);
            }
            else
            {
                return (decimal)Financial.FixedIncome.Price(time.Date, MaturityDate, rate, yield, 100, CouponFreq, SecurityDefinition.DayCountConvention);
            }
        }

        public override decimal GetAccruedInterest(DateTime date)
        {
            if (!IsActive(date)) return 0;

            return (decimal)Financial.FixedIncome.Interest(date, MaturityDate, (double)GetCouponRate(date), CouponFreq, SecurityDefinition.DayCountConvention);

        }

        public override decimal GetNominalAmount(TimeArg time)
        {
            return Common.Round(GetCount(time) * UnitPrice);
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return Common.Round(GetAccruedInterest(time.Date) / 100 * GetNominalAmount(time));
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return Common.Round(GetPurchasePrice(time, dirty) / 100 * GetNominalAmount(time));
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Common.Round(GetMarketPrice(time, dirty) / 100 * GetNominalAmount(time));
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Common.Round(GetAmortizedCostPrice(time, dirty) / 100 * GetNominalAmount(time));
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

        public override double GetYieldToMaturity(DateTime date)
        {
            if (!IsActive(date)) return 0;
            return YieldsToMaturity.Last(x => x.startDate < date).ytm;
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
                    result += Common.Round((currentPrice - previousPrice) * count / 100 * UnitPrice);
                    previousPrice = currentPrice;
                    count -= s.Count;
                }
                if (e is Events.Flow f && f.FlowType == FlowType.Redemption)
                {
                    currentPrice = 100;
                    result += Common.Round((currentPrice - previousPrice) * count / 100 * UnitPrice);
                    return result;
                }
            }

            // End of period
            currentPrice = GetAmortizedCostPrice(new TimeArg(TimeArgDirection.End, end.Date), true);
            result += Common.Round((currentPrice - previousPrice) * count / 100 * UnitPrice);
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
                    previous = (p.CleanPrice, p.CleanPrice);
                    current = (p.CleanPrice, p.CleanPrice);
                }
                if (e is Events.Derecognition s)
                {
                    previous = current;
                    current = (s.CleanPrice, s.AmortizedCostCleanPrice);

                    result += Common.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) / 100 * UnitPrice * count);
                    result -= GetRealizedGainsLossesFromValuation(e);

                    count -= s.Count;
                }
                if (e is Events.Flow f && f.FlowType == FlowType.Redemption)
                {
                    result -= Common.Round((previous.marketPrice - previous.amortizedPrice) / 100 * UnitPrice * count);
                    return result;
                }
            }

            // End of period
            previous = current;
            current = (GetMarketPrice(time, true), GetAmortizedCostPrice(time, true));
            result += Common.Round((current.marketPrice - current.amortizedPrice - (previous.marketPrice - previous.amortizedPrice)) / 100 * UnitPrice * count);

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
