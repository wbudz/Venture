using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class Booking
    {
        public static PortfolioDefinition GetPortfolio(IEnumerable<Event> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            PortfolioDefinition? output = events.First().ParentAsset.Portfolio;
            if (output == null) throw new Exception("Null portfolio definition.");
            if (!(events.All(x => x.ParentAsset.Portfolio == output))) throw new Exception("Tried to create account entries with different portfolio values.");
            return output;
        }

        public static string GetCurrency(IEnumerable<Event> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            string output = events.First().Currency;
            if (!(events.All(x => x.Currency == output))) throw new Exception("Tried to create account entries with different currency values.");
            return output;
        }

        public static decimal GetAmount(IEnumerable<Event> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            return events.Sum(x=>x.Amount);
        }

        public static DateTime GetDate(IEnumerable<Event> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            DateTime output = events.First().Timestamp;
            if (!(events.All(x => x.Timestamp == output))) throw new Exception("Tried to create account entries with different timestamp values.");
            return output;
        }

        public static int GetTransactionIndex(IEnumerable<Event> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            int output = events.First().TransactionIndex;
            if (!(events.All(x => x.TransactionIndex == output))) throw new Exception("Tried to create account entries with different transaction index values.");
            return output;
        }

        public static PaymentType GetPaymentType(IEnumerable<PaymentEvent> events)
        {
            if (events.Count() == 0) throw new Exception("No source events when trying to create account entries.");
            PaymentType output = events.First().PaymentType;
            if (!(events.All(x => x.PaymentType == output))) throw new Exception("Tried to create account entries with different payment type values.");
            return output;
        }
    }
}
