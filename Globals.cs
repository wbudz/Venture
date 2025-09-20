using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class Globals
    {
        public static string LocalCurrency { get; set; } = "PLN";

        public static List<string> SupportedCurrencies { get; set; } = new List<string>() { "PLN" };

        public static HashSet<string> TaxFreePortfolios = new HashSet<string>() { "EMRT_IKZE", "EMRT_IKE" };

        public static decimal TaxRate = 0.19m;

        public static DateTime TaxableFundSaleEndDate = new DateTime(2023, 12, 31);
    }
}
