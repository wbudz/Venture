using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture
{
    public static class TaxIncomeBooking
    {
        public static void Process(DateTime date)
        {
            if (date.Month == 1)
            {
                var manualAdjustment = Definitions.ManualEvents.OfType<IncomeTaxDeductionBookingEventDefinition>().SingleOrDefault(x => x.Timestamp.Year == date.Year);

                if (manualAdjustment != null)
                {
                    var portfolio = Definitions.Portfolios.Single(x => x.UniqueId == manualAdjustment.Portfolio);

                    var accountIncomeTaxInTaxBook = Common.TaxBook.GetAccount(AccountType.Tax, null, portfolio, Common.LocalCurrency);
                    var accountPriorPeriodResult = Common.TaxBook.GetAccount(AccountType.PriorPeriodResult, null, portfolio, Common.LocalCurrency);

                    Common.TaxBook.Enqueue(accountIncomeTaxInTaxBook, date, -1, manualAdjustment.Description, manualAdjustment.Amount);
                    Common.TaxBook.Enqueue(accountPriorPeriodResult, date, -1, manualAdjustment.Description, -manualAdjustment.Amount);
                    Common.TaxBook.Commit();
                }
            }

            DateTime prevDate = Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Monthly);
            decimal currentTaxTotalResult = -Common.TaxBook.GetResult(date, null);
            decimal previousTaxTotalResult = date.Month == 1 ? 0 : -Common.TaxBook.GetResult(prevDate, null);

            decimal currentTotalTax = Common.Round(Math.Max(currentTaxTotalResult, 0) * 0.19m, 0);
            decimal previousTotalTax = Common.Round(Math.Max(previousTaxTotalResult, 0) * 0.19m, 0);

            Dictionary<PortfolioDefinition, decimal> currentTaxBreakdown = new Dictionary<PortfolioDefinition, decimal>();
            Dictionary<PortfolioDefinition, decimal> previousTaxBreakdown = new Dictionary<PortfolioDefinition, decimal>();

            foreach (var portfolio in Definitions.Portfolios)
            {
                currentTaxBreakdown.Add(portfolio, Common.Round(Math.Max(-Common.TaxBook.GetResult(date, portfolio), 0) * 0.19m, 0));
                if (date.Month == 1)
                {
                    previousTaxBreakdown.Add(portfolio, 0);
                }
                else
                {
                    previousTaxBreakdown.Add(portfolio, Common.Round(Math.Max(-Common.TaxBook.GetResult(prevDate, portfolio), 0) * 0.19m, 0));
                }
            }

            foreach (var portfolio in currentTaxBreakdown.OrderByDescending(x=>x.Value))
            {
                var currentTax = Math.Min(currentTotalTax, currentTaxBreakdown[portfolio.Key]);
                currentTotalTax -= currentTax;
                var previousTax = Math.Min(previousTotalTax, previousTaxBreakdown[portfolio.Key]);
                previousTotalTax -= previousTax;

                /// <summary>
                /// Asset where accrued expense resulting from tax calculated but not yet charged is booked.
                /// </summary>
                var accountTaxReserves = Common.MainBook.GetAccount(AccountType.TaxReserves, null, portfolio.Key, Common.LocalCurrency);

                /// <summary>
                /// Account where liabilities resulting from tax charged is booked.
                /// </summary>
                var accountTaxLiabilities = Common.MainBook.GetAccount(AccountType.TaxLiabilities, null, portfolio.Key, Common.LocalCurrency);

                /// <summary>
                /// Account where tax that will be deducted and paid from current year's result is booked.
                /// </summary>
                var accountIncomeTax = Common.MainBook.GetAccount(AccountType.Tax, null, portfolio.Key, Common.LocalCurrency);

                if (date.Month == 12)
                {
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax (mid-year assessment derecognition)", -previousTax);
                    Common.MainBook.Enqueue(accountTaxReserves, date, -1, "Income tax (mid-year assessment derecognition)", previousTax);
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax for " + date.Year, currentTax);
                    Common.MainBook.Enqueue(accountTaxLiabilities, date, -1, "Income tax for" + date.Year, -currentTax);
                }
                else
                {
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax (mid-year assessment)", currentTax - previousTax);
                    Common.MainBook.Enqueue(accountTaxReserves, date, -1, "Income tax (mid-year assessment)", -(currentTax - previousTax));
                }
            }

            Common.MainBook.Commit();
        }
    }
}
