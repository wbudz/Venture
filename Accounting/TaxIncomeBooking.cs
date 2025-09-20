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
            (decimal result, decimal deduction, decimal tax) total;
            string deductionDescription = "";

            DateTime prevDate = Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Monthly);
            total = (-Common.TaxBook.GetTaxableResult(date, null), 0, 0);

            var manualAdjustment = Definitions.ManualEvents.OfType<IncomeTaxDeductionBookingEventDefinition>().SingleOrDefault(x => x.Timestamp.Year == date.Year);
            if (manualAdjustment != null)
            {
                total.deduction = Math.Min(total.result, manualAdjustment.Amount);
                deductionDescription = manualAdjustment.Description;
            }

            total.tax = Common.Round(Math.Max(total.result - total.deduction, 0) * Globals.TaxRate, 0);

            Dictionary<PortfolioDefinition, decimal> resultBreakdown = new();

            foreach (var portfolio in Definitions.Portfolios)
            {
                var portfolioResult = -Common.TaxBook.GetTaxableResult(date, portfolio);
                resultBreakdown.Add(portfolio, portfolioResult);
            }

            decimal unusedDeduction = total.deduction;
            decimal unassignedTax = total.tax;
            foreach (var portfolio in resultBreakdown.OrderByDescending(x => x.Value))
            {
                decimal portfolioDeduction = Math.Min(Math.Max(portfolio.Value,0), unusedDeduction);
                unusedDeduction -= portfolioDeduction;
                decimal portfolioTax = Math.Min(Common.Round(Math.Max(portfolio.Value - portfolioDeduction, 0) * Globals.TaxRate, 0), unassignedTax);
                unassignedTax -= portfolioTax;

                /// <summary>
                /// Asset where accrued expense resulting from tax calculated but not yet charged is booked.
                /// </summary>
                var accountTaxReserves = Common.MainBook.GetAccount(AccountType.TaxReserves, null, portfolio.Key, Globals.LocalCurrency);

                /// <summary>
                /// Account where liabilities resulting from tax charged is booked.
                /// </summary>
                var accountTaxLiabilities = Common.MainBook.GetAccount(AccountType.TaxLiabilities, null, portfolio.Key, Globals.LocalCurrency);

                /// <summary>
                /// Account where tax that will be deducted and paid from current year's result is booked.
                /// </summary>
                var accountIncomeTax = Common.MainBook.GetAccount(AccountType.Tax, null, portfolio.Key, Globals.LocalCurrency);

                var accountIncomeTaxDeduction = Common.TaxBook.GetAccount(AccountType.TaxDeduction, null, portfolio.Key, Globals.LocalCurrency);

                var accountPriorPeriodResult = Common.TaxBook.GetAccount(AccountType.PriorPeriodResult, null, portfolio.Key, Globals.LocalCurrency);

                // Get previous months tax
                decimal previousTax = 0;
                decimal previousDeduction = 0;
                if (date.Month != 1)
                {
                    previousTax = accountIncomeTax.GetNetAmount(prevDate);
                    previousDeduction = accountIncomeTaxDeduction.GetNetAmount(prevDate);
                }

                Common.TaxBook.Enqueue(accountIncomeTaxDeduction, date, -1, deductionDescription, portfolioDeduction - previousDeduction);
                Common.TaxBook.Enqueue(accountPriorPeriodResult, date, -1, deductionDescription, -(portfolioDeduction - previousDeduction));

                if (date.Month == 12)
                {
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax (mid-year assessment derecognition)", -previousTax);
                    Common.MainBook.Enqueue(accountTaxReserves, date, -1, "Income tax (mid-year assessment derecognition)", previousTax);
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax for " + date.Year, portfolioTax);
                    Common.MainBook.Enqueue(accountTaxLiabilities, date, -1, "Income tax for " + date.Year, -portfolioTax);
                }
                else
                {
                    Common.MainBook.Enqueue(accountIncomeTax, date, -1, "Income tax (mid-year assessment)", portfolioTax - previousTax);
                    Common.MainBook.Enqueue(accountTaxReserves, date, -1, "Income tax (mid-year assessment)", -(portfolioTax - previousTax));
                }
            }

            Common.MainBook.Commit();
        }
    }
}
