using Budziszewski.Venture.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static Budziszewski.Financial.Calendar;

namespace Budziszewski.Venture.Assets
{
    public abstract class Asset
    {
        protected List<Events.Event> events = new List<Events.Event>();
        public IEnumerable<Events.Event> Events { get { return events.AsReadOnly(); } }

        public int Index { get; protected set; } = -1;

        /// <summary>
        /// Unique identifier of the asset. Based on identifier of the transaction that results in asset creation.
        /// Includes: date, ticker, transaction index - depending on the asset type.
        /// </summary>
        public string UniqueId { get; protected set; } = "";

        public string AssetType
        {
            get
            {
                return GetType().Name;
            }
        }

        /// <summary>
        /// Underlying security (where applicable).
        /// </summary>
        //public Data.Instrument? Security { get; protected set; } = null;

        /// <summary>
        /// Portfolio which the investment belongs to.
        /// </summary>
        public string Portfolio { get; protected set; } = "";

        /// <summary>
        /// In case of securities, custody account where the instrument is kept; otherwise empty.
        /// </summary>
        //public string CustodyAccount { get; protected set; } = "";

        /// <summary>
        /// For securities, cash account which receives potential flows; by default cash account from which the purchase was made.
        /// In case of cash instruments (e.g. deposit, cash, receivable), bank account where the cash resides or where it would flow at the maturity.
        /// </summary>
        public string CashAccount { get; protected set; } = "";

        public string CustodyAccount { get; protected set; } = "";

        /// <summary>
        /// Currency in which the investment is denominated.
        /// </summary>
        public string Currency { get; protected set; } = "PLN";

        /// <summary>
        /// Valuation class, if applicable.
        /// </summary>
        public ValuationClass ValuationClass { get; protected set; } = ValuationClass.AvailableForSale;

        public Asset(Data.Transaction tr)
        {
            Index = tr.Index;
            if (tr.TransactionType == Data.TransactionType.Cash)
            {

            }
            else if (tr.TransactionType == Data.TransactionType.Buy)
            {

            }
            else if (tr.TransactionType == Data.TransactionType.Sell)
            {

            }
            else throw new ArgumentException("An attempt to create a new Asset was made but transaction type is unknown.");
        }

        public Asset()
        {

        }

        /// <summary>
        /// Generates events resulting from cashflow occurences, i.e. redemptions, coupons, dividends, etc.
        /// </summary>
        protected abstract void GenerateFlows();

        public AssetsViewEntry GenerateAssetViewEntry(DateTime date)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            return new AssetsViewEntry()
            {
                UniqueId = this.UniqueId,
                AssetType = this.AssetType,
                Portfolio = this.Portfolio,
                CashAccount = this.CashAccount,
                CustodyAccount = this.CustodyAccount,
                Currency = this.Currency,
                ValuationClass = this.ValuationClass,
                Count = this.GetCount(time),
                NominalAmount = this.GetNominalAmount(time),
                AmortizedCostValue = this.GetAmortizedCostValue(time, true),
                MarketValue = this.GetMarketValue(time, true),
                AccruedInterest = this.GetAccruedInterest(date),
                Events = new System.Collections.ObjectModel.ObservableCollection<Events.Event>(this.Events)
            };
        }

        public void AddEvent(Events.Event e)
        {
            var index = events.FindIndex(x => x.Timestamp > e.Timestamp);
            events.Insert(index == -1 ? events.Count : index, e);
        }

        /// <summary>
        /// Returns whether the investment is active (is present in the books, has any value) at specific point of time.
        /// </summary>
        public bool IsActive(TimeArg time)
        {
            return GetCount(time) != 0;
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
        public DateTime? GetPurchaseDate()
        {
            return events.FirstOrDefault()?.Timestamp;
        }

        /// <summary>
        /// Returns maturity date of the investment.
        /// </summary>
        /// <returns>Maturity date of the investment or null if there is no maturity date (e.g. in case of equity instruments)</returns>
        public DateTime? GetMaturityDate()
        {
            return events.OfType<Events.Flow>().LastOrDefault(x => x.Type == Venture.Events.FlowType.Redemption)?.Timestamp;
        }

        /// <summary>
        /// Return date of the next coupon of the investment.
        /// </summary>
        /// <param name="date">The date starting from which the next coupon is counted.</param>
        /// <returns>Date of the next coupon or null if there is no next coupon date (e.g. in case of equity instruments)</returns>
        public DateTime? GetNextCouponDate(DateTime date)
        {
            return events.OfType<Events.Flow>().FirstOrDefault(x => x.Timestamp > date && x.Type == Venture.Events.FlowType.Coupon || x.Type == Venture.Events.FlowType.Redemption)?.Timestamp;
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
        public abstract double GetCouponRate(DateTime date);

        #region Price

        /// <summary>
        /// Gets purchase price of the investment. In case of single purchase, it is the price of this transaction. In case of multiple purchases (average cost expense method) it is an average price of purchase transactions, taking into account possible disinvestments.
        /// </summary>
        /// <param name="time">Time at which price is given (it matters for average cost expense method where there could be multiple purchases)</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Purchase price of the investment</returns>
        public abstract double GetPurchasePrice(TimeArg time, bool dirty);

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
        public abstract double GetMarketPrice(TimeArg time, bool dirty);

        /// <summary>
        /// Gets amortized cost price of the investment, i.e. purchase price modified if applicable by change of valuation resulting from passing of time, calculated using internal rate of return (effective rate).
        /// </summary>
        /// <param name="time">Time at which price is given</param>
        /// <param name="dirty">If true, dirty price (including interest) will be given</param>
        /// <returns>Amortized cost price of the investment</returns>
        public abstract double GetAmortizedCostPrice(TimeArg time, bool dirty);

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

        #endregion

    }
}
