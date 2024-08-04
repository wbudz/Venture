using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class EndOfYearBooking
    {
        public static void Process(PortfolioDefinition? portfolio, string currency, DateTime date)
        {
            foreach (var book in Common.Books)
            {
                /// <summary>
                /// Asset account where change in market valuation will be recognized
                /// </summary>
                var accountPriorPeriodResult = book.GetAccount(AccountType.PriorPeriodResult, null, portfolio, currency);

                decimal totalResult = 0;

                foreach (var a in book.GetResultAccounts(portfolio, currency))
                {
                    decimal currentResult = a.GetNetAmount(date);
                    totalResult += currentResult;
                    book.Enqueue(a, date, -1, "End of year book closing", -currentResult);
                }
                book.Enqueue(accountPriorPeriodResult, date, -1, "End of year book closing", totalResult);
                book.Commit();
            }
        }
    }
}
