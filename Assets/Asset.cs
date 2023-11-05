using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Budziszewski.Financial.Calendar;

namespace Budziszewski.Venture.Assets
{
    public abstract class Asset
    {
        public List<Events.Event> Events { get; private set; } = new List<Events.Event>();

        /// <summary>
        /// Unique identifier of the asset. Based on identifier of the transaction that results in asset creation.
        /// Includes: date, ticker, transaction index - depending on the asset type.
        /// </summary>
        public string UniqueId { get; protected set; } = "";

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

        public abstract Modules.AssetsViewEntry GenerateAssetViewEntry();
    }
}
