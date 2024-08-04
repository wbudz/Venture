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
                    var accountIncomeTaxInTaxBook = Common.TaxBook.GetAccount(AccountType.Tax, null, null, Common.LocalCurrency);
                    var accountPriorPeriodResult = Common.TaxBook.GetAccount(AccountType.PriorPeriodResult, null, null, Common.LocalCurrency);

                    Common.TaxBook.Enqueue(accountIncomeTaxInTaxBook, date, -1, manualAdjustment.Description, manualAdjustment.Amount);
                    Common.TaxBook.Enqueue(accountPriorPeriodResult, date, -1, manualAdjustment.Description, -manualAdjustment.Amount);
                    Common.TaxBook.Commit();
                }
            }

            decimal currentTaxResult = -Common.TaxBook.GetResult(date);
            decimal previousTaxResult = date.Month == 1 ? 0 : -Common.TaxBook.GetResult(Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Monthly));

            decimal currentTax = Common.Round(Math.Max(currentTaxResult, 0) * 0.19m, 0);
            decimal previousTax = Common.Round(Math.Max(previousTaxResult, 0) * 0.19m, 0);

            /// <summary>
            /// Asset where accrued expense resulting from tax calculated but not yet charged is booked.
            /// </summary>
            var accountTaxReserves = Common.MainBook.GetAccount(AccountType.TaxReserves, null, null, Common.LocalCurrency);

            /// <summary>
            /// Account where liabilities resulting from tax charged is booked.
            /// </summary>
            var accountTaxLiabilities = Common.MainBook.GetAccount(AccountType.TaxLiabilities, null, null, Common.LocalCurrency);

            /// <summary>
            /// Account where tax that will be deducted and paid from current year's result is booked.
            /// </summary>
            var accountIncomeTax = Common.MainBook.GetAccount(AccountType.Tax, null, null, "PLN");

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

            Common.MainBook.Commit();
        }
    }
}
