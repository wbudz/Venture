using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class AccountEntry
    {
        public string UniqueId { get { return $"{Account.UniqueId}_{OperationIndex}"; } }

        public Account Account { get; private set; }

        /// <summary>
        /// Date of the event that is represented by the book entry.
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Unique indentifier of an investment that is the subject of this booking.
        /// </summary>
        public long OperationIndex { get; set; }

        /// <summary>
        /// Unique indentifier of a transaction that is the subject of this booking.
        /// </summary>
        public long TransactionIndex { get; private set; }

        /// <summary>
        /// Description of the booking
        /// </summary>
        public string Description { get; private set; } = "";

        /// <summary>
        /// Amount in domestic currency. Debit bookings are given as positive values, credit bookings are given as negative values.
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Creates a new booking.
        /// </summary>
        public AccountEntry(Account account, DateTime date, long transactionIndex, string description, decimal amount)
        {
            Account = account;
            Date = date;
            TransactionIndex = transactionIndex;
            Description = description;
            Amount = Common.Round(amount);
        }
    }
}
