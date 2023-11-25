using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture
{
    public static class TaxCalculations
    {
        public static decimal CalculateFromDividend(decimal amount)
        {
            decimal taxBase = Math.Round(amount, 2);
            decimal taxRate = 0.19m;
            decimal tax = taxBase * taxRate;
            return Math.Round(tax);
        }

        public static decimal CalculateFromCoupon(decimal amount)
        {
            decimal taxBase = Math.Round(amount, 2);
            decimal taxRate = 0.19m;
            decimal tax = taxBase * taxRate;
            return Math.Round(tax, 2);
        }
    }
}
