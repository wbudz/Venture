using Venture.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Venture.Assets
{
    public abstract class Asset
    {
        protected List<Events.Event> events = new List<Events.Event>();
        public IEnumerable<Events.Event> Events { get { return events.AsReadOnly(); } }

        public int Index { get; protected set; } = -1;

        private string guid = Guid.NewGuid().ToString();
        /// <summary>
        /// Unique identifier of the asset. Based on identifier of the transaction that results in asset creation.
        /// Includes: date, ticker, transaction index - depending on the asset type.
        /// </summary>
        public string UniqueId
        {
            get
            {
                if (this is Security s)
                {
                    return $"{AssetType}_{GetPurchaseDate():yyyy-MM-dd}_{Index}_{Portfolio}_{Broker}_{s.SecurityDefinition.InstrumentId}_{GetNominalAmount()}_{guid}";
                }
                else
                {
                    return $"{AssetType}_{GetPurchaseDate():yyyy-MM-dd}_{Index}_{Portfolio}_{Broker}_{GetNominalAmount()}_{guid}";
                }
            }
        }

        public AssetType AssetType { get; protected set; } = AssetType.Undefined;

        /// <summary>
        /// Portfolio which the investment belongs to.
        /// </summary>
        public string Portfolio { get; protected set; } = "";

        /// <summary>
        /// For securities, cash account which receives potential flows; by default cash account from which the purchase was made.
        /// In case of cash instruments (e.g. deposit, cash, receivable), bank account where the cash resides or where it would flow at the maturity.
        /// </summary>
        public string CashAccount { get; protected set; } = "";

        /// <summary>
        /// In case of securities, custody account where the instrument is kept; otherwise empty.
        /// </summary>
        public string CustodyAccount { get; protected set; } = "";

        public string Broker
        {
            get
            {
                if (CustodyAccount.IndexOf(":") > 0) return CustodyAccount.Split(":")[1];
                if (CashAccount.IndexOf(":") > 0) return CashAccount.Split(":")[1];
                return "";
            }
        }

        /// <summary>
        /// Currency in which the investment is denominated.
        /// </summary>
        public string Currency { get; protected set; } = "PLN";

        /// <summary>
        /// Valuation class, if applicable.
        /// </summary>
        public ValuationClass ValuationClass { get; protected set; } = ValuationClass.AvailableForSale;

        public bool IsBond
        {
            get
            {
                return AssetType==AssetType.FixedCorporateBonds ||
                    AssetType==AssetType.FixedRetailTreasuryBonds ||
                    AssetType == AssetType.FixedTreasuryBonds ||
                    AssetType == AssetType.FloatingCorporateBonds ||
                    AssetType == AssetType.FloatingRetailTreasuryBonds ||
                    AssetType == AssetType.FloatingTreasuryBonds ||
                    AssetType == AssetType.IndexedRetailTreasuryBonds;
            }
        }

        protected (DateTime startDate, int startIndex, DateTime endDate, int endIndex) bounds;

        public DateTime BoundsStart { get { return bounds.startDate; } }

        public DateTime BoundsEnd { get { return bounds.endDate; } }

        public Asset(Data.Transaction tr)
        {
            Index = tr.Index;
        }

        public Asset()
        {

        }

        /// <summary>
        /// Generates events resulting from cashflow occurences, i.e. redemptions, coupons, dividends, etc.
        /// </summary>
        protected abstract void GenerateFlows();

        protected abstract void RecalculateBounds();

        public AssetsViewEntry GenerateAssetViewEntry(DateTime date)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            var events = this.GetEvents(time);
            return new AssetsViewEntry()
            {
                UniqueId = this.UniqueId,
                AssetType = this.AssetType.ToString(),
                Portfolio = this.Portfolio,
                CashAccount = this.CashAccount,
                CustodyAccount = this.CustodyAccount,
                Currency = this.Currency,
                ValuationClass = Common.ValuationClassToString(this.ValuationClass),
                InstrumentId = this is Security ? ((Security)this).SecurityDefinition.InstrumentId : "",
                RecognitionDate = this.GetPurchaseDate(),
                Count = this.GetCount(time),
                NominalAmount = this.GetNominalAmount(time),
                AmortizedCostValue = this.GetAmortizedCostValue(time, true),
                MarketValue = this.GetMarketValue(time, true),
                AccruedInterest = this.GetInterestAmount(time),
                BookValue = this.GetValue(time),
                Purchases = new(events.OfType<Events.Recognition>()),
                Sales = new(events.OfType<Events.Derecognition>()),
                Flows = new(events.OfType<Events.Flow>()),
                Payments = new(events.OfType<Events.Payment>())
            };
        }

        public void AddEvent(Events.Event e)
        {
            var index = events.FindIndex(x => x.Timestamp > e.Timestamp);
            events.Insert(index == -1 ? events.Count : index, e);

            if (e is Events.Derecognition)
            {
                for (int i = index + 1; i < events.Count; i++)
                {
                    if (events[i] is Events.Flow f) f.RecalculateAmount();
                }
            }

            RecalculateBounds();
        }

        /// <summary>
        /// Returns whether the investment is active (is present in the books, has any value) at specific point of time.
        /// </summary>
        public bool IsActive(TimeArg time)
        {
            if (time.Date < bounds.startDate) return false;
            if (time.Date > bounds.endDate) return false;
            if (time.Date > bounds.startDate && time.Date < bounds.endDate) return true;

            bool afterStart = time.Date > bounds.startDate;
            bool beforeEnd = time.Date < bounds.endDate;

            if (time.Date == bounds.startDate)
            {
                if (time.TransactionIndex == -1)
                {
                    if (time.Direction == TimeArgDirection.Start) afterStart = false;
                    if (time.Direction == TimeArgDirection.End) afterStart = true;
                }
                else
                {
                    if (time.TransactionIndex < bounds.startIndex) afterStart = false;
                    if (time.TransactionIndex > bounds.startIndex) afterStart = true;
                    if (time.TransactionIndex == bounds.startIndex)
                    {
                        if (time.Direction == TimeArgDirection.Start) afterStart = false;
                        if (time.Direction == TimeArgDirection.End) afterStart = true;
                    }
                }
            }

            if (time.Date == bounds.endDate)
            {
                if (time.TransactionIndex == -1)
                {
                    if (time.Direction == TimeArgDirection.Start) beforeEnd = true;
                    if (time.Direction == TimeArgDirection.End) beforeEnd = false;
                }
                else
                {
                    if (time.TransactionIndex < bounds.startIndex) beforeEnd = true;
                    if (time.TransactionIndex > bounds.startIndex) beforeEnd = false;
                    if (time.TransactionIndex == bounds.startIndex)
                    {
                        if (time.Direction == TimeArgDirection.Start) beforeEnd = true;
                        if (time.Direction == TimeArgDirection.End) beforeEnd = false;
                    }
                }
            }

            return afterStart && beforeEnd;
        }

        public bool IsActive(DateTime start, DateTime end)
        {
            if (start > end) throw new ArgumentException("Start time must be less than end time when checking if an asset is active.");

            if (bounds.startDate > end) return false;
            if (bounds.endDate < start) return false;
            if (bounds.startDate <= start && bounds.startDate <= end) return true;
            if (bounds.endDate >= start && bounds.endDate >= end) return true;

            return false;
        }

        public bool IsActive(DateTime date)
        {
            return IsActive(date, date);
        }

        public IEnumerable<Events.Event> GetEvents(TimeArg time)
        {
            foreach (var evt in events)
            {
                if (evt.Timestamp < time.Date) yield return evt;

                if (evt.Timestamp == time.Date)
                {
                    if (time.Direction == TimeArgDirection.Start && time.TransactionIndex < 0)
                    {
                        continue;
                    }
                    if (time.Direction == TimeArgDirection.Start && time.TransactionIndex >= 0)
                    {
                        if (evt.TransactionIndex < time.TransactionIndex) yield return evt;
                    }
                    if (time.Direction == TimeArgDirection.End && time.TransactionIndex >= 0)
                    {
                        if (evt.TransactionIndex <= time.TransactionIndex) yield return evt;
                    }
                    if (time.Direction == TimeArgDirection.End && time.TransactionIndex < 0)
                    {
                        yield return evt;
                    }
                }

                if (evt.Timestamp > time.Date) yield break;
            }
        }


        /// <summary>
        /// Returns purchase transaction date. In case of multiple purchases, it returns the date referring to the first transactions.
        /// </summary>
        /// <returns>Purchase transaction date</returns>
        public DateTime GetPurchaseDate()
        {
            return events.First().Timestamp;
        }

        /// <summary>
        /// Returns maturity date of the investment.
        /// </summary>
        /// <returns>Maturity date of the investment or null if there is no maturity date (e.g. in case of equity instruments)</returns>
        public DateTime? GetMaturityDate()
        {
            return events.OfType<Events.Flow>().LastOrDefault(x => x.FlowType == Venture.Events.FlowType.Redemption)?.Timestamp;
        }

        /// <summary>
        /// Return date of the next coupon of the investment.
        /// </summary>
        /// <param name="date">The date starting from which the next coupon is counted.</param>
        /// <returns>Date of the next coupon or null if there is no next coupon date (e.g. in case of equity instruments)</returns>
        public DateTime? GetNextCouponDate(DateTime date)
        {
            return events.OfType<Events.Flow>().FirstOrDefault(x => x.Timestamp > date && x.FlowType == Venture.Events.FlowType.Coupon || x.FlowType == Venture.Events.FlowType.Redemption)?.Timestamp;
        }

        /// <summary>
        /// Gets count (amount of units) of the investment at the specified time.
        /// </summary>
        /// <param name="time">Time at which count is given.</param>
        /// <returns>Count (amount of units) of the investment</returns>
        public abstract decimal GetCount(TimeArg time);

        /// <summary>
        /// Gets coupon rate of the coupon that falls on the specified date or the next nearest coupon.
        /// </summary>
        /// <param name="date">The date at which coupon rate is given, if no coupon falls on the given date, then the next coupon is given</param>
        /// <returns>Coupon rate</returns>
        public abstract decimal GetCouponRate(DateTime date);

        #region Price

        /// <summary>
        /// Gets purchase price of the investment. In case of single purchase, it is the price of this transaction. In case of multiple purchases (average cost expense method) it is an average price of purchase transactions, taking into account possible disinvestments.
        /// </summary>
        /// <param name="time">Time at which price is given (it matters for average cost expense method where there could be multiple purchases)</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Purchase price of the investment</returns>
        public abstract decimal GetPurchasePrice(TimeArg time, bool dirty);

        /// <summary>
        /// Gets market price of the investment, i.e. price taken from an active market or equivalent. If no market price is available at the given date:
        /// - if there is any earlier market valuation for the investment, it is taken;
        /// - if there is no market price but there is sufficient cash flow information, market price is estimated using current market rates;
        /// - otherwise, amortized cost price is taken.
        /// In case of HTM instruments amortized cost price is always returned.
        /// </summary>
        /// <param name="time">Time at which price is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Market price of the investment</returns>
        public abstract decimal GetMarketPrice(TimeArg time, bool dirty);

        /// <summary>
        /// Gets amortized cost price of the investment, i.e. purchase price modified if applicable by change of valuation resulting from passing of time, calculated using internal rate of return (effective rate).
        /// </summary>
        /// <param name="time">Time at which price is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Amortized cost price of the investment</returns>
        public abstract decimal GetAmortizedCostPrice(TimeArg time, bool dirty);

        #endregion

        #region Asset amount

        /// <summary>
        /// Gets nominal amount of the investment. It is:
        /// - notional (face) amount of fixed income securities;
        /// - purchase amount in case of equity instruments;
        /// - initial amount in case of deposits;
        /// - actual amount in case of other assets and liabilities.
        /// </summary>
        /// <param name="time">Time at which amount is given</param>
        /// <returns>Nominal amount of the investment</returns>
        public abstract decimal GetNominalAmount(TimeArg time);

        public abstract decimal GetNominalAmount();

        /// <summary>
        /// Gets amount of accrued interest of the investment.
        /// </summary>
        /// <param name="time">Time at which amount is given</param>
        /// <returns>Interest amount of the investment</returns>
        public abstract decimal GetInterestAmount(TimeArg time);

        /// <summary>
        /// Gets purchase amount of the investment.
        /// </summary>
        /// <param name="time">Time at which amount is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Purchase amount of the investment</returns>
        public abstract decimal GetPurchaseAmount(TimeArg time, bool dirty);

        /// <summary>
        /// Gets market value of the investment.
        /// </summary>
        /// <param name="time">Time at which amount is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Market value of the investment</returns>
        public abstract decimal GetMarketValue(TimeArg time, bool dirty);

        /// <summary>
        /// Gets amortized cost value of the investment.
        /// </summary>
        /// <param name="time">Time at which amount is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Amortized cost value of the investment</returns>
        public abstract decimal GetAmortizedCostValue(TimeArg time, bool dirty);

        /// <summary>
        /// Gets amount of accrued interest of the investment (for fixed income, interest component of dirty price, per 100), if applicable.
        /// </summary>
        /// <param name="date">Date at which interest is given</param>
        /// <returns>Amount of accrued interest.</returns>
        public abstract decimal GetAccruedInterest(DateTime date);

        public decimal GetValue(TimeArg time)
        {
            switch (ValuationClass)
            {
                case ValuationClass.Undefined: return GetAmortizedCostValue(time, true);
                case ValuationClass.Trading: return GetMarketValue(time, true);
                case ValuationClass.AvailableForSale: return GetMarketValue(time, true);
                case ValuationClass.HeldToMaturity: return GetAmortizedCostValue(time, true);
                default: return GetAmortizedCostValue(time, true);
            }
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Gets tenor, i.e. amount of years (taking into account day count convention) that remains until maturity.
        /// </summary>
        /// <param name="date">Date at which tenor is given</param>
        /// <returns>Returns tenor of the investment or 0 if there is no tenor (no specific maturity date).</returns>
        public abstract double GetTenor(DateTime date);

        /// <summary>
        /// Gets modified duration, i.e. sensitivity of value to change in market rates for fixed income instruments.
        /// </summary>
        /// <param name="date">Date at which modified duration is given</param>
        /// <returns>Returns tenor of the investment or 0 if there is no modified duration (instrument not applicable).</returns>
        public abstract double GetModifiedDuration(DateTime date);

        /// <summary>
        /// Gets yield to maturity of the investment, i.e. income rate per annum it would generate until maturity.
        /// </summary>
        /// <param name="date">Date at which yield to maturity is given</param>
        /// <param name="price">Current clean price of the investment at the given date, acquired e.g. by specific function to get market value or amortized cost value. </param>
        /// <returns>Returns yield to maturity of the investment or 0 if there is no modified duration (instrument not applicable).</returns>
        public abstract double GetYieldToMaturity(DateTime date, double price);

        #endregion

        /// <summary>
        /// Gets amount of purchase fees for the amount of investment that still has not been sold (redeemed) - for tax calculations.
        /// </summary>
        /// <param name="time">Time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed (usually needed in case of this measure)</param>
        /// <returns>Purchase fee accountable to unsold amount</returns>
        public abstract decimal GetUnrealizedPurchaseFee(TimeArg time);

        #region Income

        /// <summary>
        /// Gets income resulting from passing of time and resulting revaluation of cashflows (change of amortized cost value).
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <returns>Time value of money income</returns>
        public abstract decimal GetTimeValueOfMoneyIncome(TimeArg time);

        /// <summary>
        /// Gets income resulting from incoming cashflows, e.g. redemptions, coupons, dividends.
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <returns>Cash flow income</returns>
        public abstract decimal GetCashflowIncome(TimeArg time);

        /// <summary>
        /// Gets income resulting from sale of instrument and recognition of previous market valuation of the investment, i.e. difference between market valuation (market prices) and amortized cost valuation, as realized gain or loss.
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <param name="yearly">If true, unrealized gains/losses from the current year will be derecognized, otherwise from the point of initial recognition</param>
        /// <returns>Market valuation gain or loss</returns>
        public abstract decimal GetRealizedGainsLossesFromValuation(Events.Event e);

        /// <summary>
        /// Gets income resulting from market valuation of the investment, i.e. difference between market valuation (market prices) and amortized cost valuation. Resulting difference may be booked, depending on accounting environment, in profit and loss statement or as other comprehensive income (capital).
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <param name="yearly">If true, unrealized gains/losses from the current year will be derecognized, otherwise from the point of initial recognition</param>
        /// <param name="net">If true, the amount of unrealized gains/losses subtracted due to disinvestments (and recognition as realized gains/losses) would be deducted, otherwise only gross increase would be shown.</param>
        /// <returns>Market valuation gain or loss</returns>
        public abstract decimal GetUnrealizedGainsLossesFromValuation(TimeArg time);

        /// <summary>
        /// Gets income resulting from sale of instrument and recognition of previous FX effects of the investment, as realized FX differences.
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <param name="yearly">If true, unrealized gains/losses from the current year will be derecognized, otherwise from the point of initial recognition</param>
        /// <returns>FX gain or loss</returns>
        public abstract decimal GetRealizedGainsLossesFromFX(Events.Event e);

        /// <summary>
        /// Gets income resulting from FX effects of the investment.
        /// </summary>
        /// <param name="start">Starting time of the calculation (exclusive)</param>
        /// <param name="end">Ending time of the calculation (inclusive)</param>
        /// <param name="local">If true, amount will be calculated in the local currency</param>
        /// <param name="tax">If true, rules regarding tax calculation will be applied, if needed</param>
        /// <param name="yearly">If true, unrealized gains/losses from the current year will be derecognized, otherwise from the point of initial recognition</param>
        /// <returns>FX gain or loss</returns>
        public abstract decimal GetUnrealizedGainsLossesFromFX(TimeArg time);

        #endregion

    }
}
