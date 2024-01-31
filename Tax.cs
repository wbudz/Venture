using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class TaxCalculations
    {
        public static decimal CalculateFromDividend(decimal amount)
        {
            if (amount <= 0) return 0;
            decimal taxBase = Common.Round(amount);
            decimal tax = taxBase * Globals.TaxRate;
            return Common.Round(tax, 0); // rounded to integer
        }

        public static decimal CalculateFromCoupon(decimal amount)
        {
            if (amount <= 0) return 0;
            decimal taxBase = Common.Round(amount);
            decimal tax = taxBase * Globals.TaxRate;
            return Common.Round(tax);
        }

        public static decimal CalculateFromIncome(decimal amount)
        {
            if (amount <= 0) return 0;
            decimal taxBase = Common.Round(amount);
            decimal tax = taxBase * Globals.TaxRate;
            return Common.Round(tax, 0);
        }
    }
}
