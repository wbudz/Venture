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
            DateTime prevDate = Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Monthly);
            decimal currentTaxTotalResult = -Common.TaxBook.GetResult(date, null);
            decimal previousTaxTotalResult = date.Month == 1 ? 0 : -Common.TaxBook.GetResult(prevDate, null);

            decimal currentTotalTax = Common.Round(Math.Max(currentTaxTotalResult, 0) * 0.19m, 0);
            decimal previousTotalTax = Common.Round(Math.Max(previousTaxTotalResult, 0) * 0.19m, 0);

            Dictionary<PortfolioDefinition, (decimal result, decimal deduction, decimal tax)> currentTaxBreakdown = new();
            Dictionary<PortfolioDefinition, (decimal result, decimal deduction, decimal tax)> previousTaxBreakdown = new();

            foreach (var portfolio in Definitions.Portfolios)
            {
                var currentResult = Math.Max(-Common.TaxBook.GetResult(date, portfolio, ["91*"]), 0);
                var previousResult = date.Month == 1 ? 0 : Math.Max(-Common.TaxBook.GetResult(prevDate, portfolio, ["91*"]), 0);
                decimal currentTaxDeduction = 0;
                decimal previousTaxDeduction = 0;

                var manualAdjustment = Definitions.ManualEvents.OfType<IncomeTaxDeductionBookingEventDefinition>().SingleOrDefault(x => x.Timestamp.Year == date.Year && x.Portfolio == portfolio.UniqueId);
                if (manualAdjustment != null)
                {
                    currentTaxDeduction = Math.Min(currentResult, manualAdjustment.Amount);
                    previousTaxDeduction = date.Month == 1 ? 0 : Math.Min(previousResult, manualAdjustment.Amount);
                }

                currentTaxBreakdown.Add(portfolio, (currentResult, currentTaxDeduction, Common.Round((currentResult - currentTaxDeduction) * 0.19m, 0)));
                previousTaxBreakdown.Add(portfolio, (previousResult, previousTaxDeduction, Common.Round((previousResult - previousTaxDeduction) * 0.19m, 0)));
            }

            foreach (var portfolio in currentTaxBreakdown.OrderByDescending(x => x.Value))
            {
                var currentTax = Math.Min(currentTotalTax, currentTaxBreakdown[portfolio.Key].tax);
                currentTotalTax -= currentTax;
                var previousTax = Math.Min(previousTotalTax, previousTaxBreakdown[portfolio.Key].tax);
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


                var manualAdjustment = Definitions.ManualEvents.OfType<IncomeTaxDeductionBookingEventDefinition>().SingleOrDefault(x => x.Timestamp.Year == date.Year && x.Portfolio == portfolio.Key.UniqueId);
                if (manualAdjustment != null)
                {
                    var accountIncomeTaxDeduction = Common.TaxBook.GetAccount(AccountType.TaxDeduction, null, portfolio.Key, Common.LocalCurrency);
                    var accountPriorPeriodResult = Common.TaxBook.GetAccount(AccountType.PriorPeriodResult, null, portfolio.Key, Common.LocalCurrency);

                    decimal currentTaxDeduction = currentTaxBreakdown[portfolio.Key].deduction;
                    decimal previousTaxDeduction = previousTaxBreakdown[portfolio.Key].deduction;
                    Common.TaxBook.Enqueue(accountIncomeTaxDeduction, date, -1, manualAdjustment.Description, currentTaxDeduction - previousTaxDeduction);
                    Common.TaxBook.Enqueue(accountPriorPeriodResult, date, -1, manualAdjustment.Description, -(currentTaxDeduction - previousTaxDeduction));
                    Common.TaxBook.Commit();
                }

                if (date.Month == 12)
                {
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax (mid-year assessment derecognition)", -previousTax);
                    Common.MainBook.Enqueue(accountTaxReserves, date, -1, "Income tax (mid-year assessment derecognition)", previousTax);
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax for " + date.Year, currentTax);
                    Common.MainBook.Enqueue(accountTaxLiabilities, date, -1, "Income tax for " + date.Year, -currentTax);
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
